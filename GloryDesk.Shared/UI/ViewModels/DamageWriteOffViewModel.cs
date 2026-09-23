using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels;

public partial class DamageWriteOffViewModel : ViewModelBase
{
    private readonly DamageWriteOffService _damageWriteOffService;
    private readonly InventoryService _inventoryService;
    private System.Collections.Generic.List<Product> _allProducts = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private string _productSearchText = "";
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private ObservableCollection<Product> _matchedProducts = new();

    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private string _reasonCategory = "Damaged";
    [ObservableProperty] private string _notes = "";

    public ObservableCollection<PendingWriteOffLine> PendingLines { get; } = new();
    public ObservableCollection<DamageWriteOffDisplayRow> WriteOffs { get; } = new();
    public string[] ReasonCategories => DamageWriteOffService.ReasonCategories;

    public bool HasPendingLines => PendingLines.Count > 0;

    public DamageWriteOffViewModel(DamageWriteOffService damageWriteOffService, InventoryService inventoryService)
    {
        _damageWriteOffService = damageWriteOffService;
        _inventoryService = inventoryService;
        _ = LoadInitialData();
    }

    [RelayCommand]
    public async Task LoadInitialData()
    {
        IsLoading = true;
        ErrorMessage = null;

        _allProducts = await _inventoryService.GetAllProductsAsync();
        MatchedProducts = new ObservableCollection<Product>(_allProducts.Take(8));

        WriteOffs.Clear();
        var writeOffs = await _damageWriteOffService.GetAllAsync();
        foreach (var w in writeOffs)
        {
            var product = _allProducts.FirstOrDefault(p => p.Id == w.ProductId);
            WriteOffs.Add(new DamageWriteOffDisplayRow(w, product?.Name ?? "Unknown Product"));
        }

        IsLoading = false;
    }

    partial void OnProductSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || (SelectedProduct != null && value == SelectedProduct.Name))
        {
            MatchedProducts = new ObservableCollection<Product>(_allProducts.Take(8));
            return;
        }

        var query = value.ToLower();
        var matches = _allProducts
            .Where(p => p.Name.ToLower().Contains(query) || (p.SKU?.ToLower().Contains(query) ?? false))
            .Take(8)
            .ToList();
        MatchedProducts = new ObservableCollection<Product>(matches);
        SelectedProduct = null;
    }

    [RelayCommand]
    private void SelectProduct(Product? product)
    {
        if (product == null) return;
        SelectedProduct = product;
        ProductSearchText = product.Name;
        MatchedProducts.Clear();
    }

    /// <summary>Stock already claimed by lines queued for the same product but not posted yet.</summary>
    private int PendingQuantityFor(int productId) =>
        PendingLines.Where(l => l.ProductId == productId).Sum(l => l.Quantity);

    [RelayCommand]
    private void AddLine()
    {
        ErrorMessage = null;

        if (SelectedProduct == null)
        {
            ErrorMessage = "Search for and select a product first.";
            return;
        }

        if (Quantity <= 0)
        {
            ErrorMessage = "Quantity must be greater than zero.";
            return;
        }

        var alreadyQueued = PendingQuantityFor(SelectedProduct.Id);
        var availableNow = SelectedProduct.StockQuantity - alreadyQueued;
        if (Quantity > availableNow)
        {
            ErrorMessage = availableNow <= 0
                ? $"{SelectedProduct.Name} has no stock available to write off (on hand: {SelectedProduct.StockQuantity})."
                : $"Only {availableNow} of {SelectedProduct.Name} left to write off after your other queued lines (on hand: {SelectedProduct.StockQuantity}).";
            return;
        }

        PendingLines.Add(new PendingWriteOffLine(SelectedProduct.Id, SelectedProduct.Name, Quantity, ReasonCategory, Notes, SelectedProduct.Cost));
        OnPropertyChanged(nameof(HasPendingLines));

        ProductSearchText = "";
        SelectedProduct = null;
        Quantity = 1;
        Notes = "";
        MatchedProducts = new ObservableCollection<Product>(_allProducts.Take(8));
    }

    [RelayCommand]
    private void RemoveLine(PendingWriteOffLine? line)
    {
        if (line == null) return;
        PendingLines.Remove(line);
        OnPropertyChanged(nameof(HasPendingLines));
    }

    [RelayCommand]
    public async Task RecordWriteOff()
    {
        ErrorMessage = null;
        StatusMessage = null;

        if (PendingLines.Count == 0)
        {
            ErrorMessage = "Add at least one product to the list first.";
            return;
        }

        var username = UserSession.CurrentUser?.Username ?? "System";
        var posted = 0;

        // Snapshot: posting one line changes stock, which can invalidate a later line for the
        // same product - each call re-checks live stock, so we stop at the first failure rather
        // than silently skipping it.
        foreach (var line in PendingLines.ToList())
        {
            try
            {
                await _damageWriteOffService.RecordWriteOffAsync(
                    line.ProductId,
                    line.Quantity,
                    line.ReasonCategory,
                    line.Notes,
                    username);

                PendingLines.Remove(line);
                posted++;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Stopped after {posted} of {PendingLines.Count + posted}: {ex.Message}";
                OnPropertyChanged(nameof(HasPendingLines));
                await LoadInitialData();
                return;
            }
        }

        OnPropertyChanged(nameof(HasPendingLines));
        StatusMessage = $"Recorded {posted} write-off{(posted == 1 ? "" : "s")}.";
        await LoadInitialData();
    }

    [RelayCommand]
    private async Task Replenish(DamageWriteOffDisplayRow? row)
    {
        if (row == null) return;
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            var username = UserSession.CurrentUser?.Username ?? "System";
            var (docType, docNumber) = await _damageWriteOffService.ReplenishAsync(row.WriteOff.ProductId, row.WriteOff.Quantity, username);
            StatusMessage = $"Created draft {docType} {docNumber} for {row.ProductName} - confirm it from the {(docType == "Manufacturing Order" ? "Manufacturing" : "Purchase Orders / RFQ")} screen.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public class DamageWriteOffDisplayRow
{
    public DamageWriteOffDisplayRow(DamageWriteOff writeOff, string productName)
    {
        WriteOff = writeOff;
        ProductName = productName;
    }

    public DamageWriteOff WriteOff { get; }
    public string ProductName { get; }
}

public class PendingWriteOffLine
{
    public PendingWriteOffLine(int productId, string productName, int quantity, string reasonCategory, string notes, decimal unitCost)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        ReasonCategory = reasonCategory;
        Notes = notes;
        UnitCost = unitCost;
    }

    public int ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public string ReasonCategory { get; }
    public string Notes { get; }
    public decimal UnitCost { get; }
    public decimal TotalCost => UnitCost * Quantity;
}
