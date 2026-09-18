using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels;

public partial class StockTransferViewModel : ViewModelBase
{
    private readonly LocationService _locationService;
    private readonly InventoryService _inventoryService;
    private readonly Action? _goBack;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isNewTransferVisible;
    [ObservableProperty] private Location? _sourceLocation;
    [ObservableProperty] private Location? _destLocation;
    [ObservableProperty] private string _transferNumber = string.Empty;
    [ObservableProperty] private DateTime _transferDate = DateTime.Now;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccessMessage => !string.IsNullOrWhiteSpace(SuccessMessage);

    // Keep legacy SelectedProduct and Quantity for backward compatibility if any callers bind to them
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private int _quantity;

    public ObservableCollection<Location> Locations { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<StockTransferLineItemViewModel> TransferLines { get; } = new();
    public ObservableCollection<StockTransferListItem> AllTransfers { get; } = new();
    public ObservableCollection<StockTransferListItem> FilteredTransfers { get; } = new();

    public int TotalLinesCount => TransferLines.Count(l => l.SelectedProduct != null);
    public int TotalUnitsCount => TransferLines.Where(l => l.SelectedProduct != null).Sum(l => l.Quantity);
    public bool HasAnyStockError => TransferLines.Any(l => l.HasStockError);

    public StockTransferViewModel(
        LocationService locationService, 
        InventoryService inventoryService, 
        Action? goBack = null)
    {
        _locationService = locationService;
        _inventoryService = inventoryService;
        _goBack = goBack;
        _ = LoadInitialData();
    }

    [RelayCommand]
    public async Task LoadInitialData()
    {
        IsLoading = true;
        try
        {
            Locations.Clear();
            var locations = await _locationService.GetAllLocationsAsync();
            foreach (var loc in locations) Locations.Add(loc);

            Products.Clear();
            var products = await _inventoryService.GetAllProductsAsync();
            foreach (var p in products) Products.Add(p);

            await LoadTransfersHistoryAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadTransfersHistoryAsync()
    {
        try
        {
            var transfers = await _locationService.GetAllStockTransfersAsync();
            AllTransfers.Clear();
            foreach (var t in transfers) AllTransfers.Add(t);
            FilterTransfers();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading transfer history: {ex.Message}";
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterTransfers();
    }

    public void FilterTransfers()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredTransfers.Clear();
            foreach (var t in AllTransfers) FilteredTransfers.Add(t);
            return;
        }

        var q = SearchText.Trim().ToLowerInvariant();
        var filtered = AllTransfers.Where(t =>
            (t.TransferNumber != null && t.TransferNumber.ToLowerInvariant().Contains(q)) ||
            (t.ProductName != null && t.ProductName.ToLowerInvariant().Contains(q)) ||
            (t.ProductSku != null && t.ProductSku.ToLowerInvariant().Contains(q)) ||
            (t.FromLocationName != null && t.FromLocationName.ToLowerInvariant().Contains(q)) ||
            (t.ToLocationName != null && t.ToLocationName.ToLowerInvariant().Contains(q)) ||
            (t.Notes != null && t.Notes.ToLowerInvariant().Contains(q)) ||
            (t.Status != null && t.Status.ToLowerInvariant().Contains(q))
        ).ToList();

        FilteredTransfers.Clear();
        foreach (var t in filtered) FilteredTransfers.Add(t);
    }

    [RelayCommand]
    public void ShowNewTransferForm()
    {
        TransferNumber = $"TRF-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
        TransferDate = DateTime.Now;
        Notes = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (Locations.Count > 0 && SourceLocation == null)
        {
            SourceLocation = Locations[0];
        }
        if (Locations.Count > 1 && (DestLocation == null || DestLocation.Id == SourceLocation?.Id))
        {
            DestLocation = Locations.FirstOrDefault(l => l.Id != SourceLocation?.Id) ?? Locations[1];
        }

        TransferLines.Clear();
        AddLine();
        IsNewTransferVisible = true;
        NotifySummaryChanged();
    }

    [RelayCommand]
    public void CancelNewTransfer()
    {
        IsNewTransferVisible = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public void AddLine()
    {
        var line = new StockTransferLineItemViewModel(Products, GetStockForProductAsync);
        line.LineChangedCallback = NotifySummaryChanged;
        TransferLines.Add(line);
        NotifySummaryChanged();
    }

    [RelayCommand]
    public void RemoveLine(StockTransferLineItemViewModel? line)
    {
        if (line != null && TransferLines.Contains(line))
        {
            TransferLines.Remove(line);
            if (TransferLines.Count == 0)
            {
                AddLine();
            }
            NotifySummaryChanged();
        }
    }

    [RelayCommand]
    public void ClearLines()
    {
        TransferLines.Clear();
        AddLine();
        NotifySummaryChanged();
    }

    public void NotifySummaryChanged()
    {
        OnPropertyChanged(nameof(TotalLinesCount));
        OnPropertyChanged(nameof(TotalUnitsCount));
        OnPropertyChanged(nameof(HasAnyStockError));
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasErrorMessage));
    }

    partial void OnSuccessMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasSuccessMessage));
    }

    partial void OnSourceLocationChanged(Location? value)
    {
        _ = RefreshAllLinesStockAsync();
    }

    private async Task<int> GetStockForProductAsync(int productId)
    {
        if (SourceLocation == null) return 0;
        return await _locationService.GetProductStockAtLocationAsync(SourceLocation.Id, productId);
    }

    public async Task RefreshAllLinesStockAsync()
    {
        foreach (var line in TransferLines)
        {
            await line.RefreshStockAsync();
        }
        NotifySummaryChanged();
    }

    [RelayCommand]
    public async Task PerformTransfer()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        // 1. Validate locations
        if (SourceLocation == null)
        {
            ErrorMessage = "Please select a Source Location (From).";
            return;
        }

        if (DestLocation == null)
        {
            ErrorMessage = "Please select a Destination Location (To).";
            return;
        }

        if (SourceLocation.Id == DestLocation.Id)
        {
            ErrorMessage = "Source Location and Destination Location must be different.";
            return;
        }

        // Support single-item transfer if TransferLines is empty but legacy properties were filled
        if (TransferLines.Count == 0 && SelectedProduct != null && Quantity > 0)
        {
            var singleLine = new StockTransferLineItemViewModel(Products, GetStockForProductAsync);
            await singleLine.SelectProduct(SelectedProduct);
            singleLine.Quantity = Quantity;
            TransferLines.Add(singleLine);
        }

        // 2. Validate line items
        var validLines = TransferLines.Where(l => l.SelectedProduct != null).ToList();
        if (validLines.Count == 0)
        {
            ErrorMessage = "Please select at least one product to transfer.";
            return;
        }

        var emptyLines = TransferLines.Where(l => l.SelectedProduct == null && !string.IsNullOrWhiteSpace(l.ProductSearchText)).ToList();
        if (emptyLines.Count > 0)
        {
            ErrorMessage = "One or more lines have unconfirmed product selections. Please select the product or remove the row.";
            return;
        }

        foreach (var line in validLines)
        {
            if (line.Quantity <= 0)
            {
                ErrorMessage = $"Transfer quantity for '{line.SelectedProduct!.Name}' must be greater than zero.";
                return;
            }

            if (line.Quantity > line.AvailableStock)
            {
                ErrorMessage = $"Cannot transfer {line.Quantity} of '{line.SelectedProduct!.Name}'. Only {line.AvailableStock} available at {SourceLocation.Name}.";
                return;
            }
        }

        // 3. Validate aggregated quantities if same product appears multiple times
        var grouped = validLines.GroupBy(l => l.SelectedProduct!.Id);
        foreach (var grp in grouped)
        {
            var prodId = grp.Key;
            var totalRequested = grp.Sum(l => l.Quantity);
            var available = grp.First().AvailableStock;
            if (totalRequested > available)
            {
                var prodName = grp.First().SelectedProduct!.Name;
                ErrorMessage = $"Total requested quantity for '{prodName}' ({totalRequested}) exceeds available stock ({available}) at {SourceLocation.Name}.";
                return;
            }
        }

        // 4. Build batch transfers and execute
        try
        {
            IsLoading = true;
            var refNum = string.IsNullOrWhiteSpace(TransferNumber) 
                ? $"TRF-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}" 
                : TransferNumber;

            var transfers = validLines.Select(line => new StockTransfer
            {
                TransferNumber = refNum,
                FromLocationId = SourceLocation.Id,
                ToLocationId = DestLocation.Id,
                ProductId = line.SelectedProduct!.Id,
                Quantity = line.Quantity,
                Status = "Completed",
                RequestedDate = TransferDate,
                CompletedDate = DateTime.Now,
                RequestedByUsername = UserSession.CurrentUser?.Username ?? "System",
                Notes = Notes
            }).ToList();

            await _locationService.TransferStockBatchAsync(transfers);

            var totalUnits = transfers.Sum(t => t.Quantity);
            SuccessMessage = $"Successfully transferred {transfers.Count} product(s) ({totalUnits} total units) from {SourceLocation.Name} to {DestLocation.Name} [Ref: {refNum}]!";

            // Reset legacy fields
            Quantity = 0;
            SelectedProduct = null;

            // Refresh history and close form
            await LoadTransfersHistoryAsync();
            IsNewTransferVisible = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Stock transfer failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        _goBack?.Invoke();
    }
}

