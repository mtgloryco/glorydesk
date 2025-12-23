using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class CartItem : ObservableObject
    {
        [ObservableProperty] private Product _product;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _unitPrice; // Selling Price

        private readonly int _maxStock;

        public decimal Subtotal => Quantity * UnitPrice;

        public CartItem(Product product, int quantity, decimal unitPrice)
        {
            _product = product;
            _quantity = quantity;
            _unitPrice = unitPrice;
            _maxStock = product.StockQuantity;
        }

        public void Increment()
        {
            if (Quantity < _maxStock)
            {
                Quantity++;
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public void Decrement()
        {
            if (Quantity > 1)
            {
                Quantity--;
                OnPropertyChanged(nameof(Subtotal));
            }
        }
    }

    public partial class POSViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly LicenseService _licenseService;

        [ObservableProperty] private ObservableCollection<Product> _availableProducts = new();
        [ObservableProperty] private ObservableCollection<CartItem> _cartItems = new();
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private decimal _amountPaid;
        [ObservableProperty] private decimal _changeDue;
        
        [ObservableProperty] private bool _isReceiptModalOpen;
        [ObservableProperty] private string _lastReceiptText = string.Empty;

        public POSViewModel(InventoryService inventoryService, LicenseService licenseService)
        {
            _inventoryService = inventoryService;
            _licenseService = licenseService; // Future pro features
            LoadProductsCommand.Execute(null);
        }

        async partial void OnSearchTextChanged(string value)
        {
            await LoadProducts();
        }

        [RelayCommand]
        private async Task LoadProducts()
        {
            var all = await _inventoryService.GetAllProductsAsync();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                AvailableProducts = new ObservableCollection<Product>(all);
            }
            else
            {
                var filtered = all.Where(p => 
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                    (p.SKU != null && p.SKU.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                AvailableProducts = new ObservableCollection<Product>(filtered);
            }
        }

        [RelayCommand]
        private void AddToCart(Product product)
        {
            if (product.StockQuantity <= 0) return; // Prevent adding if out of stock

            var existing = CartItems.FirstOrDefault(c => c.Product.Id == product.Id);
            if (existing != null)
            {
                if (existing.Quantity < product.StockQuantity)
                {
                    existing.Increment();
                }
            }
            else
            {
                // Default selling price could be Cost * Margin, or just Price property if we had one for sales.
                // For now, let's assume Product.Price is the default Selling Price.
                CartItems.Add(new CartItem(product, 1, product.Price));
            }
            RecalculateTotal();
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem item)
        {
            CartItems.Remove(item);
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            TotalAmount = CartItems.Sum(x => x.Subtotal);
            CalculateChange();
        }

        partial void OnAmountPaidChanged(decimal value)
        {
            CalculateChange();
        }

        private void CalculateChange()
        {
            ChangeDue = AmountPaid - TotalAmount;
        }

        [RelayCommand]
        private async Task Checkout()
        {
            if (CartItems.Count == 0) return;

            try
            {
                var user = UserSession.CurrentUser?.Username ?? "Cashier";

                // Process each item as a Stock OUT movement
                foreach (var item in CartItems)
                {
                    await _inventoryService.AddStockMovementAsync(
                        item.Product.Id, 
                        item.Quantity, 
                        "OUT", 
                        "POS Sale", 
                        user, 
                        customCost: null, 
                        unitPrice: item.UnitPrice // Capture the selling price at checkout
                    );
                }

                // Generate Receipt
                GenerateReceipt(user);
                
                // Clear Cart
                CartItems.Clear();
                RecalculateTotal();
                AmountPaid = 0;
                
                // Show Receipt Modal
                IsReceiptModalOpen = true;
                
                // Refresh Inventory List in background if needed (LoadProducts will handle it next time)
                await LoadProducts(); 
            }
            catch (Exception ex)
            {
                // Show error (for now, simply logging or maybe setting a property to show in UI)
                // In a real MVP, we'd have an error toast.
                LastReceiptText = $"Error during checkout: {ex.Message}";
                IsReceiptModalOpen = true;
            }
        }

        private void GenerateReceipt(string cashier)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== RECEIPT ==========");
            sb.AppendLine($"Date: {DateTime.Now}");
            sb.AppendLine($"Cashier: {cashier}");
            sb.AppendLine("-----------------------------");
            
            foreach (var item in CartItems) // Capture currrent state before clearing
            {
                 sb.AppendLine($"{item.Product.Name}");
                 sb.AppendLine($"  {item.Quantity} x {item.UnitPrice:C} = {item.Subtotal:C}");
            }
            
            sb.AppendLine("-----------------------------");
            sb.AppendLine($"TOTAL: {TotalAmount:C}");
            sb.AppendLine("=============================");
            sb.AppendLine("Thank you for your business!");
            
            LastReceiptText = sb.ToString();
        }

        [RelayCommand]
        private void CloseReceipt()
        {
            IsReceiptModalOpen = false;
        }

        [RelayCommand]
        private async Task PrintReceipt()
        {
             // For MVP, "Print" is just "Save to File" or "Copy to Clipboard".
             // We'll reuse the logic from ReportsViewModel if possible, or just plain text save.
             
             if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
             {
                var storageProvider = desktop.MainWindow.StorageProvider;
                var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save Receipt",
                    DefaultExtension = ".txt",
                    SuggestedFileName = $"Receipt_{DateTime.Now:yyyyMMdd_HHmmss}",
                    FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } } }
                });

                if (file != null)
                {
                    await System.IO.File.WriteAllTextAsync(file.Path.LocalPath, LastReceiptText);
                }
             }
        }
    }
}
