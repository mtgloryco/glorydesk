using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class RecurringInvoiceService
    {
        private readonly DatabaseService _databaseService;
        private readonly SalesOrderService _salesOrderService;
        private readonly AuditService? _auditService;

        public RecurringInvoiceService(
            DatabaseService databaseService,
            SalesOrderService salesOrderService,
            AuditService? auditService = null)
        {
            _databaseService = databaseService;
            _salesOrderService = salesOrderService;
            _auditService = auditService;
        }

        public async Task<List<RecurringInvoice>> GetAllAsync(bool activeOnly = false)
        {
            var query = _databaseService.Connection.Table<RecurringInvoice>().Where(r => !r.IsDeleted);
            var list = await query.OrderBy(r => r.NextRunDate).ToListAsync();
            return activeOnly ? list.Where(r => r.IsActive).ToList() : list;
        }

        public async Task<List<RecurringInvoiceLine>> GetLinesAsync(int recurringInvoiceId)
        {
            return await _databaseService.Connection.Table<RecurringInvoiceLine>()
                .Where(l => l.RecurringInvoiceId == recurringInvoiceId)
                .ToListAsync();
        }

        public async Task<RecurringInvoice> SaveAsync(RecurringInvoice schedule, IEnumerable<RecurringInvoiceLine> lines, string username)
        {
            if (schedule.CustomerId <= 0)
            {
                throw new InvalidOperationException("Customer is required.");
            }

            var lineList = lines?.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList()
                           ?? new List<RecurringInvoiceLine>();
            if (lineList.Count == 0)
            {
                throw new InvalidOperationException("At least one product line is required.");
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            schedule.CreatedByUsername = string.IsNullOrWhiteSpace(schedule.CreatedByUsername)
                ? username
                : schedule.CreatedByUsername;

            if (schedule.Id == 0)
            {
                await _databaseService.Connection.InsertAsync(schedule);
            }
            else
            {
                await _databaseService.Connection.UpdateAsync(schedule);
                var existing = await GetLinesAsync(schedule.Id);
                foreach (var old in existing)
                {
                    await _databaseService.Connection.DeleteAsync(old);
                }
            }

            foreach (var line in lineList)
            {
                line.Id = 0;
                line.RecurringInvoiceId = schedule.Id;
                await _databaseService.Connection.InsertAsync(line);
            }

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "RecurringInvoiceSaved", "RecurringInvoice", schedule.Id, schedule);
            }

            return schedule;
        }

        public async Task SetActiveAsync(int id, bool isActive, string username)
        {
            var schedule = await _databaseService.Connection.FindAsync<RecurringInvoice>(id)
                ?? throw new InvalidOperationException("Recurring invoice not found.");
            schedule.IsActive = isActive;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _databaseService.Connection.UpdateAsync(schedule);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, isActive ? "RecurringResume" : "RecurringPause", "RecurringInvoice", id, schedule);
            }
        }

        public async Task DeleteAsync(int id, string username)
        {
            var schedule = await _databaseService.Connection.FindAsync<RecurringInvoice>(id)
                ?? throw new InvalidOperationException("Recurring invoice not found.");
            schedule.IsDeleted = true;
            schedule.IsActive = false;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _databaseService.Connection.UpdateAsync(schedule);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "RecurringDeleted", "RecurringInvoice", id, schedule);
            }
        }

        public async Task<List<SalesOrder>> RunDueAsync(DateTime? asOf = null, string username = "System")
        {
            var runDate = (asOf ?? DateTime.Today).Date;
            var due = (await GetAllAsync(activeOnly: true))
                .Where(r => r.NextRunDate.Date <= runDate && (r.EndDate == null || r.EndDate.Value.Date >= runDate))
                .ToList();

            var generated = new List<SalesOrder>();
            foreach (var schedule in due)
            {
                var order = await GenerateOnceAsync(schedule, username);
                generated.Add(order);
            }

            return generated;
        }

        public async Task<SalesOrder> GenerateOnceAsync(RecurringInvoice schedule, string username)
        {
            var lines = await GetLinesAsync(schedule.Id);
            if (lines.Count == 0)
            {
                throw new InvalidOperationException($"Recurring invoice '{schedule.Name}' has no lines.");
            }

            var so = new SalesOrder
            {
                CustomerId = schedule.CustomerId,
                Status = "Draft",
                OrderDate = DateTime.Now,
                QuotationDate = DateTime.Now,
                PaymentTerms = schedule.PaymentTerms,
                Notes = string.IsNullOrWhiteSpace(schedule.Notes)
                    ? $"Generated from recurring invoice: {schedule.Name}"
                    : schedule.Notes,
                CreatedByUsername = username,
                Currency = schedule.Currency,
                IsTaxInclusive = schedule.IsTaxInclusive,
                BillingStatus = "Waiting Invoice",
                DeliveryStatus = "Pending",
                Company = "My Company"
            };

            var items = lines.Select(l => new SalesOrderItem
            {
                ProductId = l.ProductId,
                QuantityOrdered = l.Quantity,
                UnitPrice = l.UnitPrice,
                TaxId = l.TaxId
            }).ToList();

            await _salesOrderService.CreateSalesQuotationAsync(so, items);
            await _salesOrderService.ConfirmQuotationAsync(so.Id);
            await _salesOrderService.InvoiceSalesOrderAsync(so.Id);

            schedule.LastGeneratedAt = DateTime.Now;
            schedule.LastSalesOrderId = so.Id;
            schedule.NextRunDate = AdvanceDate(schedule.NextRunDate, schedule.Frequency);
            if (schedule.EndDate.HasValue && schedule.NextRunDate.Date > schedule.EndDate.Value.Date)
            {
                schedule.IsActive = false;
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            await _databaseService.Connection.UpdateAsync(schedule);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "RecurringGenerated", "RecurringInvoice", schedule.Id,
                    new { schedule.Id, SalesOrderId = so.Id, so.SONumber });
            }

            return so;
        }

        public static DateTime AdvanceDate(DateTime from, string frequency)
        {
            return (frequency ?? "Monthly").Trim().ToLowerInvariant() switch
            {
                "daily" => from.Date.AddDays(1),
                "weekly" => from.Date.AddDays(7),
                "yearly" => from.Date.AddYears(1),
                _ => from.Date.AddMonths(1)
            };
        }
    }
}
