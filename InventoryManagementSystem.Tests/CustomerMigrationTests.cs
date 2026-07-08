using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using Xunit;

namespace InventoryManagementSystem.Tests;

/// <summary>
/// Covers the v3 migration (<c>DatabaseService.MigrateToV3Async</c>) that splits Customer data
/// out of the legacy Supplier table when a SalesOrder referenced a Supplier as its "customer".
/// </summary>
public class CustomerMigrationTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;

    public Task InitializeAsync()
    {
        // Intentionally do NOT call InitializeAsync() here: this test needs to seed rows into a
        // pre-migration (fresh, user_version = 0) schema before the migration pipeline runs.
        _db = new DatabaseService(_dbPath);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task InitializeAsync_MigratesSupplierReferencedBySalesOrder_IntoNewCustomerRow()
    {
        var connection = _db.Connection;
        await connection.CreateTableAsync<Supplier>();
        await connection.CreateTableAsync<SalesOrder>();

        // An unrelated supplier that is never referenced by a SalesOrder - inserted first purely so the
        // target supplier's Id differs from the eventual auto-incremented Customer Id, making the
        // "not the original Supplier.Id" assertion below meaningful rather than a coincidence.
        var unrelatedSupplier = new Supplier { Name = "Unrelated Supplier Co", Phone = "111", Email = "unrelated@example.com" };
        await connection.InsertAsync(unrelatedSupplier);

        var targetSupplier = new Supplier
        {
            Name = "Acme Farm Supply",
            ContactPerson = "Jane Doe",
            Phone = "0788123456",
            Email = "acme@example.com"
        };
        await connection.InsertAsync(targetSupplier);

        var salesOrder = new SalesOrder { SONumber = "SO-TEST-1", CustomerId = targetSupplier.Id, TotalAmount = 100m };
        await connection.InsertAsync(salesOrder);

        // Act: this is what production code calls at startup - table creation + migrations + seeding.
        await _db.InitializeAsync();

        // Assert: a Customer row now exists mirroring the original Supplier's contact info.
        var customers = await connection.Table<Customer>().ToListAsync();
        var migratedCustomer = customers.FirstOrDefault(c => c.Name == targetSupplier.Name);
        Assert.NotNull(migratedCustomer);
        Assert.Equal(targetSupplier.Phone, migratedCustomer!.Phone);
        Assert.Equal(targetSupplier.Email, migratedCustomer.Email);
        Assert.Equal(targetSupplier.ContactPerson, migratedCustomer.ContactPerson);

        // Assert: the SalesOrder.CustomerId was remapped to the new Customer, not the original Supplier.
        var updatedOrder = await connection.Table<SalesOrder>().Where(so => so.Id == salesOrder.Id).FirstAsync();
        Assert.Equal(migratedCustomer.Id, updatedOrder.CustomerId);
        Assert.NotEqual(targetSupplier.Id, updatedOrder.CustomerId);

        // Assert: the original Supplier row was never mutated or removed.
        var supplierStillPresent = await connection.FindAsync<Supplier>(targetSupplier.Id);
        Assert.NotNull(supplierStillPresent);
        Assert.Equal(targetSupplier.Name, supplierStillPresent!.Name);
        Assert.Equal(targetSupplier.Phone, supplierStillPresent.Phone);
        Assert.Equal(targetSupplier.Email, supplierStillPresent.Email);
    }
}
