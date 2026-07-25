using System.Net;
using System.Net.Mail;

namespace InventoryManagementSystem.Cloud.Services;

public class EmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]);

    public async Task SendLicenseIssuedAsync(
        string toEmail,
        string company,
        string tier,
        string licenseKey,
        DateTime expiry)
    {
        var subject = $"Your Glory Desk {tier} license key";
        var body = $"""
            Hello,

            Your Glory Desk license for {company} has been issued.

            Tier: {tier}
            Valid until: {expiry:yyyy-MM-dd} UTC

            License key (paste in Glory Desk → License):
            {licenseKey}

            This key is bound to the Hardware ID you submitted. It works on one computer only.

            Need help? Reply to support@mtglory.com

            —
            MT GLORY CO · Glory Desk
            https://glorydesk.mtglory.com
            """;

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendRequestReceivedAsync(string toEmail, string company, string tier)
    {
        var subject = "Glory Desk license request received";
        var body = $"""
            Hello,

            We received your license request for {company} ({tier} tier).

            MT GLORY CO will review and email your signed license key within 1–2 business days.
            Track status at https://glorydesk.mtglory.com/account/login.html

            —
            MT GLORY CO · Glory Desk
            """;

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendAdminAlertAsync(string customerEmail, string company, string tier, string hardwareId)
    {
        var adminEmail = _configuration["Admin:AlertEmail"] ?? "support@mtglory.com";
        var subject = $"[Glory Desk] New license request — {company}";
        var body = $"""
            New license request pending approval.

            Company: {company}
            Email: {customerEmail}
            Tier: {tier}
            Hardware ID: {hardwareId}

            Review: https://glorydesk.mtglory.com/admin/
            """;

        await SendAsync(adminEmail, subject, body);
    }

    private async Task SendAsync(string to, string subject, string body)
    {
        if (!IsConfigured)
        {
            _logger.LogInformation("[Email skipped — SMTP not configured] To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        var host = _configuration["Smtp:Host"]!;
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? "noreply@mtglory.com";
        var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = string.IsNullOrWhiteSpace(user)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(user, password)
        };

        using var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message);
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
