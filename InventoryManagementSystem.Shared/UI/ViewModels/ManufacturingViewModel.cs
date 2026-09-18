using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class ManufacturingViewModel : ViewModelBase
    {
        private readonly ManufacturingService _manufacturingService;
        private readonly InventoryService _inventoryService;
        private readonly LocationService? _locationService;
        
        public LanguageService Language { get; }

        private List<BillOfMaterialListItem> _allBomsList = new();
        private List<ManufacturingOrderListItem> _allMOsList = new();
        private bool _isLoadingDetail;

        [ObservableProperty]
        private int _selectedTabIndex;

        // ==================== TABS SHARED ====================
        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private ObservableCollection<Location> _locations = new();

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public List<string> Units { get; } = new() { "Pcs", "Box", "g", "kg", "l", "Per Unit" };

        // ==================== TAB 1: BoM PROPERTIES ====================
        [ObservableProperty]
        private ObservableCollection<BillOfMaterialListItem> _boms = new();

        [ObservableProperty]
        private BillOfMaterialListItem? _selectedBom;

        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private string _searchText = string.Empty;

        // BoM Form Fields
        private BillOfMaterial? _editingBom;

        [ObservableProperty]
        private Product? _selectedFinalProduct;

        [ObservableProperty]
        private string _finalProductSearchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Product> _matchedFinalProducts = new();

        public bool IsFinalProductDropdownVisible => SelectedFinalProduct == null && !string.IsNullOrWhiteSpace(FinalProductSearchText) && MatchedFinalProducts.Count > 0;

        // On-the-fly product creation fields
        [ObservableProperty]
        private bool _isCreateProductModalOpen;

        [ObservableProperty]
        private string _newProductName = string.Empty;

        [ObservableProperty]
        private decimal _newProductCost;

        [ObservableProperty]
        private decimal _newProductPrice;

        [ObservableProperty]
        private string _newProductUnit = "Pcs";

        [ObservableProperty]
        private string _newProductCategory = "Manufactured";

        [ObservableProperty]
        private string _newProductErrorMessage = string.Empty;

        private bool _isCreatingFinalProduct;
        private BomLineViewModel? _targetComponentLine;

        [ObservableProperty]
        private double _quantity = 1.0;

        [ObservableProperty]
        private string _reference = string.Empty;

        [ObservableProperty]
        private string _selectedBomType = "Manufacture this product";

        [ObservableProperty]
        private string _company = "My Company";

        [ObservableProperty]
        private double _yieldPercent = 100.0;

        [ObservableProperty]
        private double _scrapPercent = 0.0;

        [ObservableProperty]
        private Location? _selectedBomDestinationLocation;

        [RelayCommand]
        public void ClearBomDestinationLocation() => SelectedBomDestinationLocation = null;

        [ObservableProperty]
        private ObservableCollection<BomLineViewModel> _componentLines = new();

        public List<string> BomTypes { get; } = new() { "Manufacture this product", "Kit" };

        public bool IsManufactureType
        {
            get => SelectedBomType == "Manufacture this product";
            set
            {
                if (value)
                {
                    SelectedBomType = "Manufacture this product";
                    OnPropertyChanged(nameof(IsManufactureType));
                    OnPropertyChanged(nameof(IsKitType));
                }
            }
        }

        public bool IsKitType
        {
            get => SelectedBomType == "Kit";
            set
            {
                if (value)
                {
                    SelectedBomType = "Kit";
                    OnPropertyChanged(nameof(IsManufactureType));
                    OnPropertyChanged(nameof(IsKitType));
                }
            }
        }

        // ==================== TAB 2: MO PROPERTIES ====================
        [ObservableProperty]
        private ObservableCollection<ManufacturingOrderListItem> _manufacturingOrders = new();

        [ObservableProperty]
        private ManufacturingOrderListItem? _selectedMO;

        [ObservableProperty]
        private bool _isMOFormVisible;

        [ObservableProperty]
        private string _searchMOText = string.Empty;

        // MO Form Fields
        private ManufacturingOrder? _editingMO;

        [ObservableProperty]
        private Product? _mOProduct;

        [ObservableProperty]
        private BillOfMaterialListItem? _selectedBoMForMO;

        [ObservableProperty]
        private double _mOTargetQuantity = 1.0;

        [ObservableProperty]
        private double _mOActualQuantity = 1.0;

        [ObservableProperty]
        private string _mONumber = string.Empty;

        [ObservableProperty]
        private string _mOStatus = "Draft";

        public bool IsDraftState => MOStatus == "Draft" || string.IsNullOrEmpty(MOStatus);
        public bool IsConfirmedState => MOStatus == "Confirmed";
        public bool IsDoneState => MOStatus == "Done";

        partial void OnMOStatusChanged(string value)
        {
            OnPropertyChanged(nameof(IsDraftState));
            OnPropertyChanged(nameof(IsConfirmedState));
            OnPropertyChanged(nameof(IsDoneState));
        }

        [ObservableProperty]
        private string _mOCompany = "My Company";

        [ObservableProperty]
        private ObservableCollection<MoLineViewModel> _mOComponentLines = new();

        [ObservableProperty]
        private bool _isProduceStateActive; // True when user clicked "Produce" and is editing final actual yield

        [ObservableProperty]
        private ObservableCollection<BillOfMaterialListItem> _activeBoms = new();

        [ObservableProperty]
        private Location? _selectedMODestinationLocation;

        [ObservableProperty]
        private Location? _selectedMOSourceLocation;

        [RelayCommand]
        public void ClearMODestinationLocation() => SelectedMODestinationLocation = null;

        [RelayCommand]
        public void ClearMOSourceLocation() => SelectedMOSourceLocation = null;

        // ==================== TAB 3: REPORTING PROPERTIES ====================
        [ObservableProperty]
        private int _totalMOsCount;

        [ObservableProperty]
        private int _completedMOsCount;

        [ObservableProperty]
        private decimal _totalProductionCost;

        [ObservableProperty]
        private ObservableCollection<ManufacturingOrderListItem> _reportOrders = new();

        // ==================== CONSTRUCTOR ====================
        public ManufacturingViewModel(
            ManufacturingService manufacturingService,
            InventoryService inventoryService,
            LanguageService languageService,
            LocationService? locationService = null)
        {
            _manufacturingService = manufacturingService;
            _inventoryService = inventoryService;
            Language = languageService;
            _locationService = locationService;

            LoadBomsCommand.Execute(null);
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            ErrorMessage = string.Empty;
            if (value == 0)
            {
                LoadBomsCommand.Execute(null);
            }
            else if (value == 1)
            {
                LoadMOsCommand.Execute(null);
            }
            else if (value == 2)
            {
                LoadReportCommand.Execute(null);
            }
        }

        // ==================== TAB 1: BoM METHODS ====================
        [RelayCommand]
        public async Task LoadBoms()
        {
            try
            {
                var list = await _manufacturingService.GetAllBomsAsync();
                _allBomsList = list;
                FilterBoms();

                var productList = await _inventoryService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(productList.OrderBy(p => p.Name));
                FilterFinalProducts();

                var locList = _locationService != null 
                    ? await _locationService.GetAllLocationsAsync() 
                    : await _manufacturingService.GetAllLocationsAsync();
                Locations = new ObservableCollection<Location>(locList.OrderBy(l => l.Name));
                
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading BoMs: {ex.Message}";
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterBoms();
        }

        private void FilterBoms()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Boms = new ObservableCollection<BillOfMaterialListItem>(_allBomsList);
            }
            else
            {
                var query = SearchText.ToLower();
                var filtered = _allBomsList.Where(b => 
                    b.ProductName.ToLower().Contains(query) || 
                    b.Reference.ToLower().Contains(query) ||
                    b.BomType.ToLower().Contains(query) ||
                    b.Company.ToLower().Contains(query)
                ).ToList();
                Boms = new ObservableCollection<BillOfMaterialListItem>(filtered);
            }
        }

        partial void OnSelectedFinalProductChanged(Product? value)
        {
            if (value != null && FinalProductSearchText != value.Name)
            {
                FinalProductSearchText = value.Name;
            }
            if (value != null && SelectedBomDestinationLocation == null && value.DefaultLocationId.HasValue)
            {
                SelectedBomDestinationLocation = Locations.FirstOrDefault(l => l.Id == value.DefaultLocationId.Value);
            }
            OnPropertyChanged(nameof(IsFinalProductDropdownVisible));
        }

        partial void OnFinalProductSearchTextChanged(string value)
        {
            if (SelectedFinalProduct != null && value != SelectedFinalProduct.Name)
            {
                SelectedFinalProduct = null;
            }
            FilterFinalProducts();
            OnPropertyChanged(nameof(IsFinalProductDropdownVisible));
        }

        private void FilterFinalProducts()
        {
            if (Products == null || Products.Count == 0)
            {
                MatchedFinalProducts = new ObservableCollection<Product>();
                return;
            }

            if (string.IsNullOrWhiteSpace(FinalProductSearchText) || (SelectedFinalProduct != null && FinalProductSearchText == SelectedFinalProduct.Name))
            {
                MatchedFinalProducts = new ObservableCollection<Product>(Products.Take(8));
                return;
            }

            var query = FinalProductSearchText.Trim().ToLower();
            var matches = Products.Where(p => 
                (p.Name != null && p.Name.ToLower().Contains(query)) || 
                (p.SKU != null && p.SKU.ToLower().Contains(query))
            ).Take(8).ToList();
            MatchedFinalProducts = new ObservableCollection<Product>(matches);
        }

        [RelayCommand]
        public void SelectFinalProduct(Product? product)
        {
            SelectedFinalProduct = product;
            if (product != null)
            {
                FinalProductSearchText = product.Name;
            }
            FilterFinalProducts();
            OnPropertyChanged(nameof(IsFinalProductDropdownVisible));
        }

        [RelayCommand]
        public void ClearFinalProduct()
        {
            SelectedFinalProduct = null;
            FinalProductSearchText = string.Empty;
            FilterFinalProducts();
            OnPropertyChanged(nameof(IsFinalProductDropdownVisible));
        }

        [RelayCommand]
        public void OpenCreateFinalProduct()
        {
            _isCreatingFinalProduct = true;
            _targetComponentLine = null;
            NewProductName = FinalProductSearchText?.Trim() ?? string.Empty;
            NewProductCost = 0;
            NewProductPrice = 0;
            NewProductUnit = "Pcs";
            NewProductCategory = "Manufactured";
            NewProductErrorMessage = string.Empty;
            IsCreateProductModalOpen = true;
        }

        [RelayCommand]
        public void OpenCreateComponentProduct(BomLineViewModel line)
        {
            _isCreatingFinalProduct = false;
            _targetComponentLine = line;
            NewProductName = line.ProductSearchText?.Trim() ?? string.Empty;
            NewProductCost = 0;
            NewProductPrice = 0;
            NewProductUnit = "Pcs";
            NewProductCategory = "Raw Material";
            NewProductErrorMessage = string.Empty;
            IsCreateProductModalOpen = true;
        }

        [RelayCommand]
        public async Task SaveNewProduct()
        {
            NewProductErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(NewProductName))
            {
                NewProductErrorMessage = "Product name is required.";
                return;
            }

            try
            {
                var p = new Product
                {
                    Name = NewProductName.Trim(),
                    Cost = NewProductCost,
                    Price = NewProductPrice,
                    SKU = $"AUTO-{DateTime.Now.Ticks % 100000}",
                    Unit = string.IsNullOrWhiteSpace(NewProductUnit) ? "Pcs" : NewProductUnit.Trim(),
                    Category = string.IsNullOrWhiteSpace(NewProductCategory) ? "General" : NewProductCategory.Trim(),
                    CanBeSold = true,
                    CanBePurchased = true
                };
                await _inventoryService.AddProductAsync(p);

                var productList = await _inventoryService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(productList.OrderBy(prod => prod.Name));

                foreach (var line in ComponentLines)
                {
                    line.UpdateProducts(Products);
                }

                FilterFinalProducts();

                var created = Products.FirstOrDefault(prod => prod.Id == p.Id) ?? p;

                if (_isCreatingFinalProduct)
                {
                    SelectFinalProduct(created);
                }
                else if (_targetComponentLine != null)
                {
                    _targetComponentLine.SelectProduct(created);
                }

                IsCreateProductModalOpen = false;
            }
            catch (Exception ex)
            {
                NewProductErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        public void CancelCreateProduct()
        {
            IsCreateProductModalOpen = false;
            NewProductErrorMessage = string.Empty;
        }

        [RelayCommand]
        public void ShowCreateBomForm()
        {
            _editingBom = null;
            SelectedFinalProduct = null;
            FinalProductSearchText = string.Empty;
            SelectedBomDestinationLocation = null;
            FilterFinalProducts();
            Quantity = 1.0;
            Reference = $"BOM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            SelectedBomType = "Manufacture this product";
            OnPropertyChanged(nameof(IsManufactureType));
            OnPropertyChanged(nameof(IsKitType));
            Company = "My Company";
            ComponentLines.Clear();
            ErrorMessage = string.Empty;

            AddComponentLine();

            IsFormVisible = true;
        }

        [RelayCommand]
        public async Task OpenBomDetail(BillOfMaterialListItem item)
        {
            if (item == null) return;

            try
            {
                _editingBom = item.BillOfMaterial;
                SelectedFinalProduct = Products.FirstOrDefault(p => p.Id == _editingBom.ProductId);
                FinalProductSearchText = SelectedFinalProduct?.Name ?? string.Empty;
                SelectedBomDestinationLocation = Locations.FirstOrDefault(l => l.Id == _editingBom.DestinationLocationId);
                FilterFinalProducts();
                Quantity = _editingBom.Quantity;
                Reference = _editingBom.Reference;
                SelectedBomType = _editingBom.BomType;
                OnPropertyChanged(nameof(IsManufactureType));
                OnPropertyChanged(nameof(IsKitType));
                Company = _editingBom.Company;
                YieldPercent = _editingBom.YieldPercent;
                ScrapPercent = _editingBom.ScrapPercent;

                ComponentLines.Clear();
                var lines = await _manufacturingService.GetBomLinesAsync(_editingBom.Id);
                foreach (var line in lines)
                {
                    var product = Products.FirstOrDefault(p => p.Id == line.ProductId);
                    var lineVm = new BomLineViewModel(Products, Units)
                    {
                        SelectedProduct = product,
                        ProductSearchText = product?.Name ?? string.Empty,
                        Quantity = line.Quantity,
                        Unit = line.Unit,
                        ScrapPercent = line.ScrapPercent
                    };
                    lineVm.RequestCreateProduct = OpenCreateComponentProduct;
                    ComponentLines.Add(lineVm);
                }

                IsFormVisible = true;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading BoM details: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddComponentLine()
        {
            var line = new BomLineViewModel(Products, Units);
            line.RequestCreateProduct = OpenCreateComponentProduct;
            ComponentLines.Add(line);
        }

        [RelayCommand]
        public void RemoveComponentLine(BomLineViewModel line)
        {
            if (ComponentLines.Contains(line))
            {
                ComponentLines.Remove(line);
            }
        }

        [RelayCommand]
        public void CloseForm()
        {
            IsFormVisible = false;
            _editingBom = null;
            SelectedFinalProduct = null;
            FinalProductSearchText = string.Empty;
            ComponentLines.Clear();
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task SaveBom()
        {
            if (SelectedFinalProduct == null && !string.IsNullOrWhiteSpace(FinalProductSearchText))
            {
                var exact = Products.FirstOrDefault(p => string.Equals(p.Name, FinalProductSearchText.Trim(), StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    SelectFinalProduct(exact);
                }
            }

            if (SelectedFinalProduct == null)
            {
                ErrorMessage = "Please select a final manufactured product.";
                return;
            }

            if (Quantity <= 0)
            {
                ErrorMessage = "Quantity must be greater than zero.";
                return;
            }

            if (ComponentLines.Count == 0)
            {
                ErrorMessage = "Please add at least one component to the Bill of Materials.";
                return;
            }

            foreach (var line in ComponentLines)
            {
                if (line.SelectedProduct == null && !string.IsNullOrWhiteSpace(line.ProductSearchText))
                {
                    var exact = Products.FirstOrDefault(p => string.Equals(p.Name, line.ProductSearchText.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (exact != null)
                    {
                        line.SelectProduct(exact);
                    }
                }

                if (line.SelectedProduct == null)
                {
                    ErrorMessage = "Please ensure all component rows have a selected product.";
                    return;
                }
                if (line.Quantity <= 0)
                {
                    ErrorMessage = "Please ensure all component rows have quantity greater than zero.";
                    return;
                }
            }

            try
            {
                var bom = _editingBom ?? new BillOfMaterial();
                bom.ProductId = SelectedFinalProduct.Id;
                bom.Quantity = Quantity;
                bom.Reference = Reference;
                bom.BomType = SelectedBomType;
                bom.Company = Company;
                bom.YieldPercent = YieldPercent;
                bom.ScrapPercent = ScrapPercent;
                bom.DestinationLocationId = SelectedBomDestinationLocation?.Id;

                var lines = ComponentLines.Select(cl => new BillOfMaterialLine
                {
                    ProductId = cl.SelectedProduct!.Id,
                    Quantity = cl.Quantity,
                    Unit = cl.Unit,
                    ScrapPercent = cl.ScrapPercent
                }).ToList();

                await _manufacturingService.SaveBomAsync(bom, lines);
                await LoadBoms();
                
                IsFormVisible = false;
                _editingBom = null;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save BoM: {ex.Message}";
            }
        }

        // ==================== TAB 2: MO METHODS ====================
        [RelayCommand]
        public async Task LoadMOs()
        {
            try
            {
                var list = await _manufacturingService.GetAllManufacturingOrdersAsync();
                _allMOsList = list;
                FilterMOs();

                var bomsList = await _manufacturingService.GetAllBomsAsync();
                ActiveBoms = new ObservableCollection<BillOfMaterialListItem>(bomsList);

                var productList = await _inventoryService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(productList.OrderBy(p => p.Name));

                var locList = _locationService != null 
                    ? await _locationService.GetAllLocationsAsync() 
                    : await _manufacturingService.GetAllLocationsAsync();
                Locations = new ObservableCollection<Location>(locList.OrderBy(l => l.Name));

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading MOs: {ex.Message}";
            }
        }

        partial void OnSearchMOTextChanged(string value)
        {
            FilterMOs();
        }

        private void FilterMOs()
        {
            if (string.IsNullOrWhiteSpace(SearchMOText))
            {
                ManufacturingOrders = new ObservableCollection<ManufacturingOrderListItem>(_allMOsList);
            }
            else
            {
                var query = SearchMOText.ToLower();
                var filtered = _allMOsList.Where(o => 
                    o.MONumber.ToLower().Contains(query) || 
                    o.ProductName.ToLower().Contains(query) ||
                    o.Status.ToLower().Contains(query) ||
                    o.Company.ToLower().Contains(query)
                ).ToList();
                ManufacturingOrders = new ObservableCollection<ManufacturingOrderListItem>(filtered);
            }
        }

        private Task? _populatingComponentsTask;
        private int _populateCounter;

        partial void OnSelectedBoMForMOChanged(BillOfMaterialListItem? value)
        {
            if (_isLoadingDetail) return;

            if (value != null)
            {
                MOProduct = Products.FirstOrDefault(p => p.Id == value.BillOfMaterial.ProductId);
                if (SelectedMODestinationLocation == null)
                {
                    SelectedMODestinationLocation = Locations.FirstOrDefault(l => l.Id == value.BillOfMaterial.DestinationLocationId)
                        ?? Locations.FirstOrDefault(l => l.Id == MOProduct?.DefaultLocationId);
                }
                MOTargetQuantity = value.BillOfMaterial.Quantity;
                MOActualQuantity = value.BillOfMaterial.Quantity;
            }
        }

        partial void OnMOTargetQuantityChanged(double value)
        {
            if (_isLoadingDetail) return;

            if (SelectedBoMForMO != null && value > 0 && IsDraftState)
            {
                MOActualQuantity = value;
                _populatingComponentsTask = PopulateMOComponentsFromBoMAsync(SelectedBoMForMO.BillOfMaterial.Id);
            }
        }

        private async Task PopulateMOComponentsFromBoMAsync(int bomId)
        {
            var myCounter = ++_populateCounter;
            try
            {
                var lines = await _manufacturingService.BuildExpectedLinesFromBomAsync(bomId, MOTargetQuantity);
                if (myCounter != _populateCounter)
                {
                    return;
                }

                MOComponentLines.Clear();
                foreach (var line in lines)
                {
                    var product = Products.FirstOrDefault(p => p.Id == line.ProductId);
                    MOComponentLines.Add(new MoLineViewModel
                    {
                        ProductId = line.ProductId,
                        ProductName = product?.Name ?? "Unknown Product",
                        ExpectedQuantity = line.ExpectedQuantity,
                        ActualQuantity = line.ActualQuantity,
                        Unit = line.Unit,
                        UnitCost = line.UnitCost
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load BoM components: {ex.Message}";
            }
        }

        partial void OnMOActualQuantityChanged(double value)
        {
            // Linearly scale ingredient components based on the yield
            if (MOTargetQuantity > 0 && MOComponentLines.Count > 0)
            {
                var ratio = value / MOTargetQuantity;
                foreach (var line in MOComponentLines)
                {
                    line.ActualQuantity = Math.Round(line.ExpectedQuantity * ratio, 4);
                }
            }
        }

        [RelayCommand]
        public void ShowCreateMOForm()
        {
            _editingMO = null;
            SelectedBoMForMO = null;
            MOProduct = null;
            SelectedMODestinationLocation = null;
            SelectedMOSourceLocation = null;
            MOTargetQuantity = 1.0;
            MOActualQuantity = 1.0;
            MONumber = $"MO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            MOStatus = "Draft";
            MOCompany = "My Company";
            MOComponentLines.Clear();
            IsProduceStateActive = false;
            ErrorMessage = string.Empty;

            IsMOFormVisible = true;
        }

        [RelayCommand]
        public async Task OpenMODetail(ManufacturingOrderListItem item)
        {
            if (item == null) return;

            try
            {
                _isLoadingDetail = true;
                _editingMO = item.ManufacturingOrder;
                MOProduct = Products.FirstOrDefault(p => p.Id == _editingMO.ProductId);
                
                // Set BoM selection
                SelectedBoMForMO = ActiveBoms.FirstOrDefault(b => b.BillOfMaterial.Id == _editingMO.BomId);
                SelectedMODestinationLocation = Locations.FirstOrDefault(l => l.Id == _editingMO.DestinationLocationId)
                    ?? Locations.FirstOrDefault(l => l.Id == SelectedBoMForMO?.BillOfMaterial.DestinationLocationId)
                    ?? Locations.FirstOrDefault(l => l.Id == MOProduct?.DefaultLocationId);
                SelectedMOSourceLocation = Locations.FirstOrDefault(l => l.Id == _editingMO.SourceLocationId);
                
                MOTargetQuantity = _editingMO.TargetQuantity;
                MOActualQuantity = _editingMO.Status == "Done" ? _editingMO.ActualQuantity : _editingMO.TargetQuantity;
                MONumber = _editingMO.MONumber;
                MOStatus = _editingMO.Status;
                MOCompany = _editingMO.Company;
                IsProduceStateActive = false;

                MOComponentLines.Clear();
                var lines = await _manufacturingService.GetManufacturingOrderLinesAsync(_editingMO.Id);
                foreach (var line in lines)
                {
                    var product = Products.FirstOrDefault(p => p.Id == line.ProductId);
                    MOComponentLines.Add(new MoLineViewModel
                    {
                        ProductId = line.ProductId,
                        ProductName = product?.Name ?? "Unknown Product",
                        ExpectedQuantity = line.ExpectedQuantity,
                        ActualQuantity = _editingMO.Status == "Done" ? line.ActualQuantity : line.ExpectedQuantity,
                        Unit = line.Unit,
                        UnitCost = line.UnitCost > 0 ? line.UnitCost : (product?.Cost ?? 0m)
                    });
                }

                IsMOFormVisible = true;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading MO details: {ex.Message}";
            }
            finally
            {
                _isLoadingDetail = false;
            }
        }

        private bool _isSaving;

        [RelayCommand]
        public async Task SaveMODraft()
        {
            if (_isSaving) return;
            _isSaving = true;

            if (SelectedBoMForMO == null)
            {
                ErrorMessage = "Please select a Bill of Materials recipe.";
                _isSaving = false;
                return;
            }

            if (MOTargetQuantity <= 0)
            {
                ErrorMessage = "Target quantity must be greater than zero.";
                _isSaving = false;
                return;
            }

            try
            {
                if (_populatingComponentsTask != null)
                {
                    await _populatingComponentsTask;
                }
                if (MOComponentLines.Count == 0 && SelectedBoMForMO != null)
                {
                    await PopulateMOComponentsFromBoMAsync(SelectedBoMForMO.BillOfMaterial.Id);
                }

                var mo = _editingMO ?? new ManufacturingOrder();
                mo.MONumber = MONumber;
                mo.BomId = SelectedBoMForMO!.BillOfMaterial.Id;
                mo.ProductId = MOProduct?.Id ?? 0;
                mo.TargetQuantity = MOTargetQuantity;
                mo.DestinationLocationId = SelectedMODestinationLocation?.Id;
                mo.SourceLocationId = SelectedMOSourceLocation?.Id;
                mo.Status = "Draft";
                mo.Company = MOCompany;
                mo.OrderDate = DateTime.Now;

                var lines = MOComponentLines.Select(cl => new ManufacturingOrderLine
                {
                    ProductId = cl.ProductId,
                    ExpectedQuantity = cl.ExpectedQuantity,
                    ActualQuantity = cl.ExpectedQuantity, // default same as expected
                    Unit = cl.Unit,
                    UnitCost = cl.UnitCost
                }).ToList();

                await _manufacturingService.SaveManufacturingOrderAsync(mo, lines);
                _editingMO = mo;
                await LoadMOs();

                IsMOFormVisible = false;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save Manufacturing Order: {ex.Message}";
            }
            finally
            {
                _isSaving = false;
            }
        }

        [RelayCommand]
        public async Task ConfirmMO()
        {
            if (_isSaving) return;
            _isSaving = true;

            if (SelectedBoMForMO == null)
            {
                ErrorMessage = "Please select a Bill of Materials recipe.";
                _isSaving = false;
                return;
            }

            if (MOTargetQuantity <= 0)
            {
                ErrorMessage = "Target quantity must be greater than zero.";
                _isSaving = false;
                return;
            }

            try
            {
                if (_populatingComponentsTask != null)
                {
                    await _populatingComponentsTask;
                }
                if (MOComponentLines.Count == 0 && SelectedBoMForMO != null)
                {
                    await PopulateMOComponentsFromBoMAsync(SelectedBoMForMO.BillOfMaterial.Id);
                }

                // Auto-save first as a draft to ensure it exists in the database
                var mo = _editingMO ?? new ManufacturingOrder();
                mo.MONumber = MONumber;
                mo.BomId = SelectedBoMForMO!.BillOfMaterial.Id;
                mo.ProductId = MOProduct?.Id ?? 0;
                mo.TargetQuantity = MOTargetQuantity;
                mo.DestinationLocationId = SelectedMODestinationLocation?.Id;
                mo.SourceLocationId = SelectedMOSourceLocation?.Id;
                mo.Status = "Draft";
                mo.Company = MOCompany;
                if (mo.Id == 0) mo.OrderDate = DateTime.Now;

                var lines = MOComponentLines.Select(cl => new ManufacturingOrderLine
                {
                    ProductId = cl.ProductId,
                    ExpectedQuantity = cl.ExpectedQuantity,
                    ActualQuantity = cl.ExpectedQuantity,
                    Unit = cl.Unit,
                    UnitCost = cl.UnitCost
                }).ToList();

                await _manufacturingService.SaveManufacturingOrderAsync(mo, lines);
                _editingMO = mo;

                // Confirm the order in database
                await _manufacturingService.ConfirmManufacturingOrderAsync(_editingMO.Id);
                await LoadMOs();
                
                // Reload MO details in the same view (so the form stays open and changes state)
                var item = ManufacturingOrders.FirstOrDefault(o => o.ManufacturingOrder.Id == _editingMO.Id);
                if (item != null)
                {
                    await OpenMODetail(item);
                }
                
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to confirm MO: {ex.Message}";
            }
            finally
            {
                _isSaving = false;
            }
        }

        [RelayCommand]
        public void StartProductionInput()
        {
            // Prompt the actual produced quantity edit and ingredients adjustments
            MOActualQuantity = MOTargetQuantity;
            IsProduceStateActive = true;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task RecordProduction()
        {
            if (_editingMO == null) return;

            if (MOActualQuantity < 0)
            {
                ErrorMessage = "Actual quantity cannot be negative.";
                return;
            }

            foreach (var line in MOComponentLines)
            {
                if (line.ActualQuantity < 0)
                {
                    ErrorMessage = "Ingredient actual quantities cannot be negative.";
                    return;
                }
            }

            try
            {
                var lines = MOComponentLines.Select(cl => new ManufacturingOrderLine
                {
                    ProductId = cl.ProductId,
                    ExpectedQuantity = cl.ExpectedQuantity,
                    ActualQuantity = cl.ActualQuantity,
                    Unit = cl.Unit,
                    UnitCost = cl.UnitCost
                }).ToList();

                // Save in service (updates stock, creates stock movements, sets status to Done, logs cost)
                string username = UserSession.CurrentUser?.Username ?? "System";
                await _manufacturingService.ProduceManufacturingOrderAsync(_editingMO.Id, MOActualQuantity, lines, username);
                
                await LoadMOs();
                IsMOFormVisible = false;
                IsProduceStateActive = false;
                _editingMO = null;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Production failed: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task DeleteMO(ManufacturingOrderListItem item)
        {
            if (item == null) return;

            try
            {
                await _manufacturingService.DeleteManufacturingOrderAsync(item.ManufacturingOrder.Id);
                await LoadMOs();
                SelectedMO = null;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete MO: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CloseMOForm()
        {
            IsMOFormVisible = false;
            IsProduceStateActive = false;
            _editingMO = null;
            ErrorMessage = string.Empty;
        }

        // ==================== TAB 3: REPORTING METHODS ====================
        [RelayCommand]
        public async Task LoadReport()
        {
            try
            {
                var summary = await _manufacturingService.GetProductionReportAsync();
                TotalMOsCount = summary.TotalMOs;
                CompletedMOsCount = summary.CompletedMOs;
                TotalProductionCost = summary.TotalProductionCost;

                ReportOrders = new ObservableCollection<ManufacturingOrderListItem>(summary.CompletedOrders);
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading production reports: {ex.Message}";
            }
        }
    }

    public partial class MoLineViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _productName = string.Empty;

        [ObservableProperty]
        private int _productId;

        [ObservableProperty]
        private double _expectedQuantity;

        [ObservableProperty]
        private double _actualQuantity;

        [ObservableProperty]
        private string _unit = "Pcs";

        [ObservableProperty]
        private decimal _unitCost;
    }

    public partial class BomLineViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _productSearchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Product> _matchedProducts = new();

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                {
                    Unit = value.Unit;
                    if (ProductSearchText != value.Name)
                    {
                        ProductSearchText = value.Name;
                    }
                }
                OnPropertyChanged(nameof(IsDropdownVisible));
            }
        }

        [ObservableProperty]
        private double _quantity = 1.0;

        [ObservableProperty]
        private string _unit = "Pcs";

        [ObservableProperty]
        private double _scrapPercent;

        public bool IsDropdownVisible => SelectedProduct == null && !string.IsNullOrWhiteSpace(ProductSearchText) && MatchedProducts.Count > 0;

        public ObservableCollection<Product> Products { get; private set; }
        public List<string> Units { get; }

        public Action<BomLineViewModel>? RequestCreateProduct { get; set; }

        public BomLineViewModel(IEnumerable<Product> products, List<string> units)
        {
            Products = new ObservableCollection<Product>(products);
            Units = units;
            MatchedProducts = new ObservableCollection<Product>(Products.Take(5));
        }

        partial void OnProductSearchTextChanged(string value)
        {
            if (SelectedProduct != null && value != SelectedProduct.Name)
            {
                SelectedProduct = null;
            }
            FilterProducts();
            OnPropertyChanged(nameof(IsDropdownVisible));
        }

        public void FilterProducts()
        {
            if (Products == null || Products.Count == 0)
            {
                MatchedProducts = new ObservableCollection<Product>();
                return;
            }

            if (string.IsNullOrWhiteSpace(ProductSearchText) || (SelectedProduct != null && ProductSearchText == SelectedProduct.Name))
            {
                MatchedProducts = new ObservableCollection<Product>(Products.Take(5));
                return;
            }

            var query = ProductSearchText.Trim().ToLower();
            var matches = Products.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(query)) ||
                (p.SKU != null && p.SKU.ToLower().Contains(query))
            ).Take(8).ToList();
            MatchedProducts = new ObservableCollection<Product>(matches);
        }

        [RelayCommand]
        public void SelectProduct(Product? product)
        {
            SelectedProduct = product;
            if (product != null)
            {
                ProductSearchText = product.Name;
                Unit = product.Unit;
            }
            FilterProducts();
            OnPropertyChanged(nameof(IsDropdownVisible));
        }

        [RelayCommand]
        public void ClearProduct()
        {
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            FilterProducts();
            OnPropertyChanged(nameof(IsDropdownVisible));
        }

        [RelayCommand]
        public void CreateProduct()
        {
            RequestCreateProduct?.Invoke(this);
        }

        public void UpdateProducts(IEnumerable<Product> products)
        {
            Products = new ObservableCollection<Product>(products);
            OnPropertyChanged(nameof(Products));
            FilterProducts();
        }
    }
}