public partial class StockTransferLineItemViewModel : ObservableObject
{
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private string _productSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<Product> _matchedProducts = new();
    [ObservableProperty] private int _availableStock;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private string _unit = "Pcs";

    public Action? LineChangedCallback { get; set; }

    public bool IsDropdownVisible => SelectedProduct == null && !string.IsNullOrWhiteSpace(ProductSearchText) && MatchedProducts.Count > 0;
    public bool HasStockError => SelectedProduct != null && (Quantity > AvailableStock || Quantity <= 0);
    public bool IsInStock => SelectedProduct != null && AvailableStock > 0 && Quantity <= AvailableStock && Quantity > 0;

    public string StockBadgeText
    {
        get
        {
            if (SelectedProduct == null) return "No Product";
            return $"{AvailableStock} in stock";
        }
    }

    public string StockStatusText
    {
        get
        {
            if (SelectedProduct == null) return "-";
            if (AvailableStock <= 0) return "⚠️ Out of Stock";
            if (Quantity > AvailableStock) return $"⚠️ Exceeds Stock ({AvailableStock})";
            if (Quantity <= 0) return "⚠️ Qty > 0 required";
            return "✓ Ready";
        }
    }

    public string StockBadgeBackground
    {
        get
        {
            if (SelectedProduct == null) return "#F3F4F6";
            if (HasStockError) return "#FEE2E2";
            return "#ECFDF5";
        }
    }

