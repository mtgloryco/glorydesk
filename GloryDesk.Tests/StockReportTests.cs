using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class StockReportTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private InventoryService _inventory = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        var license = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await license.InitializeAsync();
        _inventory = new InventoryService(_db, license, new AuditService(_db));
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task StockReportRow_ComputesValueIncomingOutgoingAndFreeToUse()
    {
        var product = new Product { Name = "Croissant", SKU = "CR-1", Cost = 10m, Price = 25m, StockQuantity = 100, ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        // Open PO: 30 ordered, 12 already received -> 18 still incoming.
        var po = new PurchaseOrder { PONumber = "PO-1", Status = "Approved", OrderDate = DateTime.Today };
        await _db.Connection.InsertAsync(po);
        await _db.Connection.InsertAsync(new PurchaseOrderItem { PurchaseOrderId = po.Id, ProductId = product.Id, QuantityOrdered = 30, QuantityReceived = 12 });

        // Open SO: 40 ordered, 15 delivered -> 25 still outgoing.
        var so = new SalesOrder { SONumber = "SO-1", Status = "Confirmed", OrderDate = DateTime.Today };
        await _db.Connection.InsertAsync(so);
        await _db.Connection.InsertAsync(new SalesOrderItem { SalesOrderId = so.Id, ProductId = product.Id, QuantityOrdered = 40, QuantityDelivered = 15 });

        var row = (await _inventory.GetStockReportRowsAsync()).Single(r => r.ProductId == product.Id);

        Assert.Equal(100, row.OnHand);
        Assert.Equal(18, row.Incoming);
        Assert.Equal(25, row.Outgoing);
        Assert.Equal(75, row.FreeToUse);          // 100 on hand - 25 committed out
        Assert.Equal(1000m, row.TotalValue);      // 100 * 10
        Assert.Equal("[CR-1] Croissant", row.DisplayName);
    }

    [Fact]
    public async Task StockMoves_FilterByProduct_NewestFirst_WithDirectionLabels()
    {
        var a = new Product { Name = "Prod A", SKU = "A-1", Cost = 5, Price = 10, ProductType = "Good", StockQuantity = 0 };
        var b = new Product { Name = "Prod B", SKU = "B-1", Cost = 5, Price = 10, ProductType = "Good", StockQuantity = 0 };
        await _db.Connection.InsertAsync(a);
        await _db.Connection.InsertAsync(b);

        await _inventory.AddStockMovementAsync(a.Id, 10, "IN", "Purchase Receipt: PO-1", "buyer", customCost: 5);
        await _inventory.AddStockMovementAsync(a.Id, 3, "OUT", "POS Sale: POS-1", "cashier", unitPrice: 10);
        await _inventory.AddStockMovementAsync(b.Id, 7, "IN", "Purchase Receipt: PO-2", "buyer", customCost: 5);

        var moves = await _inventory.GetStockMovesAsync(a.Id);

        Assert.Equal(2, moves.Count);
        Assert.All(moves, m => Assert.Equal("Prod A", m.ProductName));
        Assert.Equal("OUT", moves[0].MovementType);          // newest first
        Assert.True(moves[0].IsOutbound);
        Assert.Equal("Stock", moves[0].FromLabel);
        Assert.Equal("Customer / External", moves[0].ToLabel);
        Assert.Equal("-3", moves[0].QuantityDisplay);
        Assert.Equal("Done", moves[0].Status);
    }

    [Fact]
    public async Task StockMoves_ResolvesLotSerialFromTheBatchAnOutMoveDrewFrom()
    {
        var product = new Product { Name = "Tracked", SKU = "T-1", Cost = 8, Price = 20, ProductType = "Good", StockQuantity = 0 };
        await _db.Connection.InsertAsync(product);

        await _inventory.AddStockMovementAsync(product.Id, 10, "IN", "Purchase Receipt: PO-9", "buyer",
            customCost: 8, batchNumber: "LOT-A99");
        await _inventory.AddStockMovementAsync(product.Id, 4, "OUT", "POS Sale: POS-9", "cashier", unitPrice: 20);

        var moves = await _inventory.GetStockMovesAsync(product.Id);
        var outMove = moves.Single(m => m.MovementType == "OUT");

        Assert.Equal("LOT-A99", outMove.LotSerial);
    }
}
