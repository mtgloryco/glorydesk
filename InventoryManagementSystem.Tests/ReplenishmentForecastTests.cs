using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class ReplenishmentForecastTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private InventoryService _inventoryService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var licenseService = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await licenseService.InitializeAsync();
        var auditService = new AuditService(_db);
        _inventoryService = new InventoryService(_db, licenseService, auditService);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task GetReplenishmentForecastAsync_SumsIncomingFromOpenPOs_IncludingDraftRfqs()
    {
        var product = new Product { Name = "Widget", SKU = "WGT-1", Category = "Test", StockQuantity = 10, Cost = 5m, Price = 10m, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        var approvedPo = new PurchaseOrder { PONumber = "PO-1", SupplierId = 1, Status = "Approved", OrderDate = DateTime.Today.AddDays(-2), TotalAmount = 100 };
        await _db.Connection.InsertAsync(approvedPo);
        await _db.Connection.InsertAsync(new PurchaseOrderItem { PurchaseOrderId = approvedPo.Id, ProductId = product.Id, QuantityOrdered = 20, QuantityReceived = 5, UnitCost = 5m });

        var draftPo = new PurchaseOrder { PONumber = "PO-2-RFQ", SupplierId = 1, Status = "Draft", OrderDate = DateTime.Today.AddDays(-1), TotalAmount = 40 };
        await _db.Connection.InsertAsync(draftPo);
        await _db.Connection.InsertAsync(new PurchaseOrderItem { PurchaseOrderId = draftPo.Id, ProductId = product.Id, QuantityOrdered = 8, QuantityReceived = 0, UnitCost = 5m });

        var cancelledPo = new PurchaseOrder { PONumber = "PO-3-CANCELLED", SupplierId = 1, Status = "Cancelled", OrderDate = DateTime.Today, TotalAmount = 500 };
        await _db.Connection.InsertAsync(cancelledPo);
        await _db.Connection.InsertAsync(new PurchaseOrderItem { PurchaseOrderId = cancelledPo.Id, ProductId = product.Id, QuantityOrdered = 100, QuantityReceived = 0, UnitCost = 5m });

        var fullyReceivedPo = new PurchaseOrder { PONumber = "PO-4-DONE", SupplierId = 1, Status = "Received", OrderDate = DateTime.Today, TotalAmount = 50 };
        await _db.Connection.InsertAsync(fullyReceivedPo);
        await _db.Connection.InsertAsync(new PurchaseOrderItem { PurchaseOrderId = fullyReceivedPo.Id, ProductId = product.Id, QuantityOrdered = 10, QuantityReceived = 10, UnitCost = 5m });

        var forecast = await _inventoryService.GetReplenishmentForecastAsync(product.Id);

        Assert.Equal(10, forecast.OnHand);
        Assert.Equal(23, forecast.Incoming); // 15 (approved, partial) + 8 (draft RFQ)
        Assert.Equal(2, forecast.IncomingSources.Count);
        Assert.Contains(forecast.IncomingSources, s => s.DocumentNumber == "PO-1" && s.Quantity == 15);
        Assert.Contains(forecast.IncomingSources, s => s.DocumentNumber == "PO-2-RFQ" && s.Quantity == 8);
        Assert.DoesNotContain(forecast.IncomingSources, s => s.DocumentNumber == "PO-3-CANCELLED");
        Assert.DoesNotContain(forecast.IncomingSources, s => s.DocumentNumber == "PO-4-DONE");
    }

    [Fact]
    public async Task GetReplenishmentForecastAsync_SumsOutgoingFromOpenSOs_IncludingDraftQuotations()
    {
        var product = new Product { Name = "Gadget", SKU = "GDG-1", Category = "Test", StockQuantity = 50, Cost = 5m, Price = 10m, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        var confirmedSo = new SalesOrder { SONumber = "SO-1", CustomerId = 1, Status = "Confirmed", OrderDate = DateTime.Today.AddDays(-2), TotalAmount = 60 };
        await _db.Connection.InsertAsync(confirmedSo);
        await _db.Connection.InsertAsync(new SalesOrderItem { SalesOrderId = confirmedSo.Id, ProductId = product.Id, QuantityOrdered = 6, QuantityDelivered = 2, UnitPrice = 10m });

        var draftSo = new SalesOrder { SONumber = "SO-2-QUOTE", CustomerId = 1, Status = "Draft", OrderDate = DateTime.Today.AddDays(-1), TotalAmount = 30 };
        await _db.Connection.InsertAsync(draftSo);
        await _db.Connection.InsertAsync(new SalesOrderItem { SalesOrderId = draftSo.Id, ProductId = product.Id, QuantityOrdered = 3, QuantityDelivered = 0, UnitPrice = 10m });

        var cancelledSo = new SalesOrder { SONumber = "SO-3-CANCELLED", CustomerId = 1, Status = "Cancelled", OrderDate = DateTime.Today, TotalAmount = 200 };
        await _db.Connection.InsertAsync(cancelledSo);
        await _db.Connection.InsertAsync(new SalesOrderItem { SalesOrderId = cancelledSo.Id, ProductId = product.Id, QuantityOrdered = 20, QuantityDelivered = 0, UnitPrice = 10m });

        var deliveredSo = new SalesOrder { SONumber = "SO-4-DONE", CustomerId = 1, Status = "Delivered", OrderDate = DateTime.Today, TotalAmount = 10 };
        await _db.Connection.InsertAsync(deliveredSo);
        await _db.Connection.InsertAsync(new SalesOrderItem { SalesOrderId = deliveredSo.Id, ProductId = product.Id, QuantityOrdered = 1, QuantityDelivered = 1, UnitPrice = 10m });

        var forecast = await _inventoryService.GetReplenishmentForecastAsync(product.Id);

        Assert.Equal(50, forecast.OnHand);
        Assert.Equal(7, forecast.Outgoing); // 4 (confirmed, partial) + 3 (draft quotation)
        Assert.Equal(2, forecast.OutgoingSources.Count);
        Assert.Contains(forecast.OutgoingSources, s => s.DocumentNumber == "SO-1" && s.Quantity == 4);
        Assert.Contains(forecast.OutgoingSources, s => s.DocumentNumber == "SO-2-QUOTE" && s.Quantity == 3);
        Assert.DoesNotContain(forecast.OutgoingSources, s => s.DocumentNumber == "SO-3-CANCELLED");
        Assert.DoesNotContain(forecast.OutgoingSources, s => s.DocumentNumber == "SO-4-DONE");
        Assert.Equal(50 + 0 - 7, forecast.ForecastedAvailable);
    }
}
