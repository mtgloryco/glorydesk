using InventoryManagementSystem.Cloud.Data;
using InventoryManagementSystem.Cloud.Models;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace InventoryManagementSystem.Cloud.Services;

public class LicenseRequestService
{
    private static readonly HashSet<string> ValidTiers =
        new(StringComparer.OrdinalIgnoreCase) { "Basic", "Medium", "Pro", "Enterprise" };

    private readonly CloudDatabase _db;
    private readonly EmailNotificationService _email;

    public LicenseRequestService(CloudDatabase db, EmailNotificationService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<LicenseRequestResponse> SubmitAsync(LicenseRequestDto request)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var company = request.Company?.Trim() ?? string.Empty;
        var tier = request.Tier?.Trim() ?? string.Empty;
        var hardwareId = request.HardwareId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return new LicenseRequestResponse(false, "A valid business email is required.");
        }

        if (string.IsNullOrWhiteSpace(company))
        {
            return new LicenseRequestResponse(false, "Company or shop name is required.");
        }

        if (!ValidTiers.Contains(tier))
        {
            return new LicenseRequestResponse(false, "Select a valid tier: Basic, Medium, Pro, or Enterprise.");
        }

        if (hardwareId.Length < 8)
        {
            return new LicenseRequestResponse(false, "Paste the Hardware ID from Glory Desk → License.");
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO license_requests (id, email, company, tier, hardware_id, created_at, status)
                    VALUES (@id, @email, @company, @tier, @hardware_id, @created, @status)
                    """,
                    pg);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("email", email.ToLowerInvariant());
                cmd.Parameters.AddWithValue("company", company);
                cmd.Parameters.AddWithValue("tier", tier);
                cmd.Parameters.AddWithValue("hardware_id", hardwareId);
                cmd.Parameters.AddWithValue("created", now);
                cmd.Parameters.AddWithValue("status", "pending");
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var sqlite = (SqliteConnection)conn;
                await using var cmd = sqlite.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO LicenseRequests (Id, Email, Company, Tier, HardwareId, CreatedAt, Status)
                    VALUES ($id, $email, $company, $tier, $hardwareId, $created, $status)
                    """;
                cmd.Parameters.AddWithValue("$id", id.ToString());
                cmd.Parameters.AddWithValue("$email", email.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$company", company);
                cmd.Parameters.AddWithValue("$tier", tier);
                cmd.Parameters.AddWithValue("$hardwareId", hardwareId);
                cmd.Parameters.AddWithValue("$created", now.ToString("O"));
                cmd.Parameters.AddWithValue("$status", "pending");
                await cmd.ExecuteNonQueryAsync();
            }
        });

        await _email.SendRequestReceivedAsync(email, company, tier);
        await _email.SendAdminAlertAsync(email, company, tier, hardwareId);

        return new LicenseRequestResponse(
            true,
            "Request received. MT GLORY CO will email your signed license key within 1–2 business days.");
    }
}
