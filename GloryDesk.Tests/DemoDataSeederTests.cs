using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class DemoDataSeederTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task InitializeAsync_SeedsDemoProductsAndRelatedData_OnFreshDatabase()
    {
        var products = await _db.Connection.Table<Product>().CountAsync();
        var suppliers = await _db.Connection.Table<Supplier>().CountAsync();
        var customers = await _db.Connection.Table<Customer>().CountAsync();
        var salesOrders = await _db.Connection.Table<SalesOrder>().CountAsync();
        var movements = await _db.Connection.Table<StockMovement>().CountAsync();

        Assert.True(products >= 20);
        Assert.True(suppliers >= 5);
        Assert.True(customers >= 5);
        Assert.True(salesOrders >= 10);
        Assert.True(movements >= 20);
    }
}
