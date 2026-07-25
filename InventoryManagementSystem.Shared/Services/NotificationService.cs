using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class NotificationService
    {
        private readonly DatabaseService _databaseService;
        private readonly PaymentService _paymentService;
        private readonly AuditService? _auditService;
        private readonly SettingsService? _settingsService;

        public NotificationService(
            DatabaseService databaseService,
            PaymentService paymentService,
            AuditService? auditService = null,
            SettingsService? settingsService = null)
        {
            _databaseService = databaseService;
            _paymentService = paymentService;
            _auditService = auditService;
            _settingsService = settingsService;
        }

        public async Task<NotificationOutbox> QueueInvoiceDeliveryAsync(int salesOrderId, string channel = "Email")
        {
            var order = await _databaseService.Connection.FindAsync<SalesOrder>(salesOrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Sales order {salesOrderId} not found.");
            }

            var customer = order.CustomerId > 0
                ? await _databaseService.Connection.FindAsync<Customer>(order.CustomerId)
                : null;

            var recipient = channel.Equals("SMS", StringComparison.OrdinalIgnoreCase)
                ? customer?.Phone ?? string.Empty
                : customer?.Email ?? string.Empty;

            if (string.IsNullOrWhiteSpace(recipient))
            {
                throw new InvalidOperationException("Customer has no contact details for the selected channel.");
            }

            var notification = new NotificationOutbox
            {
                Channel = channel,
                Recipient = recipient,
                Subject = $"Invoice {order.SONumber}",
                Body = BuildInvoiceBody(order, customer?.Name ?? "Customer"),
                ReferenceType = "SalesOrder",
                ReferenceId = salesOrderId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _databaseService.Connection.InsertAsync(notification);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    UserSession.CurrentUser?.Username ?? "System",
                    "QueueNotification",
                    "NotificationOutbox",
                    notification.Id,
                    notification);
            }

            return notification;
        }

        public async Task<List<NotificationOutbox>> QueuePaymentRemindersAsync(int overdueDays = 7)
        {
            var cutoff = DateTime.Today.AddDays(-overdueDays);
            var orders = await _databaseService.Connection.Table<SalesOrder>()
                .Where(so => so.BillingStatus == "Invoiced" && so.OrderDate <= cutoff)
                .ToListAsync();

            var queued = new List<NotificationOutbox>();
            foreach (var order in orders)
            {
                var open = await _paymentService.GetOpenBalanceAsync("SalesOrder", order.Id);
                if (open <= 0) continue;

                var customer = order.CustomerId > 0
                    ? await _databaseService.Connection.FindAsync<Customer>(order.CustomerId)
                    : null;
                if (string.IsNullOrWhiteSpace(customer?.Email)) continue;

                var notification = new NotificationOutbox
                {
                    Channel = "Email",
                    Recipient = customer!.Email,
                    Subject = $"Payment reminder: {order.SONumber}",
                    Body = $"Dear {customer.Name},\n\nThis is a reminder that invoice {order.SONumber} still has an open balance of {open:N2} {order.Currency}.\n\nThank you.",
                    ReferenceType = "SalesOrder",
                    ReferenceId = order.Id,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _databaseService.Connection.InsertAsync(notification);
                queued.Add(notification);

                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(
                        UserSession.CurrentUser?.Username ?? "System",
                        "QueueReminder",
                        "NotificationOutbox",
                        notification.Id,
                        notification);
                }
            }

            return queued;
        }

        public async Task<int> ProcessPendingNotificationsAsync()
        {
            var pending = await _databaseService.Connection.Table<NotificationOutbox>()
                .Where(n => n.Status == "Pending")
                .ToListAsync();

            var folder = AppPaths.EnsureDocumentsSubfolder("Notifications");
            var settings = _settingsService?.CurrentSettings;
            var useSmtp = settings != null
                          && settings.UseSmtp
                          && !string.IsNullOrWhiteSpace(settings.SmtpHost)
                          && !string.IsNullOrWhiteSpace(settings.SmtpFromAddress);

            var sent = 0;
            foreach (var notification in pending)
            {
                try
                {
                    if (notification.Channel.Equals("Email", StringComparison.OrdinalIgnoreCase) && useSmtp)
                    {
                        await SendSmtpAsync(settings!, notification);
                    }
                    else
                    {
                        var extension = notification.Channel.Equals("SMS", StringComparison.OrdinalIgnoreCase) ? ".sms.txt" : ".eml";
                        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{notification.Id}{extension}";
                        var path = Path.Combine(folder, fileName);

                        var content = notification.Channel.Equals("SMS", StringComparison.OrdinalIgnoreCase)
                            ? $"TO: {notification.Recipient}\n{notification.Body}"
                            : $"To: {notification.Recipient}\nSubject: {notification.Subject}\n\n{notification.Body}";

                        await File.WriteAllTextAsync(path, content);
                    }

                    notification.Status = "Sent";
                    notification.SentAt = DateTime.UtcNow;
                    notification.ErrorMessage = string.Empty;
                    sent++;
                }
                catch (Exception ex)
                {
                    notification.Status = "Failed";
                    notification.ErrorMessage = ex.Message;
                }

                await _databaseService.Connection.UpdateAsync(notification);
            }

            return sent;
        }

        private static async Task SendSmtpAsync(AppSettings settings, NotificationOutbox notification)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(
                    settings.SmtpFromAddress,
                    string.IsNullOrWhiteSpace(settings.SmtpFromName) ? settings.StoreName : settings.SmtpFromName),
                Subject = notification.Subject,
                Body = notification.Body,
                IsBodyHtml = false
            };
            message.To.Add(notification.Recipient);

            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort > 0 ? settings.SmtpPort : 587)
            {
                EnableSsl = settings.SmtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(settings.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword ?? string.Empty);
            }

            await client.SendMailAsync(message);
        }

        private static string BuildInvoiceBody(SalesOrder order, string customerName)
        {
            return $"Dear {customerName},\n\nPlease find your invoice {order.SONumber} dated {order.OrderDate:yyyy-MM-dd} for {order.TotalAmount:N2} {order.Currency}.\n\nThank you for your business.";
        }
    }
}
