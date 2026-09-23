using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

/// <summary>
/// Covers three accounting-correctness fixes: standalone Expense recording, categorized
/// Damage/Loss stock write-offs, and the requirement that a Sales Order be fully delivered
/// before it can be invoiced (so AR is never posted to the Aging Report ahead of the ledger).
/// </summary>
public class AccountingCorrectnessTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private readonly string _settingsPath = TempFile.CreateSettingsPath();
    private DatabaseService _db = null!;
    private ExpenseService _expenseService = null!;
    private DamageWriteOffService _damageWriteOffService = null!;
    private InventoryService _inventoryService = null!;
    private SalesOrderService _salesOrderService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var licenseService = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await licenseService.InitializeAsync();
        var auditService = new AuditService(_db);
        _inventoryService = new InventoryService(_db, licenseService, auditService);
        _salesOrderService = new SalesOrderService(_db, _inventoryService, auditService);
        _expenseService = new ExpenseService(_db, auditService);
        var settingsService = new SettingsService(_settingsPath);
        var purchaseOrderService = new PurchaseOrderService(_db, _inventoryService, auditService, settingsService);
        var manufacturingService = new ManufacturingService(_db, auditService);
        _damageWriteOffService = new DamageWriteOffService(_db, _inventoryService, purchaseOrderService, manufacturingService);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
        TempFile.DeleteFile(_settingsPath);
    }

    [Fact]
    public async Task RecordExpenseAsync_PostsBalancedJournalEntry()
    {
        var expense = new Expense
        {
            Category = "Rent",
            Description = "August rent",
            Amount = 250m,
            PaymentMethod = "Cash"
        };

        await _expenseService.RecordExpenseAsync(expense, "tester");

        var lines = await _db.Connection.Table<JournalLine>()
            .Where(l => l.Label.Contains("August rent"))
            .ToListAsync();

        Assert.Equal(2, lines.Count);
        Assert.Equal(250m, lines.Sum(l => l.Debit));
        Assert.Equal(250m, lines.Sum(l => l.Credit));

        var rentAccount = await _db.Connection.Table<Account>().Where(a => a.Code == "512000").FirstAsync();
        Assert.Contains(lines, l => l.AccountId == rentAccount.Id && l.Debit == 250m);

        var cashAccount = await _db.Connection.Table<Account>().Where(a => a.Code == "101000").FirstAsync();
        Assert.Contains(lines, l => l.AccountId == cashAccount.Id && l.Credit == 250m);
    }

    [Fact]
    public async Task RecordExpenseAsync_RejectsZeroAmount()
    {
        var expense = new Expense { Category = "General", Description = "Bad expense", Amount = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _expenseService.RecordExpenseAsync(expense, "tester"));
    }

    [Fact]
    public async Task RecordWriteOffAsync_DeductsStockAndPostsAdjustmentJournal()
    {
        var product = new Product
        {
            Name = "Fragile Widget",
            SKU = "FRAG-1",
            Category = "Test",
            StockQuantity = 0,
            Cost = 10m,
            Price = 20m,
            ProductType = "Good"
        };
        await _db.Connection.InsertAsync(product);
        await _inventoryService.AddStockMovementAsync(product.Id, 20, "IN", "Initial stock", "tester", customCost: 10m);

        await _damageWriteOffService.RecordWriteOffAsync(product.Id, 5, "Damaged", "Dropped in warehouse", "tester");

        var updatedProduct = await _db.Connection.FindAsync<Product>(product.Id);
        Assert.Equal(15, updatedProduct!.StockQuantity);

        var writeOffs = await _damageWriteOffService.GetAllAsync();
        var writeOff = Assert.Single(writeOffs);
        Assert.Equal("Damaged", writeOff.ReasonCategory);
        Assert.Equal(50m, writeOff.TotalCost); // 5 * 10

        var adjExpenseAccount = await _db.Connection.Table<Account>().Where(a => a.Code == "520000").FirstAsync();
        var lines = await _db.Connection.Table<JournalLine>()
            .Where(l => l.AccountId == adjExpenseAccount.Id)
            .ToListAsync();
        Assert.Contains(lines, l => l.Debit == 50m);
    }

    [Fact]
    public async Task RecordWriteOffAsync_RejectsInsufficientStock()
    {
        var product = new Product { Name = "Empty Shelf Item", SKU = "EMPTY-1", Category = "Test", StockQuantity = 2, Cost = 10m, Price = 20m, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _damageWriteOffService.RecordWriteOffAsync(product.Id, 5, "Damaged", "", "tester"));
    }

    [Fact]
    public async Task ReplenishAsync_CreatesDraftPurchaseOrder_WhenNoBom()
    {
        var supplier = new Supplier { Name = "Backup Supplies Ltd" };
        await _db.Connection.InsertAsync(supplier);

        var product = new Product { Name = "Purchased Widget", SKU = "PUR-1", Category = "Test", StockQuantity = 10, Cost = 8m, Price = 20m, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        await _db.Connection.InsertAsync(new ReorderRule
        {
            ProductId = product.Id,
            PreferredSupplierId = supplier.Id,
            ReorderPoint = 5,
            ReorderQuantity = 20,
            LeadTimeDays = 7
        });

        var (docType, docNumber) = await _damageWriteOffService.ReplenishAsync(product.Id, 5, "tester");

        Assert.Equal("Purchase Order", docType);
        var po = await _db.Connection.Table<PurchaseOrder>().Where(p => p.PONumber == docNumber).FirstAsync();
        Assert.Equal("Draft", po.Status);
        Assert.Equal(supplier.Id, po.SupplierId);

        var items = await _db.Connection.Table<PurchaseOrderItem>().Where(i => i.PurchaseOrderId == po.Id).ToListAsync();
        var item = Assert.Single(items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(5, item.QuantityOrdered);
    }

    [Fact]
    public async Task ReplenishAsync_ThrowsWhenNoPreferredSupplierConfigured()
    {
        var product = new Product { Name = "Orphan Widget", SKU = "ORPH-1", Category = "Test", StockQuantity = 10, Cost = 8m, Price = 20m, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _damageWriteOffService.ReplenishAsync(product.Id, 5, "tester"));
    }

    [Fact]
    public async Task ReplenishAsync_CreatesDraftManufacturingOrder_WhenProductHasBom()
    {
        var component = new Product { Name = "Raw Component", SKU = "COMP-1", Category = "Test", StockQuantity = 100, Cost = 2m, Price = 5m, ProductType = "Good" };
        await _db.Connection.InsertAsync(component);

        var finished = new Product { Name = "Assembled Widget", SKU = "ASM-1", Category = "Test", StockQuantity = 3, Cost = 6m, Price = 15m, ProductType = "Good" };
        await _db.Connection.InsertAsync(finished);

        var bom = new BillOfMaterial { ProductId = finished.Id, Quantity = 1.0 };
        await _db.Connection.InsertAsync(bom);
        await _db.Connection.InsertAsync(new BillOfMaterialLine { BillOfMaterialId = bom.Id, ProductId = component.Id, Quantity = 2.0 });

        var (docType, docNumber) = await _damageWriteOffService.ReplenishAsync(finished.Id, 4, "tester");

        Assert.Equal("Manufacturing Order", docType);
        var mo = await _db.Connection.Table<ManufacturingOrder>().Where(m => m.MONumber == docNumber).FirstAsync();
        Assert.Equal("Draft", mo.Status);
        Assert.Equal(bom.Id, mo.BomId);
        Assert.Equal(4, mo.TargetQuantity);
    }

    [Fact]
    public async Task InvoiceSalesOrderAsync_ThrowsWhenNotYetDelivered()
    {
        var so = new SalesOrder
        {
            SONumber = "SO-UNDELIVERED-1",
            CustomerId = 1,
            Status = "Confirmed",
            DeliveryStatus = "Pending",
            BillingStatus = "Waiting Invoice",
            OrderDate = DateTime.Today,
            TotalAmount = 500
        };
        await _db.Connection.InsertAsync(so);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _salesOrderService.InvoiceSalesOrderAsync(so.Id));

        var reloaded = await _db.Connection.FindAsync<SalesOrder>(so.Id);
        Assert.Equal("Waiting Invoice", reloaded!.BillingStatus);
    }

    [Fact]
    public async Task InvoiceSalesOrderAsync_SucceedsAfterFullDelivery()
    {
        var product = new Product { Name = "Deliverable Widget", SKU = "DLV-1", Category = "Test", StockQuantity = 0, Cost = 5m, Price = 15m, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);
        await _inventoryService.AddStockMovementAsync(product.Id, 10, "IN", "Initial stock", "tester", customCost: 5m);

        var so = new SalesOrder { SONumber = "SO-DELIVERED-1", CustomerId = 1, Status = "Confirmed", OrderDate = DateTime.Today };
        var item = new SalesOrderItem { ProductId = product.Id, QuantityOrdered = 4, UnitPrice = 15m };
        await _salesOrderService.CreateSalesOrderAsync(so, new() { item });

        var items = await _salesOrderService.GetItemsAsync(so.Id);
        await _salesOrderService.DeliverSalesOrderAsync(so.Id, items.Select(i => (i.Id, i.QuantityOrdered)).ToList());

        await _salesOrderService.InvoiceSalesOrderAsync(so.Id);

        var reloaded = await _db.Connection.FindAsync<SalesOrder>(so.Id);
        Assert.Equal("Invoiced", reloaded!.BillingStatus);
    }
}
