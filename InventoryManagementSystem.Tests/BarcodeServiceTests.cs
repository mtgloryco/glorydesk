using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class BarcodeServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private BarcodeService _barcodeService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _barcodeService = new BarcodeService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task FindProductByBarcodeAsync_MatchesDedicatedBarcodeField()
    {
        var product = new Product { Name = "Scannable Widget", SKU = "SKU-1", Barcode = "6001234567890", ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        var found = await _barcodeService.FindProductByBarcodeAsync("6001234567890");

        Assert.NotNull(found);
        Assert.Equal(product.Id, found!.Id);
    }

    [Fact]
    public async Task FindProductByBarcodeAsync_FallsBackToSkuWhenBarcodeNotSet()
    {
        var product = new Product { Name = "Legacy Widget", SKU = "LEGACY-1", ProductType = "Good" };
        await _db.Connection.InsertAsync(product);

        var found = await _barcodeService.FindProductByBarcodeAsync("LEGACY-1");

        Assert.NotNull(found);
        Assert.Equal(product.Id, found!.Id);
    }

    [Fact]
    public async Task FindProductByBarcodeAsync_PrefersBarcodeOverSkuOnConflict()
    {
        // A product whose SKU happens to equal another product's barcode - Barcode match must win.
        var byBarcode = new Product { Name = "Real Match", SKU = "SKU-A", Barcode = "999", ProductType = "Good" };
        var byStaleSku = new Product { Name = "Stale SKU Coincidence", SKU = "999", ProductType = "Good" };
        await _db.Connection.InsertAsync(byBarcode);
        await _db.Connection.InsertAsync(byStaleSku);

        var found = await _barcodeService.FindProductByBarcodeAsync("999");

        Assert.NotNull(found);
        Assert.Equal(byBarcode.Id, found!.Id);
    }
}
