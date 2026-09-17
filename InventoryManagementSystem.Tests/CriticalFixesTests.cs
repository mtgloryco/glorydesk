using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.UI.ViewModels;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class CriticalFixesTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private AuditService _auditService = null!;
    private InventoryService _inventoryService = null!;
    private SalesOrderService _salesOrderService = null!;
    private PurchaseOrderService _purchaseOrderService = null!;
    private ReturnsService _returnsService = null!;
    private DailyBriefingService _briefingService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var licenseService = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await licenseService.InitializeAsync();
        _auditService = new AuditService(_db);
        _inventoryService = new InventoryService(_db, licenseService, _auditService);
        _salesOrderService = new SalesOrderService(_db, _inventoryService);
        _purchaseOrderService = new PurchaseOrderService(_db, _inventoryService, _auditService);
        _returnsService = new ReturnsService(_db, _auditService);
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

    [Fact]
    public async Task PurchaseOrderReturn_ProcessesReturnDeductsStockAndCreatesDebitNote()
    {
        var conn = _db.Connection;
        var supplier = await conn.Table<Supplier>().FirstAsync();
        var product = await SeedProductWithStockAsync("PO Return Widget", stock: 0, cost: 25m, price: 50m);

        var po = new PurchaseOrder
        {
            PONumber = "PO-RET-TEST",
            SupplierId = supplier.Id,
            OrderDate = DateTime.Now,
            Status = "Confirmed",
            ReceiptStatus = "Pending",
            BillingStatus = "Waiting Bill",
            TotalAmount = 250m
        };
        await conn.InsertAsync(po);

        var item = new PurchaseOrderItem
        {
            PurchaseOrderId = po.Id,
            ProductId = product.Id,
            QuantityOrdered = 10,
            QuantityReceived = 0,
            UnitCost = 25m
        };
        await conn.InsertAsync(item);

        // Receive the items
        await _purchaseOrderService.ReceivePurchaseOrderAsync(po.Id, new List<PurchaseReceiveLine>
        {
            new PurchaseReceiveLine { ItemId = item.Id, QuantityReceived = 10 }
        });

        // Verify received
        var updatedItem = await conn.FindAsync<PurchaseOrderItem>(item.Id);
        Assert.Equal(10, updatedItem!.QuantityReceived);
        var updatedProd = await conn.FindAsync<Product>(product.Id);
        Assert.Equal(10, updatedProd!.StockQuantity);

        // Process return of 4 items
        await _returnsService.ProcessPurchaseOrderReturnAsync(po.Id, new List<(int itemId, int quantityToReturn, string reason, decimal creditAmount)>
        {
            (item.Id, 4, "Defective Goods", 100m)
        }, "tester");

        // Verify item QuantityReceived decremented
        updatedItem = await conn.FindAsync<PurchaseOrderItem>(item.Id);
        Assert.Equal(6, updatedItem!.QuantityReceived);

        // Verify product stock decremented
        updatedProd = await conn.FindAsync<Product>(product.Id);
        Assert.Equal(6, updatedProd!.StockQuantity);

        // Verify PO receipt status is Partially Received
        var updatedPo = await conn.FindAsync<PurchaseOrder>(po.Id);
        Assert.Equal("Partially Received", updatedPo!.ReceiptStatus);

        // Verify SupplierReturn record
        var supplierReturns = await conn.Table<SupplierReturn>().Where(r => r.ProductId == product.Id).ToListAsync();
        Assert.Single(supplierReturns);
        Assert.Equal(4, supplierReturns[0].Quantity);
        Assert.Equal(100m, supplierReturns[0].CreditAmount);
        Assert.Equal(po.PONumber, supplierReturns[0].OriginalReceiptId);

        // Verify DebitNote record
        var debitNotes = await conn.Table<DebitNote>().Where(d => d.PurchaseOrderId == po.Id).ToListAsync();
        Assert.Single(debitNotes);
        Assert.Equal(100m, debitNotes[0].Amount);
        Assert.Equal(100m, debitNotes[0].AppliedAmount);
        Assert.Equal(po.Id, debitNotes[0].AppliedToPurchaseOrderId);
    }

    [Fact]
    public async Task PurchaseOrdersViewModel_OpenReturnOrderAndSubmit_WorksCorrectly()
    {
        var conn = _db.Connection;
        var supplier = await conn.Table<Supplier>().FirstAsync();
        var product = await SeedProductWithStockAsync("VM Return Widget", stock: 0, cost: 15m, price: 30m);

        var po = new PurchaseOrder
        {
            PONumber = "PO-VM-RET",
            SupplierId = supplier.Id,
            OrderDate = DateTime.Now,
            Status = "Confirmed",
            ReceiptStatus = "Pending",
            BillingStatus = "Waiting Bill",
            TotalAmount = 75m
        };
        await conn.InsertAsync(po);

        var item = new PurchaseOrderItem
        {
            PurchaseOrderId = po.Id,
            ProductId = product.Id,
            QuantityOrdered = 5,
            QuantityReceived = 0,
            UnitCost = 15m
        };
        await conn.InsertAsync(item);

        await _purchaseOrderService.ReceivePurchaseOrderAsync(po.Id, new List<PurchaseReceiveLine>
        {
            new PurchaseReceiveLine { ItemId = item.Id, QuantityReceived = 5 }
        });

        var taxService = new TaxService(_db);
        var settingsService = new SettingsService();
        var currencyService = new CurrencyService(_db, _auditService);
        var paymentService = new PaymentService(_db, _auditService, currencyService, settingsService);
        var languageService = new LanguageService();
        var supplierService = new SupplierService(_db);

        var vm = new PurchaseOrdersViewModel(
            _purchaseOrderService,
            supplierService,
            _inventoryService,
            taxService,
            settingsService,
            _returnsService,
            paymentService,
            currencyService,
            languageService);

        var displayItem = new PurchaseOrderDisplayItem(po, supplier.Name);
        await vm.OpenDetailsCommand.ExecuteAsync(displayItem);

        Assert.True(vm.CanReturnDetailedPo);

        // Open return order without passing parameter (simulates clicking button in modal)
        await vm.OpenReturnOrderCommand.ExecuteAsync(null);

        Assert.True(vm.IsReturnModalOpen);
        Assert.Single(vm.ReturnRows);
        Assert.Equal(5, vm.ReturnRows[0].QuantityToReturn);
        Assert.Equal("VM Return Widget", vm.ReturnRows[0].ProductName);
        Assert.Equal(75m, vm.ReturnRows[0].CreditAmount);

        // Change quantity to return to 2 -> CreditAmount dynamically recalculates to 30
        vm.ReturnRows[0].QuantityToReturn = 2;
        Assert.Equal(30m, vm.ReturnRows[0].CreditAmount);

        // Submit return
        await vm.SubmitReturnCommand.ExecuteAsync(null);

        Assert.False(vm.IsReturnModalOpen);
        Assert.Contains("Return processed successfully", vm.StatusMessage);

        var prodAfter = await conn.FindAsync<Product>(product.Id);
        Assert.Equal(3, prodAfter!.StockQuantity);
    }

    [Fact]
    public async Task ReturnsViewModel_SupplierReturn_ProcessesAndRecordsDebitNote()
    {
        var conn = _db.Connection;
        var supplier = await conn.Table<Supplier>().FirstAsync();
        var product = await SeedProductWithStockAsync("ReturnsVM Supplier Widget", stock: 10, cost: 20m, price: 40m);

        var vm = new ReturnsViewModel(_returnsService, _inventoryService, _salesOrderService, _purchaseOrderService);
        await vm.LoadInitialData();

        vm.ReturnType = "Supplier Return";
        vm.SelectedProduct = vm.Products.First(p => p.Id == product.Id);
        vm.SelectedSupplier = vm.Suppliers.First(s => s.Id == supplier.Id);
        vm.Quantity = 3;
        vm.Reason = "Overstocked";

        Assert.True(vm.IsSupplierReturn);
        Assert.Equal(60m, vm.RefundAmount);

        await vm.ProcessReturnCommand.ExecuteAsync(null);

        Assert.Contains("processed successfully", vm.StatusMessage);

        var prodAfter = await conn.FindAsync<Product>(product.Id);
        Assert.Equal(7, prodAfter!.StockQuantity);

        var supReturns = await _returnsService.GetSupplierReturnsAsync(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
        Assert.Contains(supReturns, r => r.ProductId == product.Id && r.Quantity == 3);
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

        if (stock > 0)
        {
            await _inventoryService.AddStockMovementAsync(
                product.Id, stock, "IN", "Seed stock", "tester", customCost: cost, unitPrice: price);
        }

        return product;
    }
}
