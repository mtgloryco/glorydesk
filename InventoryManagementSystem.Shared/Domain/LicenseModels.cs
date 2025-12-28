using System;
using System.Text.Json;

namespace InventoryManagementSystem.Domain
{
    public class LicensePayload
    {
        public Guid LicenseId { get; set; }
        public string HardwareId { get; set; } = string.Empty;
        public string IssuedTo { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime Expiry { get; set; }
        public string Tier { get; set; } = "Free"; // Free | Premium

        public string ToJson()
        {
            // Canonical JSON (stable order)
            var options = new JsonSerializerOptions { WriteIndented = false };
            return JsonSerializer.Serialize(this, options);
        }

        public static LicensePayload? FromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<LicensePayload>(json, options);
            }
            catch
            {
                return null;
            }
        }
    }

    public enum LicenseValidationResult
    {
        Valid,
        Expired,
        HardwareMismatch,
        InvalidSignature,
        InvalidFormat,
        ClockSkewed // System time moved backwards
    }
}
