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

public class ManufacturingBomTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private readonly string _settingsPath = TempFile.CreateSettingsPath();
    private DatabaseService _db = null!;
    private InventoryService _inventoryService = null!;
    private ManufacturingService _mfgService = null!;
    private LanguageService _languageService = null!;
    private LocationService _locationService = null!;

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
        _mfgService = new ManufacturingService(_db, auditService);
        _languageService = new LanguageService();
        _locationService = new LocationService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
        TempFile.DeleteFile(_settingsPath);
    }

    [Fact]
    public void BomLineViewModel_ProductAutoSearch_FiltersAndSelectsCorrectly()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Wheat Flour", SKU = "RAW-01", Unit = "kg" },
            new() { Id = 2, Name = "White Sugar", SKU = "RAW-02", Unit = "kg" },
            new() { Id = 3, Name = "Cocoa Powder", SKU = "RAW-03", Unit = "g" }
        };
        var units = new List<string> { "Pcs", "kg", "g" };

        var line = new BomLineViewModel(products, units);

        // Initially no text, dropdown hidden
        Assert.False(line.IsDropdownVisible);
        Assert.Null(line.SelectedProduct);

        // Type search text
        line.ProductSearchText = "Sug";
        Assert.True(line.IsDropdownVisible);
        Assert.Single(line.MatchedProducts);
        Assert.Equal("White Sugar", line.MatchedProducts[0].Name);

        // Select the product
        line.SelectProduct(line.MatchedProducts[0]);
        Assert.NotNull(line.SelectedProduct);
        Assert.Equal("White Sugar", line.SelectedProduct.Name);
        Assert.Equal("kg", line.Unit);
        Assert.False(line.IsDropdownVisible);

        // Clear the product
        line.ClearProduct();
        Assert.Null(line.SelectedProduct);
        Assert.Equal(string.Empty, line.ProductSearchText);
        Assert.False(line.IsDropdownVisible);
    }

    [Fact]
    public async Task ManufacturingViewModel_FinalProductAutoSearch_FiltersAndSelectsCorrectly()
    {
        var p1 = new Product { Name = "Chocolate Cake 1kg", SKU = "FIN-01", Unit = "Pcs" };
        var p2 = new Product { Name = "Vanilla Cake 1kg", SKU = "FIN-02", Unit = "Pcs" };
        await _inventoryService.AddProductAsync(p1);
        await _inventoryService.AddProductAsync(p2);

        var vm = new ManufacturingViewModel(_mfgService, _inventoryService, _languageService);
        await vm.LoadBoms();
        vm.ShowCreateBomForm();

        // Search for "Vanilla"
        vm.FinalProductSearchText = "Van";
        Assert.True(vm.IsFinalProductDropdownVisible);
        Assert.Single(vm.MatchedFinalProducts);
        Assert.Equal("Vanilla Cake 1kg", vm.MatchedFinalProducts[0].Name);

        // Select it
        vm.SelectFinalProduct(vm.MatchedFinalProducts[0]);
        Assert.NotNull(vm.SelectedFinalProduct);
        Assert.Equal("Vanilla Cake 1kg", vm.SelectedFinalProduct.Name);
        Assert.False(vm.IsFinalProductDropdownVisible);

        // Clear it
        vm.ClearFinalProduct();
        Assert.Null(vm.SelectedFinalProduct);
        Assert.Equal(string.Empty, vm.FinalProductSearchText);
        Assert.False(vm.IsFinalProductDropdownVisible);
    }

    [Fact]
    public async Task ManufacturingViewModel_CreateProductOnTheFly_ForFinalProduct_SavesAndSelects()
    {
        var vm = new ManufacturingViewModel(_mfgService, _inventoryService, _languageService);
        await vm.LoadBoms();
        vm.ShowCreateBomForm();

        // User types new product name that does not exist in inventory
        vm.FinalProductSearchText = "Berry Glazed Donut";
        Assert.Null(vm.SelectedFinalProduct);

        // Clicks + New Product button
        vm.OpenCreateFinalProduct();
        Assert.True(vm.IsCreateProductModalOpen);
        Assert.Equal("Berry Glazed Donut", vm.NewProductName);
        Assert.Equal("Manufactured", vm.NewProductCategory);

        // Set attributes
        vm.NewProductCost = 800m;
        vm.NewProductPrice = 1500m;
        vm.NewProductUnit = "Pcs";

        // Save
        await vm.SaveNewProduct();

        // Modal should close
        Assert.False(vm.IsCreateProductModalOpen);
        Assert.Empty(vm.NewProductErrorMessage);

        // Product should be auto-selected as SelectedFinalProduct
        Assert.NotNull(vm.SelectedFinalProduct);
        Assert.Equal("Berry Glazed Donut", vm.SelectedFinalProduct.Name);
        Assert.Equal("Pcs", vm.SelectedFinalProduct.Unit);

        // Verify product in database
        var allProducts = await _inventoryService.GetAllProductsAsync();
        var createdInDb = allProducts.FirstOrDefault(p => p.Name == "Berry Glazed Donut");
        Assert.NotNull(createdInDb);
        Assert.Equal(800m, createdInDb.Cost);
        Assert.Equal(1500m, createdInDb.Price);
    }

    [Fact]
    public async Task ManufacturingViewModel_CreateProductOnTheFly_ForComponentLine_SavesAndSelects()
    {
        var vm = new ManufacturingViewModel(_mfgService, _inventoryService, _languageService);
        await vm.LoadBoms();
        vm.ShowCreateBomForm();

        Assert.NotEmpty(vm.ComponentLines);
        var firstLine = vm.ComponentLines[0];

        // Type new raw ingredient in component line
        firstLine.ProductSearchText = "Organic Almond Extract";
        Assert.Null(firstLine.SelectedProduct);

        // Trigger on-the-fly creation from component line
        firstLine.CreateProduct();
        Assert.True(vm.IsCreateProductModalOpen);
        Assert.Equal("Organic Almond Extract", vm.NewProductName);
        Assert.Equal("Raw Material", vm.NewProductCategory);

        vm.NewProductCost = 5000m;
        vm.NewProductUnit = "l";

        await vm.SaveNewProduct();

        // Modal closed
        Assert.False(vm.IsCreateProductModalOpen);

        // Selected in component line
        Assert.NotNull(firstLine.SelectedProduct);
        Assert.Equal("Organic Almond Extract", firstLine.SelectedProduct.Name);
        Assert.Equal("l", firstLine.Unit);

        // Add a second line and verify it also has the new product available in its list
        vm.AddComponentLine();
        var secondLine = vm.ComponentLines[1];
        Assert.Contains(secondLine.Products, p => p.Name == "Organic Almond Extract");
    }

    [Fact]
    public async Task ManufacturingViewModel_SaveBom_WithAutoMatchedExactNames_SavesSuccessfully()
    {
        // Existing raw materials in inventory
        var raw1 = new Product { Name = "Wheat Flour", SKU = "WF-01", Unit = "kg", Cost = 500m };
        var raw2 = new Product { Name = "Cane Sugar", SKU = "CS-01", Unit = "kg", Cost = 800m };
        await _inventoryService.AddProductAsync(raw1);
        await _inventoryService.AddProductAsync(raw2);

        var vm = new ManufacturingViewModel(_mfgService, _inventoryService, _languageService);
        await vm.LoadBoms();
        vm.ShowCreateBomForm();

        // Create the final product on the fly
        vm.FinalProductSearchText = "Artisan Bread";
        vm.OpenCreateFinalProduct();
        await vm.SaveNewProduct();

        // Configure component line 1 - type exact name without clicking dropdown
        vm.ComponentLines[0].ProductSearchText = "Wheat Flour";
        vm.ComponentLines[0].Quantity = 2.5;

        // Configure component line 2
        vm.AddComponentLine();
        vm.ComponentLines[1].ProductSearchText = "Cane Sugar";
        vm.ComponentLines[1].Quantity = 0.5;

        // Save BOM
        await vm.SaveBom();

        Assert.Empty(vm.ErrorMessage);
        Assert.False(vm.IsFormVisible);

        // Verify BOM exists in database
        var boms = await _mfgService.GetAllBomsAsync();
        var savedBom = boms.FirstOrDefault(b => b.ProductName == "Artisan Bread");
        Assert.NotNull(savedBom);

        var bomLines = await _mfgService.GetBomLinesAsync(savedBom.BillOfMaterial.Id);
        Assert.Equal(2, bomLines.Count);
        Assert.Contains(bomLines, l => l.Quantity == 2.5);
        Assert.Contains(bomLines, l => l.Quantity == 0.5);
    }

    [Fact]
    public async Task Manufacturing_DestinationLocation_And_SourceLocation_EndToEnd()
    {
        // 1. Setup locations
        var locSource = new Location { Name = "Raw Materials Warehouse", Type = "Warehouse", IsActive = true };
        var locDest = new Location { Name = "Finished Goods Showroom", Type = "Store", IsActive = true };
        await _locationService.AddLocationAsync(locSource);
        await _locationService.AddLocationAsync(locDest);

        var allLocs = await _locationService.GetAllLocationsAsync();
        var sourceLoc = allLocs.First(l => l.Name == "Raw Materials Warehouse");
        var destLoc = allLocs.First(l => l.Name == "Finished Goods Showroom");

        // 2. Setup raw material and finished product
        var rawIngredient = new Product 
        { 
            Name = "Organic Cocoa Powder", 
            SKU = "RAW-COCOA", 
            Unit = "kg", 
            Cost = 2000m,
            StockQuantity = 50 
        };
        await _inventoryService.AddProductAsync(rawIngredient);
        await _db.Connection.InsertAsync(new LocationStock
        {
            LocationId = sourceLoc.Id,
            ProductId = rawIngredient.Id,
            Quantity = 50
        });

        var finishedGood = new Product 
        { 
            Name = "Dark Chocolate Bar", 
            SKU = "FIN-DARKCHOC", 
            Unit = "Pcs", 
            Cost = 500m, 
            Price = 1500m, 
            DefaultLocationId = destLoc.Id 
        };
        await _inventoryService.AddProductAsync(finishedGood);

        // 3. Build BoM in ManufacturingViewModel
        var vm = new ManufacturingViewModel(_mfgService, _inventoryService, _languageService, _locationService);
        await vm.LoadBoms();
        vm.ShowCreateBomForm();

        vm.SelectFinalProduct(vm.Products.First(p => p.Id == finishedGood.Id));
        // Destination location is auto-picked from finished product's DefaultLocationId
        Assert.NotNull(vm.SelectedBomDestinationLocation);
        Assert.Equal(destLoc.Id, vm.SelectedBomDestinationLocation.Id);

        // Add component line
        vm.ComponentLines[0].SelectProduct(vm.Products.First(p => p.Id == rawIngredient.Id));
        vm.ComponentLines[0].Quantity = 2.0;

        await vm.SaveBom();

        var boms = await _mfgService.GetAllBomsAsync();
        var savedBom = boms.First(b => b.ProductName == "Dark Chocolate Bar");
        Assert.Equal(destLoc.Id, savedBom.BillOfMaterial.DestinationLocationId);
        Assert.Equal("Finished Goods Showroom", savedBom.DestinationLocationName);

        // 4. Create MO
        await vm.LoadMOs();
        vm.ShowCreateMOForm();
        vm.SelectedBoMForMO = vm.ActiveBoms.First(b => b.BillOfMaterial.Id == savedBom.BillOfMaterial.Id);

        // Destination location defaults from BoM
        Assert.NotNull(vm.SelectedMODestinationLocation);
        Assert.Equal(destLoc.Id, vm.SelectedMODestinationLocation.Id);

        // Explicitly set source location for ingredients
        vm.SelectedMOSourceLocation = vm.Locations.First(l => l.Id == sourceLoc.Id);
        vm.MOTargetQuantity = 10.0;

        await vm.ConfirmMO();

        var mos = await _mfgService.GetAllManufacturingOrdersAsync();
        var savedMo = mos.First(m => m.ProductName == "Dark Chocolate Bar");
        Assert.Equal(destLoc.Id, savedMo.ManufacturingOrder.DestinationLocationId);
        Assert.Equal(sourceLoc.Id, savedMo.ManufacturingOrder.SourceLocationId);
        Assert.Equal("Finished Goods Showroom", savedMo.DestinationLocationName);
        Assert.Equal("Raw Materials Warehouse", savedMo.SourceLocationName);

        // 5. Produce MO
        await vm.OpenMODetail(savedMo);
        vm.StartProductionInput();
        vm.MOActualQuantity = 10.0;
        await vm.RecordProduction();

        // 6. Verify stocks in database
        var rawSourceStock = (await _locationService.GetProductLocationsAsync(rawIngredient.Id))
            .FirstOrDefault(s => s.LocationId == sourceLoc.Id);
        Assert.NotNull(rawSourceStock);
        Assert.Equal(30, rawSourceStock.Quantity); // 50 - (2 * 10) = 30

        var finishedDestStock = (await _locationService.GetProductLocationsAsync(finishedGood.Id))
            .FirstOrDefault(s => s.LocationId == destLoc.Id);
        Assert.NotNull(finishedDestStock);
        Assert.Equal(10, finishedDestStock.Quantity); // 0 + 10 = 10

        // 7. Verify stock movements have correct location names
        var movements = await _db.Connection.Table<StockMovement>().ToListAsync();
        var outMovement = movements.FirstOrDefault(m => m.ProductId == rawIngredient.Id && m.MovementType == "OUT");
        Assert.NotNull(outMovement);
        Assert.Equal("Raw Materials Warehouse", outMovement.FromLocation);

        var inMovement = movements.FirstOrDefault(m => m.ProductId == finishedGood.Id && m.MovementType == "IN");
        Assert.NotNull(inMovement);
        Assert.Equal("Finished Goods Showroom", inMovement.ToLocation);
    }

    [Fact]
    public async Task InventoryViewModel_LoadsDefaultLocation_AndStockBreakdown()
    {
        var loc = new Location { Name = "Cold Storage #1", Type = "Warehouse", IsActive = true };
        await _locationService.AddLocationAsync(loc);
        var insertedLoc = (await _locationService.GetAllLocationsAsync()).First(l => l.Name == "Cold Storage #1");

        var prod = new Product
        {
            Name = "Ice Cream Tub",
            SKU = "IC-001",
            Unit = "Box",
            DefaultLocationId = insertedLoc.Id
        };
        await _inventoryService.AddProductAsync(prod);

        await _db.Connection.InsertAsync(new LocationStock
        {
            LocationId = insertedLoc.Id,
            ProductId = prod.Id,
            Quantity = 45,
            ReorderPoint = 10
        });

        var settingsService = new SettingsService(_settingsPath);
        var taxService = new TaxService(_db);
        var accountService = new AccountService(_db);
        var invVm = new InventoryViewModel(
            _inventoryService,
            new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService()),
            settingsService,
            _languageService,
            taxService,
            accountService,
            locationService: _locationService);

        await invVm.LoadProductsCommand.ExecuteAsync(null);
        await invVm.OpenEditProductPaneCommand.ExecuteAsync(prod);

        Assert.NotNull(invVm.SelectedDefaultLocation);
        Assert.Equal("Cold Storage #1", invVm.SelectedDefaultLocation.Name);
        Assert.NotEmpty(invVm.ProductLocationStocks);
        var stockRow = invVm.ProductLocationStocks.First();
        Assert.Equal("Cold Storage #1", stockRow.LocationName);
        Assert.Equal(45, stockRow.Quantity);
        Assert.Equal(10, stockRow.ReorderPoint);

        // Clear location
        invVm.ClearDefaultLocationCommand.Execute(null);
        Assert.Null(invVm.SelectedDefaultLocation);
        Assert.Null(invVm.CurrentProduct.DefaultLocationId);
    }
}
