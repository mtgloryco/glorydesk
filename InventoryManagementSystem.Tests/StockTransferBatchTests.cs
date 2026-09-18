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

public class StockTransferBatchTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private readonly string _settingsPath = TempFile.CreateSettingsPath();
    private DatabaseService _db = null!;
    private LocationService _locationService = null!;
    private InventoryService _inventoryService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var licenseService = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await licenseService.InitializeAsync();
        licenseService.CurrentLicense.Status = "Active";
        licenseService.CurrentLicense.Type = "Enterprise";
        var auditService = new AuditService(_db);
        _inventoryService = new InventoryService(_db, licenseService, auditService);
        _locationService = new LocationService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
        TempFile.DeleteFile(_settingsPath);
    }

    [Fact]
    public async Task StockTransfer_MultiProductBatch_TransfersAtomicallyAndUpdatesLocations()
    {
        // 1. Setup locations
        var mainWarehouse = new Location { Name = "Main Warehouse", Type = "Warehouse", IsActive = true };
        var retailStore = new Location { Name = "Retail Store Kigali", Type = "Store", IsActive = true };
        await _locationService.AddLocationAsync(mainWarehouse);
        await _locationService.AddLocationAsync(retailStore);
        var locs = await _locationService.GetAllLocationsAsync();
        var srcLoc = locs.First(l => l.Name == "Main Warehouse");
        var dstLoc = locs.First(l => l.Name == "Retail Store Kigali");

        // 2. Setup products and stocks at source location
        var prod1 = new Product { Name = "Basmati Rice 5kg", SKU = "RICE-5K", Unit = "Bag", Cost = 5000m, StockQuantity = 100 };
        var prod2 = new Product { Name = "Sunflower Oil 1L", SKU = "OIL-1L", Unit = "Bottle", Cost = 2500m, StockQuantity = 50 };
        var prod3 = new Product { Name = "Wheat Flour 2kg", SKU = "FLOUR-2K", Unit = "Pcs", Cost = 1800m, StockQuantity = 40 };
        await _inventoryService.AddProductAsync(prod1);
        await _inventoryService.AddProductAsync(prod2);
        await _inventoryService.AddProductAsync(prod3);

        await _db.Connection.InsertAsync(new LocationStock { LocationId = srcLoc.Id, ProductId = prod1.Id, Quantity = 100 });
        await _db.Connection.InsertAsync(new LocationStock { LocationId = srcLoc.Id, ProductId = prod2.Id, Quantity = 50 });
        await _db.Connection.InsertAsync(new LocationStock { LocationId = srcLoc.Id, ProductId = prod3.Id, Quantity = 40 });

        // 3. Initialize ViewModel and create transfer
        var vm = new StockTransferViewModel(_locationService, _inventoryService);
        await vm.LoadInitialData();

        vm.ShowNewTransferForm();
        Assert.True(vm.IsNewTransferVisible);
        Assert.StartsWith("TRF-", vm.TransferNumber);

        vm.SourceLocation = vm.Locations.First(l => l.Id == srcLoc.Id);
        vm.DestLocation = vm.Locations.First(l => l.Id == dstLoc.Id);
        vm.Notes = "Stock replenishment for Kigali store";

        // Line 1: 25 bags Rice
        Assert.Single(vm.TransferLines);
        await vm.TransferLines[0].SelectProduct(vm.Products.First(p => p.Id == prod1.Id));
        vm.TransferLines[0].Quantity = 25;
        Assert.Equal(100, vm.TransferLines[0].AvailableStock);
        Assert.False(vm.TransferLines[0].HasStockError);

        // Line 2: 15 bottles Oil
        vm.AddLine();
        Assert.Equal(2, vm.TransferLines.Count);
        await vm.TransferLines[1].SelectProduct(vm.Products.First(p => p.Id == prod2.Id));
        vm.TransferLines[1].Quantity = 15;
        Assert.Equal(50, vm.TransferLines[1].AvailableStock);
        Assert.False(vm.TransferLines[1].HasStockError);

        // Line 3: 10 bags Flour
        vm.AddLine();
        Assert.Equal(3, vm.TransferLines.Count);
        await vm.TransferLines[2].SelectProduct(vm.Products.First(p => p.Id == prod3.Id));
        vm.TransferLines[2].Quantity = 10;
        Assert.Equal(40, vm.TransferLines[2].AvailableStock);
        Assert.False(vm.TransferLines[2].HasStockError);

        Assert.Equal(3, vm.TotalLinesCount);
        Assert.Equal(50, vm.TotalUnitsCount); // 25 + 15 + 10 = 50

        // 4. Perform transfer
        await vm.PerformTransfer();

        Assert.Empty(vm.ErrorMessage);
        Assert.NotEmpty(vm.SuccessMessage);
        Assert.False(vm.IsNewTransferVisible);

        // 5. Verify source stock deducted
        Assert.Equal(75, await _locationService.GetProductStockAtLocationAsync(srcLoc.Id, prod1.Id)); // 100 - 25
        Assert.Equal(35, await _locationService.GetProductStockAtLocationAsync(srcLoc.Id, prod2.Id)); // 50 - 15
        Assert.Equal(30, await _locationService.GetProductStockAtLocationAsync(srcLoc.Id, prod3.Id)); // 40 - 10

        // 6. Verify destination stock added
        Assert.Equal(25, await _locationService.GetProductStockAtLocationAsync(dstLoc.Id, prod1.Id));
        Assert.Equal(15, await _locationService.GetProductStockAtLocationAsync(dstLoc.Id, prod2.Id));
        Assert.Equal(10, await _locationService.GetProductStockAtLocationAsync(dstLoc.Id, prod3.Id));

        // 7. Verify stock movements contain FromLocation and ToLocation labels
        var movements = (await _db.Connection.Table<StockMovement>().ToListAsync())
            .Where(m => m.Reason.StartsWith("Stock Transfer"))
            .ToList();
        Assert.Equal(6, movements.Count); // 3 OUT, 3 IN

        var outMovements = movements.Where(m => m.QuantityChanged < 0).ToList();
        Assert.All(outMovements, m =>
        {
            Assert.Equal("Main Warehouse", m.FromLocation);
            Assert.Equal("Retail Store Kigali", m.ToLocation);
        });

        var inMovements = movements.Where(m => m.QuantityChanged > 0).ToList();
        Assert.All(inMovements, m =>
        {
            Assert.Equal("Main Warehouse", m.FromLocation);
            Assert.Equal("Retail Store Kigali", m.ToLocation);
        });

        // 8. Verify history records
        var history = await _locationService.GetAllStockTransfersAsync();
        Assert.Equal(3, history.Count);
        Assert.All(history, h =>
        {
            Assert.Equal("Main Warehouse", h.FromLocationName);
            Assert.Equal("Retail Store Kigali", h.ToLocationName);
            Assert.Equal("Completed", h.Status);
        });
    }

    [Fact]
    public async Task StockTransfer_ExceedingAvailableStock_IsBlockedWithClearError()
    {
        // 1. Setup locations & product
        var loc1 = new Location { Name = "Loc A", Type = "Warehouse", IsActive = true };
        var loc2 = new Location { Name = "Loc B", Type = "Store", IsActive = true };
        await _locationService.AddLocationAsync(loc1);
        await _locationService.AddLocationAsync(loc2);

        var prod = new Product { Name = "Limited Sugar", SKU = "SUGAR-LIM", StockQuantity = 8 };
        await _inventoryService.AddProductAsync(prod);

        var locs = await _locationService.GetAllLocationsAsync();
        var src = locs.First(l => l.Name == "Loc A");
        var dst = locs.First(l => l.Name == "Loc B");
        await _db.Connection.InsertAsync(new LocationStock { LocationId = src.Id, ProductId = prod.Id, Quantity = 8 });

        // 2. Setup ViewModel
        var vm = new StockTransferViewModel(_locationService, _inventoryService);
        await vm.LoadInitialData();
        vm.ShowNewTransferForm();

        vm.SourceLocation = src;
        vm.DestLocation = dst;

        // Try to transfer 20 (only 8 available!)
        await vm.TransferLines[0].SelectProduct(vm.Products.First(p => p.Id == prod.Id));
        vm.TransferLines[0].Quantity = 20;

        Assert.Equal(8, vm.TransferLines[0].AvailableStock);
        Assert.True(vm.TransferLines[0].HasStockError);
        Assert.Contains("Exceeds", vm.TransferLines[0].StockStatusText);

        // 3. Try to execute
        await vm.PerformTransfer();

        // Execution blocked
        Assert.NotEmpty(vm.ErrorMessage);
        Assert.Contains("Only 8 available", vm.ErrorMessage);
        Assert.True(vm.IsNewTransferVisible);

        // Stock unchanged
        Assert.Equal(8, await _locationService.GetProductStockAtLocationAsync(src.Id, prod.Id));
        Assert.Equal(0, await _locationService.GetProductStockAtLocationAsync(dst.Id, prod.Id));
    }

    [Fact]
    public async Task StockTransfer_SameSourceAndDestination_IsBlocked()
    {
        var loc = new Location { Name = "Central Hub", Type = "Warehouse", IsActive = true };
        await _locationService.AddLocationAsync(loc);
        var inserted = (await _locationService.GetAllLocationsAsync()).First();

        var prod = new Product { Name = "Widget", SKU = "WDG-1", StockQuantity = 20 };
        await _inventoryService.AddProductAsync(prod);
        await _db.Connection.InsertAsync(new LocationStock { LocationId = inserted.Id, ProductId = prod.Id, Quantity = 20 });

        var vm = new StockTransferViewModel(_locationService, _inventoryService);
        await vm.LoadInitialData();
        vm.ShowNewTransferForm();

        vm.SourceLocation = inserted;
        vm.DestLocation = inserted; // Same location!
        await vm.TransferLines[0].SelectProduct(vm.Products.First(p => p.Id == prod.Id));
        vm.TransferLines[0].Quantity = 5;

        await vm.PerformTransfer();

        Assert.NotEmpty(vm.ErrorMessage);
        Assert.Contains("must be different", vm.ErrorMessage);
    }

    [Fact]
    public void InventoryViewModel_StockTransferCommand_InvokesNavigationAction()
    {
        bool navigatedToTransfer = false;
        var vm = new InventoryViewModel(
            _inventoryService,
            new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService()),
            new SettingsService(_settingsPath),
            new LanguageService(),
            new TaxService(_db),
            new AccountService(_db),
            goToStockTransfer: () => navigatedToTransfer = true);

        // User clicks "Stock Transfer" under Stock Tools menu
        vm.GoToStockTransferScreenCommand.Execute(null);

        Assert.True(navigatedToTransfer);
    }
}
