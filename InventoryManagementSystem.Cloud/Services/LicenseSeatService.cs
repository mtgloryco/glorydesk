using InventoryManagementSystem.Cloud.Data;
using InventoryManagementSystem.Cloud.Models;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace InventoryManagementSystem.Cloud.Services;

/// <summary>
/// Self-service seat activation. A single issued license (identified by its LicenseId +
/// account email) can be activated on up to `Seats` machines without needing a fresh
/// admin-approved request per machine. This is what lets a company buy one Enterprise
/// license for N seats and roll it out to N PCs on their own.
/// </summary>
public class LicenseSeatService
{
    private readonly CloudDatabase _db;
    private readonly LicenseAdminService _adminService;
    private readonly LicenseKeyGeneratorService _keyGenerator;

    public LicenseSeatService(CloudDatabase db, LicenseAdminService adminService, LicenseKeyGeneratorService keyGenerator)
    {
        _db = db;
        _adminService = adminService;
        _keyGenerator = keyGenerator;
    }

    public async Task<LicenseActivateResponse> ActivateAsync(LicenseActivateRequest request)
    {
        var hardwareId = request.HardwareId?.Trim() ?? string.Empty;
        if (hardwareId.Length < 8)
        {
            return new LicenseActivateResponse(false, "A valid Hardware ID is required.");
        }

        var license = await _adminService.GetByLicenseIdAsync(request.LicenseId, request.Email ?? string.Empty);
        if (license is null)
        {
            return new LicenseActivateResponse(false, "No matching issued license found for that email.");
        }

        if (license.Expiry is not null && license.Expiry.Value < DateTime.UtcNow)
        {
            return new LicenseActivateResponse(false, "This license has expired.");
        }

        if (!_keyGenerator.IsConfigured)
        {
            return new LicenseActivateResponse(false, "License signing key is not configured on the server.");
        }

        var activeCount = await CountActiveActivationsAsync(license.Id);

        // Reactivating the same machine (e.g. reinstalled the app) doesn't cost a seat.
        var existing = await FindActivationAsync(license.Id, hardwareId);
        if (existing is { IsActive: true })
        {
            return new LicenseActivateResponse(true, "This machine is already activated.", null, activeCount, license.Seats);
        }

        if (existing is null && activeCount >= license.Seats)
        {
            return new LicenseActivateResponse(
                false,
                $"Seat limit reached ({activeCount}/{license.Seats}). Deactivate another machine or buy more seats.",
                null,
                activeCount,
                license.Seats);
        }

        var expiry = license.Expiry ?? DateTime.UtcNow.AddYears(1);
        var licenseKey = _keyGenerator.GenerateKey(hardwareId, license.Company, license.Tier, expiry, out _);
        var now = DateTime.UtcNow;

        await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                if (existing is not null)
                {
                    await using var reactivate = new NpgsqlCommand(
                        "UPDATE license_activations SET is_active = TRUE, license_key = @key, activated_at = @activatedAt WHERE id = @id",
                        pg);
                    reactivate.Parameters.AddWithValue("key", licenseKey);
                    reactivate.Parameters.AddWithValue("activatedAt", now);
                    reactivate.Parameters.AddWithValue("id", existing.Value.Id);
                    await reactivate.ExecuteNonQueryAsync();
                }
                else
                {
                    await using var insert = new NpgsqlCommand(
                        """
                        INSERT INTO license_activations (id, license_request_id, hardware_id, license_key, activated_at, is_active)
                        VALUES (@id, @requestId, @hardwareId, @key, @activatedAt, TRUE)
                        """,
                        pg);
                    insert.Parameters.AddWithValue("id", Guid.NewGuid());
                    insert.Parameters.AddWithValue("requestId", license.Id);
                    insert.Parameters.AddWithValue("hardwareId", hardwareId);
                    insert.Parameters.AddWithValue("key", licenseKey);
                    insert.Parameters.AddWithValue("activatedAt", now);
                    await insert.ExecuteNonQueryAsync();
                }
            }
            else
            {
                var sqlite = (SqliteConnection)conn;
                if (existing is not null)
                {
                    await using var reactivate = sqlite.CreateCommand();
                    reactivate.CommandText = "UPDATE LicenseActivations SET IsActive = 1, LicenseKey = $key, ActivatedAt = $activatedAt WHERE Id = $id";
                    reactivate.Parameters.AddWithValue("$key", licenseKey);
                    reactivate.Parameters.AddWithValue("$activatedAt", now.ToString("O"));
                    reactivate.Parameters.AddWithValue("$id", existing.Value.Id.ToString());
                    await reactivate.ExecuteNonQueryAsync();
                }
                else
                {
                    await using var insert = sqlite.CreateCommand();
                    insert.CommandText = """
                        INSERT INTO LicenseActivations (Id, LicenseRequestId, HardwareId, LicenseKey, ActivatedAt, IsActive)
                        VALUES ($id, $requestId, $hardwareId, $key, $activatedAt, 1)
                        """;
                    insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                    insert.Parameters.AddWithValue("$requestId", license.Id.ToString());
                    insert.Parameters.AddWithValue("$hardwareId", hardwareId);
                    insert.Parameters.AddWithValue("$key", licenseKey);
                    insert.Parameters.AddWithValue("$activatedAt", now.ToString("O"));
                    await insert.ExecuteNonQueryAsync();
                }
            }
        });

        var newCount = existing is null ? activeCount + 1 : activeCount;
        return new LicenseActivateResponse(true, $"Machine activated ({newCount}/{license.Seats} seats used).", licenseKey, newCount, license.Seats);
    }

    public async Task<LicenseDeactivateResponse> DeactivateAsync(LicenseDeactivateRequest request)
    {
        var hardwareId = request.HardwareId?.Trim() ?? string.Empty;
        var license = await _adminService.GetByLicenseIdAsync(request.LicenseId, request.Email ?? string.Empty);
        if (license is null)
        {
            return new LicenseDeactivateResponse(false, "No matching issued license found for that email.");
        }

        var existing = await FindActivationAsync(license.Id, hardwareId);
        if (existing is null || !existing.Value.IsActive)
        {
            return new LicenseDeactivateResponse(false, "That machine is not currently active on this license.");
        }

        await _db.WithConnectionAsync(async conn =>
        {
            if (_db.Provider == CloudDatabaseProvider.Postgres)
            {
                var pg = (NpgsqlConnection)conn;
                await using var cmd = new NpgsqlCommand("UPDATE license_activations SET is_active = FALSE WHERE id = @id", pg);
                cmd.Parameters.AddWithValue("id", existing.Value.Id);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var sqlite = (SqliteConnection)conn;
                await using var cmd = sqlite.CreateCommand();
                cmd.CommandText = "UPDATE LicenseActivations SET IsActive = 0 WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", existing.Value.Id.ToString());
                await cmd.ExecuteNonQueryAsync();
            }
        });

        return new LicenseDeactivateResponse(true, "Seat freed. It can now be used to activate a different machine.");
    }

    private async Task<int> CountActiveActivationsAsync(Guid licenseRequestId)
    {
        var activations = await _adminService.GetActivationsAsync(licenseRequestId);
        return activations.Count(a => a.IsActive);
    }

    private async Task<(Guid Id, bool IsActive)?> FindActivationAsync(Guid licenseRequestId, string hardwareId)
    {
        var activations = await _adminService.GetActivationsAsync(licenseRequestId);
        var match = activations.FirstOrDefault(a => string.Equals(a.HardwareId, hardwareId, StringComparison.OrdinalIgnoreCase));
        return match is null ? null : (match.Id, match.IsActive);
    }
}
