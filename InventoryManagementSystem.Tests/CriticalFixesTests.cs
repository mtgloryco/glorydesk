using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class CriticalFixesTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private InventoryService _inventoryService = null!;
    private SalesOrderService _salesOrderService = null!;
    private ReturnsService _returnsService = null!;
    private DailyBriefingService _briefingService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var licenseService = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await licenseService.InitializeAsync();
        var auditService = new AuditService(_db);
        _inventoryService = new InventoryService(_db, licenseService, auditService);
        _salesOrderService = new SalesOrderService(_db, _inventoryService);
        _returnsService = new ReturnsService(_db, auditService);
        _briefingService = new DailyBriefingService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task PosCheckout_WithInvoice_DoesNotDoubleCountRevenue()
    {
        var product = await SeedProductWithStockAsync("POS Widget", stock: 10, cost: 5m, price: 20m);
        const decimal saleAmount = 40m;

        await _inventoryService.AddStockMovementAsync(
            product.Id, 2, "OUT", "POS Sale: POS-TEST-1", "cashier",
            unitPrice: 20m, postSalesRevenueJournal: false);

        var conn = _db.Connection;
        var salesJournal = await conn.Table<Journal>().Where(j => j.Type == "Sales").FirstAsync();
        var entryCount = await conn.Table<JournalEntry>().Where(e => e.JournalId == salesJournal.Id).CountAsync();

        var invoiceEntry = new JournalEntry
        {
            EntryNumber = $"{salesJournal.SequencePrefix}/{DateTime.Now.Year}/{(entryCount + 1):D5}",
            JournalId = salesJournal.Id,
            Date = DateTime.Now,
            Reference = "POS Invoice: POS-TEST-1",
            State = "Posted"
        };
        await conn.InsertAsync(invoiceEntry);

        var revAccount = await conn.Table<Account>().Where(a => a.Code == "401000").FirstAsync();
        var arAccount = await conn.Table<Account>().Where(a => a.Code == "111000").FirstAsync();

        await conn.InsertAsync(new JournalLine
        {
            JournalEntryId = invoiceEntry.Id,
            AccountId = arAccount.Id,
            Debit = saleAmount,
            Credit = 0
        });
        await conn.InsertAsync(new JournalLine
        {
            JournalEntryId = invoiceEntry.Id,
            AccountId = revAccount.Id,
            ProductId = product.Id,
            Debit = 0,
            Credit = saleAmount
        });

        var revenueCredits = (await conn.Table<JournalLine>().ToListAsync())
            .Where(l => l.AccountId == revAccount.Id)
            .Sum(l => l.Credit);

        Assert.Equal(saleAmount, revenueCredits);
    }

    [Fact]
    public async Task CustomerReturn_PostsRefundAndRestockJournals()
    {
        var product = await SeedProductWithStockAsync("Return Widget", stock: 5, cost: 10m, price: 25m);

        await _returnsService.ProcessCustomerReturnAsync(new CustomerReturn
        {
            ReturnNumber = "RET-TEST-1",
            ProductId = product.Id,
            Quantity = 1,
            Condition = "Resaleable",
            RefundAmount = 25m,
            ProcessedByUsername = "tester",
            ReturnDate = DateTime.Now
        });

        var conn = _db.Connection;
        var revAccount = await conn.Table<Account>().Where(a => a.Code == "401000").FirstAsync();
        var cashAccount = await conn.Table<Account>().Where(a => a.Code == "101000").FirstAsync();
        var inventoryAccount = await conn.Table<Account>().Where(a => a.Code == "120000").FirstAsync();

        var lines = await conn.Table<JournalLine>().ToListAsync();
        Assert.Contains(lines, l => l.AccountId == revAccount.Id && l.Debit == 25m);
        Assert.Contains(lines, l => l.AccountId == cashAccount.Id && l.Credit == 25m);
        Assert.Contains(lines, l => l.AccountId == inventoryAccount.Id && l.Debit == 10m);
    }

    [Fact]
    public async Task SupplierReturn_PostsApAndInventoryReversal()
    {
        var product = await SeedProductWithStockAsync("Supplier Return Widget", stock: 5, cost: 8m, price: 15m);

        await _returnsService.ProcessSupplierReturnAsync(new SupplierReturn
        {
            ReturnNumber = "SRET-TEST-1",
            SupplierId = 1,
            ProductId = product.Id,
            Quantity = 2,
            CreditAmount = 16m,
            ProcessedByUsername = "tester",
            ReturnDate = DateTime.Now
        });

        var conn = _db.Connection;
        var apAccount = await conn.Table<Account>().Where(a => a.Code == "201000").FirstAsync();
        var inventoryAccount = await conn.Table<Account>().Where(a => a.Code == "120000").FirstAsync();
        var lines = await conn.Table<JournalLine>().ToListAsync();

        Assert.Contains(lines, l => l.AccountId == apAccount.Id && l.Debit == 16m);
        Assert.Contains(lines, l => l.AccountId == inventoryAccount.Id && l.Credit == 16m);
    }

    [Fact]
    public async Task GetAllSalesOrdersAsync_ResolvesCustomerNameFromCustomerTable()
    {
        var conn = _db.Connection;
        var customer = new Customer { Name = "Jane Retailer", Phone = "555", Email = "jane@example.com" };
        await conn.InsertAsync(customer);

        await conn.InsertAsync(new SalesOrder
        {
            SONumber = "SO-CUST-TEST",
            CustomerId = customer.Id,
            OrderDate = DateTime.Now,
            TotalAmount = 150m,
            Status = "Confirmed"
        });

        var orders = await _salesOrderService.GetAllSalesOrdersAsync();
        var match = orders.First(o => o.SalesOrder.SONumber == "SO-CUST-TEST");

        Assert.Equal("Jane Retailer", match.CustomerName);
    }

    [Fact]
    public async Task StockMovement_UpdatesDefaultLocationStock()
    {
        var product = new Product
        {
            Name = "Location Widget",
            SKU = "LOC-1",
            Category = "Test",
            StockQuantity = 0,
            Cost = 5m,
            Price = 12m,
            ProductType = "Good"
        };
        await _db.Connection.InsertAsync(product);

        await _inventoryService.AddStockMovementAsync(
            product.Id, 8, "IN", "Initial stock", "tester", customCost: 5m);

        var location = await _db.Connection.Table<Location>().FirstAsync();
        var locationStock = await _db.Connection.Table<LocationStock>()
            .Where(ls => ls.LocationId == location.Id && ls.ProductId == product.Id)
            .FirstAsync();

        Assert.Equal(8, locationStock.Quantity);

        var updatedProduct = await _db.Connection.FindAsync<Product>(product.Id);
        Assert.Equal(8, updatedProduct!.StockQuantity);
    }

    [Fact]
    public async Task DailyBriefing_ComputesWeekOverWeekSalesComparison()
    {
        var conn = _db.Connection;
        await conn.ExecuteAsync("DELETE FROM SalesOrder;");

        var today = DateTime.Today;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);

        await conn.InsertAsync(new SalesOrder
        {
            SONumber = "SO-LAST-WEEK",
            CustomerId = 0,
            OrderDate = thisWeekStart.AddDays(-3),
            TotalAmount = 100m,
            Status = "Delivered"
        });
        await conn.InsertAsync(new SalesOrder
        {
            SONumber = "SO-THIS-WEEK",
            CustomerId = 0,
            OrderDate = thisWeekStart.AddDays(1),
            TotalAmount = 150m,
            Status = "Delivered"
        });

        var briefing = await _briefingService.GetDailyBriefingAsync();
        var salesItem = briefing.FirstOrDefault(b => b.Message.Contains("compared to last week", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(salesItem);
        Assert.Contains("up 50%", salesItem!.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Product> SeedProductWithStockAsync(string name, int stock, decimal cost, decimal price)
    {
        var product = new Product
        {
            Name = name,
            SKU = Guid.NewGuid().ToString("N")[..8],
            Category = "Test",
            StockQuantity = 0,
            Cost = cost,
            Price = price,
            ProductType = "Good"
        };
        await _db.Connection.InsertAsync(product);

        await _inventoryService.AddStockMovementAsync(
            product.Id, stock, "IN", "Seed stock", "tester", customCost: cost, unitPrice: price);

        return product;
    }
}
