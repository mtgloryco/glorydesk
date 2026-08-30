using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class CustomerOrderHistoryTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private InventoryService _inventory = null!;
    private SalesOrderService _sales = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var license = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await license.InitializeAsync();
        var audit = new AuditService(_db);
        _inventory = new InventoryService(_db, license, audit);
        _sales = new SalesOrderService(_db, _inventory);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    private async Task<int> SeedCustomerAsync(string name)
    {
        var c = new Customer { Name = name, IsActive = true };
        await _db.Connection.InsertAsync(c);
        return c.Id;
    }

    private async Task<int> SeedProductAsync(string name)
    {
        var p = new Product { Name = name, Price = 100 };
        await _db.Connection.InsertAsync(p);
        return p.Id;
    }

    private async Task<int> SeedOrderAsync(int customerId, DateTime date, decimal total, bool isPos,
        params (int productId, int qty, decimal unitPrice)[] lines)
    {
        var order = new SalesOrder
        {
            SONumber = $"SO-{Guid.NewGuid():N}".Substring(0, 12),
            CustomerId = customerId,
            OrderDate = date,
            TotalAmount = total,
            Status = "Confirmed",
            IsPosSale = isPos,
        };
        await _db.Connection.InsertAsync(order);
        foreach (var (productId, qty, unitPrice) in lines)
        {
            await _db.Connection.InsertAsync(new SalesOrderItem
            {
                SalesOrderId = order.Id,
                ProductId = productId,
                QuantityOrdered = qty,
                QuantityDelivered = qty,
                UnitPrice = unitPrice,
            });
        }
        return order.Id;
    }

    [Fact]
    public async Task GetSalesOrdersForCustomerAsync_ReturnsOnlyThatCustomer_NewestFirst()
    {
        var alice = await SeedCustomerAsync("Alice");
        var bob = await SeedCustomerAsync("Bob");
        var bread = await SeedProductAsync("Bread");

        await SeedOrderAsync(alice, new DateTime(2026, 1, 10), 100, isPos: false, (bread, 1, 100m));
        await SeedOrderAsync(alice, new DateTime(2026, 3, 5), 300, isPos: true, (bread, 3, 100m));
        await SeedOrderAsync(bob, new DateTime(2026, 2, 1), 200, isPos: false, (bread, 2, 100m));

        var orders = await _sales.GetSalesOrdersForCustomerAsync(alice);

        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal("Alice", o.CustomerName));
        Assert.Equal(new DateTime(2026, 3, 5), orders[0].SalesOrder.OrderDate); // newest first
    }

    [Fact]
    public async Task GetSalesOrdersForCustomerAsync_ExcludesDeletedOrders()
    {
        var alice = await SeedCustomerAsync("Alice");
        var bread = await SeedProductAsync("Bread");
        var keptId = await SeedOrderAsync(alice, new DateTime(2026, 1, 10), 100, isPos: false, (bread, 1, 100m));
        var goneId = await SeedOrderAsync(alice, new DateTime(2026, 1, 11), 100, isPos: false, (bread, 1, 100m));

        var gone = await _db.Connection.FindAsync<SalesOrder>(goneId);
        gone.IsDeleted = true;
        await _db.Connection.UpdateAsync(gone);

        var orders = await _sales.GetSalesOrdersForCustomerAsync(alice);

        Assert.Single(orders);
        Assert.Equal(keptId, orders[0].SalesOrder.Id);
    }

    [Fact]
    public async Task GetOrderLinesDetailedAsync_JoinsProductNames_AndComputesLineTotal()
    {
        var alice = await SeedCustomerAsync("Alice");
        var bread = await SeedProductAsync("Bread");
        var soda = await SeedProductAsync("Soda");
        var orderId = await SeedOrderAsync(alice, new DateTime(2026, 1, 10), 500, isPos: false,
            (bread, 2, 100m), (soda, 3, 100m));

        var lines = await _sales.GetOrderLinesDetailedAsync(orderId);

        Assert.Equal(2, lines.Count);
        var breadLine = lines.Single(l => l.ProductName == "Bread");
        Assert.Equal(2, breadLine.QuantityOrdered);
        Assert.Equal(200m, breadLine.LineTotal);
    }
}
