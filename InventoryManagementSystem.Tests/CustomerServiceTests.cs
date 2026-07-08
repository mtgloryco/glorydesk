using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class CustomerServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private CustomerService _service = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _service = new CustomerService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task EnsureWalkInCustomerAsync_CalledMultipleTimes_CreatesExactlyOneRow()
    {
        var first = await _service.EnsureWalkInCustomerAsync();
        var second = await _service.EnsureWalkInCustomerAsync();
        var third = await _service.EnsureWalkInCustomerAsync();

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Id, third.Id);

        var allWalkIns = (await _db.Connection.Table<Customer>().ToListAsync())
            .Where(c => c.Name == "Walk-in Customer")
            .ToList();

        Assert.Single(allWalkIns);
    }
}
