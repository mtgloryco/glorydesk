using System.Text;
using System.Text.Json;

namespace InventoryManagementSystem.Cloud.Models;

public class LicensePayload
{
    public Guid LicenseId { get; set; }
    public string HardwareId { get; set; } = string.Empty;
    public string IssuedTo { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime Expiry { get; set; }
    public string Tier { get; set; } = "Basic";

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });
    }
}

public record LicenseRequestRecord(
    Guid Id,
    string Email,
    string Company,
    string Tier,
    string HardwareId,
    DateTime CreatedAt,
    string Status,
    string? LicenseKey,
    Guid? LicenseId,
    DateTime? Expiry,
    DateTime? ProcessedAt,
    int Seats = 1);

public record AdminIssueRequest(int ValidYears = 1, string? Notes = null, int Seats = 1);
public record AdminIssueResponse(bool Success, string Message, string? LicenseKey = null);
public record AccountLicenseDto(
    Guid Id,
    string Tier,
    string Company,
    string HardwareId,
    DateTime IssuedAt,
    DateTime Expiry,
    string Status,
    string? LicenseKey);

// Seat-based multi-machine activation. A single issued LicenseRequest (identified by
// its LicenseId) can be activated on up to `Seats` distinct machines. The first machine
// is activated automatically when the admin issues the license; additional machines call
// /api/license/activate directly (no per-machine admin approval needed) until the seat
// count is used up.
public record LicenseActivateRequest(Guid LicenseId, string Email, string HardwareId);
public record LicenseActivateResponse(
    bool Success,
    string Message,
    string? LicenseKey = null,
    int SeatsUsed = 0,
    int MaxSeats = 0);

public record LicenseDeactivateRequest(Guid LicenseId, string Email, string HardwareId);
public record LicenseDeactivateResponse(bool Success, string Message);

public record LicenseActivationDto(
    Guid Id,
    string HardwareId,
    DateTime ActivatedAt,
    bool IsActive);
