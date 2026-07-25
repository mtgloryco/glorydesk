using System.Security.Cryptography;
using System.Text;
using InventoryManagementSystem.Cloud.Models;

namespace InventoryManagementSystem.Cloud.Services;

public class LicenseKeyGeneratorService
{
    private readonly RSA? _rsa;
    private readonly ILogger<LicenseKeyGeneratorService> _logger;

    public LicenseKeyGeneratorService(IConfiguration configuration, ILogger<LicenseKeyGeneratorService> logger)
    {
        _logger = logger;
        var privateKeyBase64 = configuration["License:PrivateKeyBase64"]
            ?? Environment.GetEnvironmentVariable("LICENSE_RSA_PRIVATE_KEY_B64");

        if (string.IsNullOrWhiteSpace(privateKeyBase64))
        {
            _logger.LogWarning("License private key not configured. Key generation disabled.");
            return;
        }

        try
        {
            _rsa = RSA.Create();
            _rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load license private key.");
            _rsa = null;
        }
    }

    public bool IsConfigured => _rsa is not null;

    public string GenerateKey(string hardwareId, string issuedTo, string tier, DateTime expiry, out Guid licenseId)
    {
        if (_rsa is null)
        {
            throw new InvalidOperationException("License signing key is not configured.");
        }

        licenseId = Guid.NewGuid();
        var payload = new LicensePayload
        {
            LicenseId = licenseId,
            HardwareId = hardwareId.Trim(),
            IssuedTo = issuedTo.Trim(),
            IssuedAt = DateTime.UtcNow,
            Expiry = expiry.ToUniversalTime(),
            Tier = tier.Trim()
        };

        var payloadJson = payload.ToJson();
        var dataBytes = Encoding.UTF8.GetBytes(payloadJson);
        var signature = _rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{Convert.ToBase64String(dataBytes)}.{Convert.ToBase64String(signature)}";
    }
}
