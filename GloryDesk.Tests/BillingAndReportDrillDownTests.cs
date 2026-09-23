using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class BillingAndReportDrillDownTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private readonly string _settingsPath = TempFile.CreateSettingsPath();
    private DatabaseService _db = null!;
    private PurchaseOrderService _poService = null!;
    private PaymentService _paymentService = null!;
    private AccountingReportService _reportService = null!;
    private InventoryService _inventoryService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var licenseService = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await licenseService.InitializeAsync();
        var auditService = new AuditService(_db);
        _inventoryService = new InventoryService(_db, licenseService, auditService);
        var settingsService = new SettingsService(_settingsPath);
        _poService = new PurchaseOrderService(_db, _inventoryService, auditService, settingsService);
        _paymentService = new PaymentService(_db, auditService, null, settingsService);
        _reportService = new AccountingReportService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
        TempFile.DeleteFile(_settingsPath);
    }

    [Fact]
    public async Task PurchaseOrder_BillingAndPaymentLifecycle_TransitionsCorrectly()
    {
        // 1. Create a PO with 100 total amount
        var po = new PurchaseOrder
        {
            PONumber = "PO-LIFECYCLE-1",
            SupplierId = 1,
            Status = "Approved",
            ReceiptStatus = "Received",
            BillingStatus = "Waiting Bill",
            OrderDate = DateTime.Today,
            TotalAmount = 100m,
            Currency = "USD"
        };
        await _db.Connection.InsertAsync(po);

        var item = new PurchaseOrderItem
        {
            PurchaseOrderId = po.Id,
            ProductId = 1,
            QuantityOrdered = 10,
            QuantityReceived = 10,
            QuantityBilled = 0,
            UnitCost = 10m
        };
        await _db.Connection.InsertAsync(item);

        Assert.Equal("Waiting Bill", po.BillingStatus);

        // 2. Create Bill -> Transitions to "In Payment"
        await _poService.CreateBillAsync(po.Id);
        var billedPo = await _db.Connection.FindAsync<PurchaseOrder>(po.Id);
        Assert.NotNull(billedPo);
        Assert.Equal("In Payment", billedPo!.BillingStatus);

        // 3. Partial Payment (40) -> Transitions to "Partially Paid"
        await _paymentService.RecordInvoicePaymentAsync("PurchaseOrder", po.Id, 40m, "Bank", "tester");
        var partialPo = await _db.Connection.FindAsync<PurchaseOrder>(po.Id);
        Assert.NotNull(partialPo);
        Assert.Equal("Partially Paid", partialPo!.BillingStatus);

        // 4. Remaining Payment (60) -> Transitions to "Paid"
        await _paymentService.RecordInvoicePaymentAsync("PurchaseOrder", po.Id, 60m, "Bank", "tester");
        var fullyPaidPo = await _db.Connection.FindAsync<PurchaseOrder>(po.Id);
        Assert.NotNull(fullyPaidPo);
        Assert.Equal("Paid", fullyPaidPo!.BillingStatus);

        // 5. GetAllPurchaseOrdersAsync reflects the synced status
        var allPos = await _poService.GetAllPurchaseOrdersAsync();
        var listed = allPos.FirstOrDefault(p => p.PurchaseOrder.Id == po.Id);
        Assert.NotNull(listed);
        Assert.Equal("Paid", listed!.PurchaseOrder.BillingStatus);
    }

    [Fact]
    public async Task ReportLineDrillDown_ReturnsOnlyItsOwnJournalLines()
    {
        // 1. Set up Chart of Accounts
        var arAccount = new Account { Code = "1200", Name = "Accounts Receivable", Type = "Asset: Receivable" };
        var revAccount = new Account { Code = "4000", Name = "Product Sales", Type = "Income: Revenue" };
        await _db.Connection.InsertAsync(arAccount);
        await _db.Connection.InsertAsync(revAccount);

        // 2. Set up a report line for Revenue (Prefix "40")
        var report = new AccountingReport { Name = "Profit and Loss", RootReport = "profit-loss" };
        await _db.Connection.InsertAsync(report);

        var reportLine = new ReportLine
        {
            ReportId = report.Id,
            Name = "Operating Revenue",
            Code = "REV_OP",
            Level = 1
        };
        await _db.Connection.InsertAsync(reportLine);

        var computation = new ReportLineComputation
        {
            ReportLineId = reportLine.Id,
            ComputationEngine = "Prefix of Account Codes",
            Formula = "40"
        };
        await _db.Connection.InsertAsync(computation);

        // 3. Create a journal entry: Debit AR $500, Credit Revenue $500
        var entry = new JournalEntry
        {
            EntryNumber = "JE-001",
            Date = DateTime.Now,
            State = "Posted",
            Reference = "Sales Invoice #101"
        };
        await _db.Connection.InsertAsync(entry);

        var arLine = new JournalLine
        {
            JournalEntryId = entry.Id,
            AccountId = arAccount.Id,
            Label = "Customer Receivable",
            Debit = 500m,
            Credit = 0m
        };
        var revLine = new JournalLine
        {
            JournalEntryId = entry.Id,
            AccountId = revAccount.Id,
            Label = "Product Sales Revenue",
            Debit = 0m,
            Credit = 500m
        };
        await _db.Connection.InsertAsync(arLine);
        await _db.Connection.InsertAsync(revLine);

        // 4. Drill-down on the Revenue report line
        var drillDownLines = await _reportService.GetJournalLinesForReportLineAsync(reportLine.Id);

        // 5. Verify: MUST contain ONLY the revenue line, NOT the AR line!
        Assert.Single(drillDownLines);
        var resultLine = drillDownLines[0];
        Assert.Contains("4000", resultLine.AccountDisplay);
        Assert.Equal(0m, resultLine.Debit);
        Assert.Equal(500m, resultLine.Credit);
        Assert.Equal("Product Sales Revenue", resultLine.Label);
    }
}
