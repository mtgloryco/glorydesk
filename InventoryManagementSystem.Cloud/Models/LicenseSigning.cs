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
    DateTime? ProcessedAt);

public record AdminIssueRequest(int ValidYears = 1, string? Notes = null);
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