    public string StockBadgeForeground
    {
        get
        {
            if (SelectedProduct == null) return "#9CA3AF";
            if (HasStockError) return "#DC2626";
            return "#059669";
        }
    }

    public string StockBadgeBorder
    {
        get
        {
            if (SelectedProduct == null) return "#E5E7EB";
            if (HasStockError) return "#FCA5A5";
            return "#A7F3D0";
        }
    }

    public ObservableCollection<Product> Products { get; }
    private readonly Func<int, Task<int>>? _getStockCallback;

    public StockTransferLineItemViewModel(IEnumerable<Product> products, Func<int, Task<int>>? getStockCallback = null)
    {
        Products = new ObservableCollection<Product>(products);
        _getStockCallback = getStockCallback;
        MatchedProducts = new ObservableCollection<Product>(Products.Take(6));
    }

    partial void OnProductSearchTextChanged(string value)
    {
        if (SelectedProduct != null && value != SelectedProduct.Name)
        {
            SelectedProduct = null;
            AvailableStock = 0;
            NotifyStockState();
        }
        FilterProducts();
        OnPropertyChanged(nameof(IsDropdownVisible));
        LineChangedCallback?.Invoke();
    }

    partial void OnQuantityChanged(int value)
    {
        NotifyStockState();
        LineChangedCallback?.Invoke();
    }

    partial void OnAvailableStockChanged(int value)
    {
        NotifyStockState();
        LineChangedCallback?.Invoke();
    }

    public void NotifyStockState()
    {
        OnPropertyChanged(nameof(HasStockError));
        OnPropertyChanged(nameof(IsInStock));
        OnPropertyChanged(nameof(StockBadgeText));
        OnPropertyChanged(nameof(StockStatusText));
        OnPropertyChanged(nameof(StockBadgeBackground));
        OnPropertyChanged(nameof(StockBadgeForeground));
        OnPropertyChanged(nameof(StockBadgeBorder));
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
            MatchedProducts = new ObservableCollection<Product>(Products.Take(6));
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
    public async Task SelectProduct(Product? product)
    {
        SelectedProduct = product;
        if (product != null)
        {
            ProductSearchText = product.Name;
            Unit = string.IsNullOrWhiteSpace(product.Unit) ? "Pcs" : product.Unit;
            if (_getStockCallback != null)
            {
                AvailableStock = await _getStockCallback(product.Id);
            }
        }
        else
        {
            AvailableStock = 0;
        }
        FilterProducts();
        OnPropertyChanged(nameof(IsDropdownVisible));
        NotifyStockState();
        LineChangedCallback?.Invoke();
    }

    [RelayCommand]
    public void ClearProduct()
    {
        SelectedProduct = null;
        ProductSearchText = string.Empty;
        AvailableStock = 0;
        FilterProducts();
        OnPropertyChanged(nameof(IsDropdownVisible));
        NotifyStockState();
        LineChangedCallback?.Invoke();
    }

    public async Task RefreshStockAsync()
    {
        if (SelectedProduct != null && _getStockCallback != null)
        {
            AvailableStock = await _getStockCallback(SelectedProduct.Id);
            NotifyStockState();
        }
    }
}
