using InventoryManagementSystem.Cloud.Data;
using InventoryManagementSystem.Cloud.Models;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace InventoryManagementSystem.Cloud.Services;

public class LicenseAdminService
{
    private readonly CloudDatabase _db;
    private readonly LicenseKeyGeneratorService _keyGenerator;
    private readonly EmailNotificationService _email;

    public LicenseAdminService(
        CloudDatabase db,
        LicenseKeyGeneratorService keyGenerator,
        EmailNotificationService email)
    {
        _db = db;
        _keyGenerator = keyGenerator;
        _email = email;
    }

    public async Task<IReadOnlyList<LicenseRequestRecord>> ListRequestsAsync(string? status = null)
    {
        return await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                var sql = """
                    SELECT id, email, company, tier, hardware_id, created_at, status,
                           license_key, license_id, expiry, processed_at
                    FROM license_requests
                    """;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    sql += " WHERE status = @status";
                }
                sql += " ORDER BY created_at DESC LIMIT 200";

                await using var cmd = new NpgsqlCommand(sql, pg);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    cmd.Parameters.AddWithValue("status", status);
                }

                var list = new List<LicenseRequestRecord>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(ReadPostgresRecord(reader));
                }
                return (IReadOnlyList<LicenseRequestRecord>)list;
            }

            var sqlite = (SqliteConnection)conn;
            var sqliteSql = """
                SELECT Id, Email, Company, Tier, HardwareId, CreatedAt, Status,
                       LicenseKey, LicenseId, Expiry, ProcessedAt
                FROM LicenseRequests
                """;
            if (!string.IsNullOrWhiteSpace(status))
            {
                sqliteSql += " WHERE Status = $status";
            }
            sqliteSql += " ORDER BY CreatedAt DESC LIMIT 200";

            await using var sqliteCmd = sqlite.CreateCommand();
            sqliteCmd.CommandText = sqliteSql;
            if (!string.IsNullOrWhiteSpace(status))
            {
                sqliteCmd.Parameters.AddWithValue("$status", status);
            }

            var sqliteList = new List<LicenseRequestRecord>();
            await using var sqliteReader = await sqliteCmd.ExecuteReaderAsync();
            while (await sqliteReader.ReadAsync())
            {
                sqliteList.Add(ReadSqliteRecord(sqliteReader));
            }
            return sqliteList;
        });
    }

    public async Task<AdminIssueResponse> IssueAsync(Guid requestId, AdminIssueRequest options)
    {
        if (!_keyGenerator.IsConfigured)
        {
            return new AdminIssueResponse(false, "License signing key is not configured on the server.");
        }

        var years = Math.Clamp(options.ValidYears, 1, 10);
        var expiry = DateTime.UtcNow.AddYears(years);
        var now = DateTime.UtcNow;

        var request = await GetRequestAsync(requestId);
        if (request is null)
        {
            return new AdminIssueResponse(false, "Request not found.");
        }

        if (request.Status == "issued")
        {
            return new AdminIssueResponse(false, "License already issued for this request.", request.LicenseKey);
        }

        var licenseKey = _keyGenerator.GenerateKey(
            request.HardwareId,
            request.Company,
            request.Tier,
            expiry,
            out var licenseId);

        await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                await using var cmd = new NpgsqlCommand(
                    """
                    UPDATE license_requests
                    SET status = 'issued', license_key = @key, license_id = @lid,
                        expiry = @expiry, processed_at = @processed, admin_notes = @notes
                    WHERE id = @id
                    """,
                    pg);
                cmd.Parameters.AddWithValue("key", licenseKey);
                cmd.Parameters.AddWithValue("lid", licenseId);
                cmd.Parameters.AddWithValue("expiry", expiry);
                cmd.Parameters.AddWithValue("processed", now);
                cmd.Parameters.AddWithValue("notes", options.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("id", requestId);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var sqlite = (SqliteConnection)conn;
                await using var cmd = sqlite.CreateCommand();
                cmd.CommandText = """
                    UPDATE LicenseRequests
                    SET Status = 'issued', LicenseKey = $key, LicenseId = $lid,
                        Expiry = $expiry, ProcessedAt = $processed, AdminNotes = $notes
                    WHERE Id = $id
                    """;
                cmd.Parameters.AddWithValue("$key", licenseKey);
                cmd.Parameters.AddWithValue("$lid", licenseId.ToString());
                cmd.Parameters.AddWithValue("$expiry", expiry.ToString("O"));
                cmd.Parameters.AddWithValue("$processed", now.ToString("O"));
                cmd.Parameters.AddWithValue("$notes", options.Notes ?? string.Empty);
                cmd.Parameters.AddWithValue("$id", requestId.ToString());
                await cmd.ExecuteNonQueryAsync();
            }
        });

        await _email.SendLicenseIssuedAsync(request.Email, request.Company, request.Tier, licenseKey, expiry);

        return new AdminIssueResponse(true, $"License issued. Valid until {expiry:yyyy-MM-dd}.", licenseKey);
    }

    public async Task<AdminIssueResponse> RejectAsync(Guid requestId, string? reason)
    {
        var request = await GetRequestAsync(requestId);
        if (request is null)
        {
            return new AdminIssueResponse(false, "Request not found.");
        }

        await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                await using var cmd = new NpgsqlCommand(
                    "UPDATE license_requests SET status = 'rejected', processed_at = @processed, admin_notes = @notes WHERE id = @id",
                    pg);
                cmd.Parameters.AddWithValue("processed", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("notes", reason ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("id", requestId);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var sqlite = (SqliteConnection)conn;
                await using var cmd = sqlite.CreateCommand();
                cmd.CommandText = "UPDATE LicenseRequests SET Status = 'rejected', ProcessedAt = $processed, AdminNotes = $notes WHERE Id = $id";
                cmd.Parameters.AddWithValue("$processed", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("$notes", reason ?? string.Empty);
                cmd.Parameters.AddWithValue("$id", requestId.ToString());
                await cmd.ExecuteNonQueryAsync();
            }
        });

        return new AdminIssueResponse(true, "Request rejected.");
    }

    public async Task<IReadOnlyList<AccountLicenseDto>> GetLicensesForEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                await using var cmd = new NpgsqlCommand(
                    """
                    SELECT id, tier, company, hardware_id, created_at, expiry, status, license_key
                    FROM license_requests
                    WHERE lower(email) = @email AND status = 'issued'
                    ORDER BY processed_at DESC
                    """,
                    pg);
                cmd.Parameters.AddWithValue("email", normalized);

                var list = new List<AccountLicenseDto>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new AccountLicenseDto(
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetDateTime(4),
                        reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
                        reader.GetString(6),
                        reader.IsDBNull(7) ? null : reader.GetString(7)));
                }
                return list;
            }

            var sqlite = (SqliteConnection)conn;
            await using var sqliteCmd = sqlite.CreateCommand();
            sqliteCmd.CommandText = """
                SELECT Id, Tier, Company, HardwareId, CreatedAt, Expiry, Status, LicenseKey
                FROM LicenseRequests
                WHERE lower(Email) = $email AND Status = 'issued'
                ORDER BY ProcessedAt DESC
                """;
            sqliteCmd.Parameters.AddWithValue("$email", normalized);

            var sqliteList = new List<AccountLicenseDto>();
            await using var sqliteReader = await sqliteCmd.ExecuteReaderAsync();
            while (await sqliteReader.ReadAsync())
            {
                sqliteList.Add(new AccountLicenseDto(
                    Guid.Parse(sqliteReader.GetString(0)),
                    sqliteReader.GetString(1),
                    sqliteReader.GetString(2),
                    sqliteReader.GetString(3),
                    DateTime.Parse(sqliteReader.GetString(4)),
                    sqliteReader.IsDBNull(5) ? DateTime.MinValue : DateTime.Parse(sqliteReader.GetString(5)),
                    sqliteReader.GetString(6),
                    sqliteReader.IsDBNull(7) ? null : sqliteReader.GetString(7)));
            }
            return sqliteList;
        });
    }

    private async Task<LicenseRequestRecord?> GetRequestAsync(Guid id)
    {
        var all = await ListRequestsAsync(null);
        return all.FirstOrDefault(r => r.Id == id);
    }

    private static LicenseRequestRecord ReadPostgresRecord(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetDateTime(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetGuid(8),
            reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            reader.IsDBNull(10) ? null : reader.GetDateTime(10));

    private static LicenseRequestRecord ReadSqliteRecord(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            DateTime.Parse(reader.GetString(5)),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : Guid.Parse(reader.GetString(8)),
            reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
            reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)));
}
