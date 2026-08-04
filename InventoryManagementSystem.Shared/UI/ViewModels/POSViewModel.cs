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
    public partial class PosTender : ObservableObject
    {
        [ObservableProperty] private PosPaymentMethod _method;
        [ObservableProperty] private decimal _amount;

        public PosTender(PosPaymentMethod method, decimal amount)
        {
            _method = method;
            _amount = amount;
        }
    }

    public partial class CartItem : ObservableObject
    {
        [ObservableProperty] private Product _product;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _unitPrice; // Selling Price - editable at checkout (price override)

        private readonly int _maxStock;

        public decimal Subtotal => Quantity * UnitPrice;

        public Action? OnChanged { get; set; }

        public CartItem(Product product, int quantity, decimal unitPrice)
        {
            _product = product;
            _quantity = quantity;
            _unitPrice = unitPrice;
            _maxStock = product.StockQuantity;
        }

        partial void OnQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(Subtotal));
            OnChanged?.Invoke();
        }

        partial void OnUnitPriceChanged(decimal value)
        {
            OnPropertyChanged(nameof(Subtotal));
            OnChanged?.Invoke();
        }

        public void Increment()
        {
            if (Product.ProductType == "Service" || Quantity < _maxStock)
            {
                Quantity++;
            }
        }

        public void Decrement()
        {
            if (Quantity > 1)
            {
                Quantity--;
            }
        }
    }

    public partial class POSViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly LicenseService _licenseService;
        private readonly ReceiptService _receiptService;
        private readonly SettingsService _settingsService;
        private readonly SalesOrderService _salesOrderService;
        private readonly CustomerService _customerService;
        private readonly JournalService _journalService;
        private readonly TaxService _taxService;
        private readonly BarcodeService _barcodeService;
        private readonly CurrencyService _currencyService;
        private readonly ReturnsService _returnsService;
        private readonly PosSessionService _posSessionService;
        private readonly AuditService? _auditService;

        [ObservableProperty] private ObservableCollection<Product> _availableProducts = new();
        [ObservableProperty] private string _barcodeStatusMessage = string.Empty;
        [ObservableProperty] private ObservableCollection<CartItem> _cartItems = new();
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private decimal _changeDue;
        [ObservableProperty] private string _posCheckoutCurrency = "RWF";
        [ObservableProperty] private decimal _checkoutTotalAmount;
        [ObservableProperty] private string _checkoutBaseEquivalent = string.Empty;

        public List<string> PosCurrencies { get; } = new() { "USD", "EUR", "GBP", "KES", "RWF", "UGX" };
        [ObservableProperty] private bool _isReceiptModalOpen;
        [ObservableProperty] private string _lastReceiptPath = string.Empty;
        [ObservableProperty] private string _lastReceiptText = string.Empty; // Keep for fallback/display

        // --- Screen Navigation ---
        [ObservableProperty] private string _activeTab = "Sales"; // "Sales", "Orders", "PaymentMethods"
        public bool IsSalesTabActive => ActiveTab == "Sales";
        public bool IsOrdersTabActive => ActiveTab == "Orders";
        public bool IsPaymentMethodsTabActive => ActiveTab == "PaymentMethods";

        // Only the Sales screen needs a register open - Orders history and Configuration are
        // freely usable regardless, since they're lookup/admin screens rather than a till.
        public bool ShowSalesContent => IsSalesTabActive && CurrentSession != null;
        public bool ShowOpenRegisterPrompt => IsSalesTabActive && CurrentSession == null;

        // --- Cashier ---
        public string CashierName => UserSession.CurrentUser?.Username ?? "Cashier";

        // --- POS Payment Methods Screen ---
        [ObservableProperty] private ObservableCollection<PosPaymentMethod> _paymentMethods = new();
        [ObservableProperty] private ObservableCollection<Journal> _journals = new();
        [ObservableProperty] private string _newPaymentMethodName = string.Empty;
        [ObservableProperty] private Journal? _newPaymentMethodSelectedJournal;
        [ObservableProperty] private string _journalSearchText = string.Empty;
        [ObservableProperty] private ObservableCollection<Journal> _matchedJournals = new();

        // --- POS Checkout Payment Panel ---
        [ObservableProperty] private bool _isPaymentPanelVisible;
        [ObservableProperty] private ObservableCollection<Customer> _matchedCustomers = new();
        [ObservableProperty] private Customer? _selectedCustomer;
        [ObservableProperty] private string _customerSearchText = string.Empty;
        [ObservableProperty] private PosPaymentMethod? _selectedPaymentMethod;
        [ObservableProperty] private bool _autoCreateInvoice = true;

        // --- Split-tender payment (part cash, part mobile money, etc.) ---
        [ObservableProperty] private decimal _tenderAmount;
        [ObservableProperty] private string? _paymentErrorMessage;
        public ObservableCollection<PosTender> Tenders { get; } = new();
        public decimal TotalTendered => Tenders.Sum(t => t.Amount);
        public decimal RemainingDue => Math.Max(0, CheckoutTotalAmount - TotalTendered);
        public bool CanCompleteSale => Tenders.Count > 0 && RemainingDue <= 0.001m;

        // --- inline Customer Creation Modal ---
        [ObservableProperty] private bool _isCreateCustomerModalOpen;
        [ObservableProperty] private string _newCustomerName = string.Empty;
        [ObservableProperty] private string _newCustomerPhone = string.Empty;
        [ObservableProperty] private string _newCustomerEmail = string.Empty;
        [ObservableProperty] private string _newCustomerAddress = string.Empty;
        [ObservableProperty] private string _newCustomerErrorMessage = string.Empty;
        [ObservableProperty] private bool _isCheckoutSuccess;

        // --- POS Order History ---
        [ObservableProperty] private ObservableCollection<SalesOrderListItem> _posOrders = new();
        [ObservableProperty] private ObservableCollection<CustomerOrderGroup> _posOrderGroups = new();
        [ObservableProperty] private string _orderSearchText = string.Empty;

        // --- POS Order Details Modal ---
        [ObservableProperty] private bool _isOrderDetailsOpen;
        [ObservableProperty] private SalesOrder? _detailedOrder;
        [ObservableProperty] private string _detailedCustomerName = string.Empty;
        [ObservableProperty] private ObservableCollection<POSDetailedOrderItemRow> _detailedOrderItems = new();
        [ObservableProperty] private ObservableCollection<POSDetailedPaymentRow> _detailedPayments = new();
        [ObservableProperty] private ObservableCollection<CustomerReturn> _detailedReturns = new();
        public bool HasReturns => DetailedReturns.Count > 0;
        [ObservableProperty] private decimal _detailedSubtotal;
        [ObservableProperty] private decimal _detailedTaxes;
        [ObservableProperty] private decimal _detailedTotal;
        [ObservableProperty] private string _detailedTaxBreakdownText = string.Empty;

        // --- POS Session (cash register shift) ---
        [ObservableProperty] private PosSession? _currentSession;
        [ObservableProperty] private bool _isOpenRegisterModalOpen;
        [ObservableProperty] private string _openRegisterErrorMessage = string.Empty;
        public ObservableCollection<PosOpeningBalanceRow> OpeningBalanceRows { get; } = new();

        [ObservableProperty] private bool _isCloseRegisterModalOpen;
        [ObservableProperty] private string _closeRegisterErrorMessage = string.Empty;
        [ObservableProperty] private string _closingNotes = string.Empty;
        public ObservableCollection<PosCloseBalanceRow> CloseBalanceRows { get; } = new();

        [ObservableProperty] private bool _isCashMovementModalOpen;
        [ObservableProperty] private string _cashMovementType = "In";
        [ObservableProperty] private PosPaymentMethod? _cashMovementMethod;
        [ObservableProperty] private decimal _cashMovementAmount;
        [ObservableProperty] private string _cashMovementReason = string.Empty;
        [ObservableProperty] private string _cashMovementErrorMessage = string.Empty;
        public List<string> CashMovementTypes { get; } = new() { "In", "Out" };
        public ObservableCollection<PosPaymentMethod> CashMovementMethods { get; } = new();

        // --- Orders tab: toggle between individual Orders and Sessions ---
        [ObservableProperty] private string _ordersViewMode = "Orders"; // "Orders" or "Sessions"
        public bool IsOrdersModeSelected => OrdersViewMode == "Orders";
        public bool IsSessionsModeSelected => OrdersViewMode == "Sessions";
        public ObservableCollection<PosSession> AllSessions { get; } = new();
        [ObservableProperty] private PosSession? _selectedSessionForView;
        [ObservableProperty] private PosSessionSummary? _selectedSessionSummary;

        public string CurrencySymbol => _settingsService.CurrentSettings.CurrencySymbol;
        public string BaseCurrency => CurrencySymbol ?? "RWF";
        public string ActiveCheckoutCurrency => string.IsNullOrWhiteSpace(PosCheckoutCurrency) ? BaseCurrency : PosCheckoutCurrency;
        public LanguageService Language { get; }

        public POSViewModel(
            InventoryService inventoryService, 
            LicenseService licenseService, 
            ReceiptService receiptService, 
            SettingsService settingsService, 
            LanguageService languageService,
            SalesOrderService salesOrderService,
            CustomerService customerService,
            JournalService journalService,
            TaxService taxService,
            BarcodeService barcodeService,
            CurrencyService currencyService,
            ReturnsService returnsService,
            PosSessionService posSessionService,
            AuditService? auditService = null)
        {
            _inventoryService = inventoryService;
            _licenseService = licenseService; // Future pro features
            _receiptService = receiptService;
            _settingsService = settingsService;
            Language = languageService;
            _salesOrderService = salesOrderService;
            _customerService = customerService;
            _auditService = auditService;
            _journalService = journalService;
            _taxService = taxService;
            _barcodeService = barcodeService;
            _currencyService = currencyService;
            _returnsService = returnsService;
            _posSessionService = posSessionService;
            PosCheckoutCurrency = BaseCurrency;
            CheckoutTotalAmount = 0;
            LoadProductsCommand.Execute(null);
            _ = EnsureWalkInCustomerExistsAsync();
            _ = CheckForOpenSessionAsync();
        }

        async partial void OnSearchTextChanged(string value)
        {
            await LoadProducts();
        }

        [RelayCommand]
        private async Task ScanBarcodeAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return;
            }

            var product = await _barcodeService.FindProductByBarcodeAsync(SearchText.Trim());
            if (product == null)
            {
                BarcodeStatusMessage = "No product found for this barcode.";
                return;
            }

            if (!product.AvailableInPOS || !product.CanBeSold)
            {
                BarcodeStatusMessage = $"{product.Name} is not available at POS.";
                return;
            }

            AddToCart(product);
            SearchText = string.Empty;
            BarcodeStatusMessage = $"Added {product.Name} to cart.";
            await LoadProducts();
        }

        [RelayCommand]
        private async Task LoadProducts()
        {
            var all = await _inventoryService.GetAllProductsAsync();
            var posProducts = all.Where(p => p.AvailableInPOS && p.CanBeSold).ToList();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                AvailableProducts = new ObservableCollection<Product>(posProducts);
            }
            else
            {
                var filtered = posProducts.Where(p => 
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                    (p.SKU != null && p.SKU.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                AvailableProducts = new ObservableCollection<Product>(filtered);
            }
        }

        [RelayCommand]
        private void AddToCart(Product product)
        {
            if (product.ProductType != "Service" && product.StockQuantity <= 0) return; // Prevent adding if out of stock

            var existing = CartItems.FirstOrDefault(c => c.Product.Id == product.Id);
            if (existing != null)
            {
                if (existing.Quantity < product.StockQuantity || product.ProductType == "Service")
                {
                    existing.Increment();
                }
            }
            else
            {
                // Default selling price: use product.Price, but if it is 0 or less, fall back to product.Cost
                decimal sellingPrice = product.Price > 0 ? product.Price : product.Cost;
                var item = new CartItem(product, 1, sellingPrice);
                item.OnChanged = RecalculateTotal;
                CartItems.Add(item);
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
            _ = UpdateCheckoutAmountsAsync();
        }

        partial void OnPosCheckoutCurrencyChanged(string value)
        {
            _ = UpdateCheckoutAmountsAsync();
        }

        private async Task UpdateCheckoutAmountsAsync()
        {
            var checkoutCurrency = ActiveCheckoutCurrency;
            if (string.Equals(checkoutCurrency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                CheckoutTotalAmount = TotalAmount;
                CheckoutBaseEquivalent = string.Empty;
            }
            else
            {
                var (converted, _) = await _currencyService.TryFormatFromBaseAsync(TotalAmount, checkoutCurrency, BaseCurrency);
                CheckoutTotalAmount = converted ?? TotalAmount;
                CheckoutBaseEquivalent = $"Listed at {TotalAmount:N2} {BaseCurrency}";
            }

            OnPropertyChanged(nameof(ActiveCheckoutCurrency));
            RaiseTenderTotalsChanged();
        }

        [RelayCommand]
        private void SwitchTab(string tabName)
        {
            ActiveTab = tabName;
            IsPaymentPanelVisible = false;
        }

        [RelayCommand]
        private void TogglePaymentPanel()
        {
            if (CartItems.Count == 0) return;
            IsPaymentPanelVisible = !IsPaymentPanelVisible;
            if (IsPaymentPanelVisible)
            {
                Tenders.Clear();
                PaymentErrorMessage = null;
                TenderAmount = CheckoutTotalAmount;
                RaiseTenderTotalsChanged();
                _ = LoadCustomersAndPaymentMethodsAsync();
            }
        }

        private void RaiseTenderTotalsChanged()
        {
            OnPropertyChanged(nameof(TotalTendered));
            OnPropertyChanged(nameof(RemainingDue));
            OnPropertyChanged(nameof(CanCompleteSale));
            ChangeDue = Math.Max(0, TotalTendered - CheckoutTotalAmount);
        }

        [RelayCommand]
        private void AddTender()
        {
            PaymentErrorMessage = null;
            if (SelectedPaymentMethod == null)
            {
                PaymentErrorMessage = "Select a payment method.";
                return;
            }
            if (TenderAmount <= 0)
            {
                PaymentErrorMessage = "Enter an amount greater than zero.";
                return;
            }

            Tenders.Add(new PosTender(SelectedPaymentMethod, TenderAmount));
            TenderAmount = Math.Max(0, CheckoutTotalAmount - TotalTendered);
            RaiseTenderTotalsChanged();
        }

        [RelayCommand]
        private void RemoveTender(PosTender? tender)
        {
            if (tender == null) return;
            Tenders.Remove(tender);
            RaiseTenderTotalsChanged();
        }

        private async Task LoadCustomersAndPaymentMethodsAsync()
        {
            try
            {
                var connection = _journalService.Database.Connection;
                
                // Load payment methods
                var list = await connection.Table<PosPaymentMethod>().ToListAsync();
                PaymentMethods = new ObservableCollection<PosPaymentMethod>(list);
                if (SelectedPaymentMethod == null && list.Count > 0)
                {
                    SelectedPaymentMethod = list[0];
                }

                // Initial customers list
                var allCusts = await _customerService.GetAllCustomersAsync();
                MatchedCustomers.Clear();
                foreach (var c in allCusts.Take(5))
                {
                    MatchedCustomers.Add(c);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load customer/payment data: {ex.Message}");
            }
        }

        async partial void OnCustomerSearchTextChanged(string value)
        {
            if (SelectedCustomer != null && value != SelectedCustomer.Name)
            {
                SelectedCustomer = null;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                MatchedCustomers.Clear();
                var allCusts = await _customerService.GetAllCustomersAsync();
                foreach (var c in allCusts.Take(5)) MatchedCustomers.Add(c);
                return;
            }

            var query = value.ToLower();
            var matched = await _customerService.GetAllCustomersAsync();
            var filtered = matched.Where(c => c.Name.ToLower().Contains(query)).Take(5).ToList();
            
            MatchedCustomers.Clear();
            foreach (var c in filtered) MatchedCustomers.Add(c);
        }

        [RelayCommand]
        private void SelectCustomer(Customer customer)
        {
            if (customer == null) return;
            SelectedCustomer = customer;
            CustomerSearchText = customer.Name;
            MatchedCustomers.Clear();
        }

        [RelayCommand]
        private void OpenCreateCustomerModal()
        {
            NewCustomerName = string.Empty;
            NewCustomerPhone = string.Empty;
            NewCustomerEmail = string.Empty;
            NewCustomerAddress = string.Empty;
            NewCustomerErrorMessage = string.Empty;
            IsCreateCustomerModalOpen = true;
        }

        [RelayCommand]
        private void CloseCreateCustomerModal()
        {
            IsCreateCustomerModalOpen = false;
        }

        [RelayCommand]
        private async Task CreateCustomerAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName))
            {
                NewCustomerErrorMessage = "Name is required.";
                return;
            }

            try
            {
                var customer = new Customer
                {
                    Name = NewCustomerName.Trim(),
                    Phone = NewCustomerPhone.Trim(),
                    Email = NewCustomerEmail.Trim(),
                    Address = NewCustomerAddress.Trim(),
                    CreatedAt = DateTime.Now
                };
                await _customerService.AddCustomerAsync(customer);
                IsCreateCustomerModalOpen = false;

                // Select the newly created customer
                SelectCustomer(customer);
            }
            catch (Exception ex)
            {
                NewCustomerErrorMessage = $"Error: {ex.Message}";
            }
        }

        partial void OnActiveTabChanged(string value)
        {
            OnPropertyChanged(nameof(IsSalesTabActive));
            OnPropertyChanged(nameof(IsOrdersTabActive));
            OnPropertyChanged(nameof(IsPaymentMethodsTabActive));
            OnPropertyChanged(nameof(ShowSalesContent));
            OnPropertyChanged(nameof(ShowOpenRegisterPrompt));

            if (value == "Orders")
            {
                _ = LoadPosOrdersAsync();
            }
            else if (value == "PaymentMethods")
            {
                _ = LoadPaymentMethodsDataAsync();
            }
        }

        partial void OnOrderSearchTextChanged(string value)
        {
            _ = LoadPosOrdersAsync();
        }

        private async Task LoadPosOrdersAsync()
        {
            try
            {
                var list = await _salesOrderService.GetPosSalesOrdersAsync();

                if (!string.IsNullOrWhiteSpace(OrderSearchText))
                {
                    var query = OrderSearchText.ToLower();
                    list = list.Where(o =>
                        o.CustomerName.ToLower().Contains(query) ||
                        o.SalesOrder.SONumber.ToLower().Contains(query)
                    ).ToList();
                }

                PosOrders.Clear();
                foreach (var item in list)
                {
                    PosOrders.Add(item);
                }

                // Group by customer so the cashier can drill: customer -> their orders -> order items.
                var groups = list
                    .GroupBy(o => o.CustomerName)
                    .OrderBy(g => g.Key)
                    .Select(g => new CustomerOrderGroup(g.Key, g.OrderByDescending(o => o.SalesOrder.OrderDate).ToList()))
                    .ToList();

                var previouslyExpanded = PosOrderGroups.Where(g => g.IsExpanded).Select(g => g.CustomerName).ToHashSet();
                foreach (var group in groups)
                {
                    if (previouslyExpanded.Contains(group.CustomerName))
                    {
                        group.IsExpanded = true;
                    }
                }

                PosOrderGroups = new ObservableCollection<CustomerOrderGroup>(groups);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load POS orders: {ex.Message}");
            }
        }

        private async Task LoadPaymentMethodsDataAsync()
        {
            try
            {
                var connection = _journalService.Database.Connection;
                
                var list = await connection.Table<PosPaymentMethod>().ToListAsync();
                PaymentMethods = new ObservableCollection<PosPaymentMethod>(list);

                var allJournals = await _journalService.GetAllJournalsAsync();
                var filteredJournals = allJournals.Where(j => j.Type == "Cash" || j.Type == "Bank" || j.Type == "Credit Card" || j.Type == "Miscellaneous").ToList();
                Journals = new ObservableCollection<Journal>(filteredJournals);
                MatchedJournals = new ObservableCollection<Journal>(filteredJournals);
                if (NewPaymentMethodSelectedJournal == null && filteredJournals.Count > 0)
                {
                    NewPaymentMethodSelectedJournal = filteredJournals[0];
                    JournalSearchText = filteredJournals[0].Name;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Payment methods page: {ex.Message}");
            }
        }

        partial void OnJournalSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || (NewPaymentMethodSelectedJournal != null && value == NewPaymentMethodSelectedJournal.Name))
            {
                MatchedJournals = new ObservableCollection<Journal>(Journals);
                return;
            }

            var query = value.ToLower();
            var matches = Journals.Where(j => j.Name.ToLower().Contains(query)).ToList();
            MatchedJournals = new ObservableCollection<Journal>(matches);
            NewPaymentMethodSelectedJournal = null;
        }

        [RelayCommand]
        private void SelectJournal(Journal? journal)
        {
            if (journal == null) return;
            NewPaymentMethodSelectedJournal = journal;
            JournalSearchText = journal.Name;
            MatchedJournals.Clear();
        }

        [RelayCommand]
        private async Task CreatePaymentMethodAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPaymentMethodName))
            {
                return;
            }

            if (NewPaymentMethodSelectedJournal == null)
            {
                return;
            }

            try
            {
                var method = new PosPaymentMethod
                {
                    Name = NewPaymentMethodName.Trim(),
                    JournalId = NewPaymentMethodSelectedJournal.Id
                };

                var connection = _journalService.Database.Connection;
                await connection.InsertAsync(method);

                NewPaymentMethodName = string.Empty;
                NewPaymentMethodSelectedJournal = null;
                JournalSearchText = string.Empty;
                await LoadPaymentMethodsDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create payment method: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeletePaymentMethodAsync(PosPaymentMethod method)
        {
            if (method == null) return;
            try
            {
                var connection = _journalService.Database.Connection;
                await connection.DeleteAsync(method);
                await LoadPaymentMethodsDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete payment method: {ex.Message}");
            }
        }

        private async Task EnsureWalkInCustomerExistsAsync()
        {
            try
            {
                await _customerService.EnsureWalkInCustomerAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to ensure Walk-in Customer: {ex.Message}");
            }
        }

        // --- POS Session (cash register shift) ---
        // Deliberately scoped to Sales only: Orders history and Configuration (payment methods)
        // stay usable without a register open, since they're lookup/admin screens, not a till.
        // Only ringing up a sale needs a session, and the cashier opens one on their own terms via
        // the "Open Register" prompt shown in place of the product grid - never a forced popup.

        private async Task CheckForOpenSessionAsync()
        {
            CurrentSession = await _posSessionService.GetOpenSessionAsync();
        }

        partial void OnCurrentSessionChanged(PosSession? value)
        {
            OnPropertyChanged(nameof(ShowSalesContent));
            OnPropertyChanged(nameof(ShowOpenRegisterPrompt));
        }

        [RelayCommand]
        private async Task PromptOpenRegister()
        {
            await PrepareOpenRegisterModalAsync();
        }

        private async Task PrepareOpenRegisterModalAsync()
        {
            OpenRegisterErrorMessage = string.Empty;
            var connection = _journalService.Database.Connection;
            var methods = await connection.Table<PosPaymentMethod>().ToListAsync();

            OpeningBalanceRows.Clear();
            foreach (var method in methods)
            {
                OpeningBalanceRows.Add(new PosOpeningBalanceRow(method));
            }

            IsOpenRegisterModalOpen = true;
        }

        [RelayCommand]
        private async Task OpenRegister()
        {
            OpenRegisterErrorMessage = string.Empty;
            if (OpeningBalanceRows.Count == 0)
            {
                OpenRegisterErrorMessage = "Set up at least one payment method first (POS > Payment Methods).";
                return;
            }

            try
            {
                var openingBalances = OpeningBalanceRows.ToDictionary(r => r.Method.Id, r => r.Amount);
                var user = UserSession.CurrentUser?.Username ?? "Cashier";
                CurrentSession = await _posSessionService.OpenSessionAsync(openingBalances, user);
                IsOpenRegisterModalOpen = false;
            }
            catch (Exception ex)
            {
                OpenRegisterErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task OpenCloseRegisterModal()
        {
            if (CurrentSession == null) return;

            CloseRegisterErrorMessage = string.Empty;
            ClosingNotes = string.Empty;
            var summary = await _posSessionService.GetSessionSummaryAsync(CurrentSession.Id);

            CloseBalanceRows.Clear();
            foreach (var row in summary.Balances)
            {
                CloseBalanceRows.Add(new PosCloseBalanceRow(row) { CountedBalance = row.ExpectedBalance });
            }

            IsCloseRegisterModalOpen = true;
        }

        [RelayCommand]
        private void CloseCloseRegisterModal()
        {
            IsCloseRegisterModalOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmCloseRegister()
        {
            if (CurrentSession == null) return;

            try
            {
                var countedBalances = CloseBalanceRows.ToDictionary(r => r.Summary.PosPaymentMethodId, r => r.CountedBalance);
                var user = UserSession.CurrentUser?.Username ?? "Cashier";
                await _posSessionService.CloseSessionAsync(CurrentSession.Id, countedBalances, user, ClosingNotes);

                IsCloseRegisterModalOpen = false;
                CurrentSession = null;
                // Deliberately not re-prompting here: the next register is opened when the cashier
                // is ready to sell again (via the Sales tab), not forced immediately on close.
            }
            catch (Exception ex)
            {
                CloseRegisterErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task OpenCashMovementModal(string movementType)
        {
            if (CurrentSession == null) return;

            CashMovementErrorMessage = string.Empty;
            CashMovementType = movementType;
            CashMovementAmount = 0;
            CashMovementReason = string.Empty;

            var connection = _journalService.Database.Connection;
            var methods = await connection.Table<PosPaymentMethod>().ToListAsync();
            CashMovementMethods.Clear();
            foreach (var m in methods) CashMovementMethods.Add(m);
            CashMovementMethod = methods.FirstOrDefault();

            IsCashMovementModalOpen = true;
        }

        [RelayCommand]
        private void CloseCashMovementModal()
        {
            IsCashMovementModalOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmCashMovement()
        {
            if (CurrentSession == null || CashMovementMethod == null) return;

            try
            {
                var user = UserSession.CurrentUser?.Username ?? "Cashier";
                await _posSessionService.RecordCashMovementAsync(
                    CurrentSession.Id, CashMovementMethod.Id, CashMovementType, CashMovementAmount, CashMovementReason, user);

                IsCashMovementModalOpen = false;

                // Keep the close-register summary in sync if it's open behind this modal.
                if (IsCloseRegisterModalOpen)
                {
                    await OpenCloseRegisterModal();
                }
            }
            catch (Exception ex)
            {
                CashMovementErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private void SwitchOrdersViewMode(string mode)
        {
            OrdersViewMode = mode;
            OnPropertyChanged(nameof(IsOrdersModeSelected));
            OnPropertyChanged(nameof(IsSessionsModeSelected));

            if (mode == "Sessions")
            {
                _ = LoadAllSessionsAsync();
            }
        }

        private async Task LoadAllSessionsAsync()
        {
            var sessions = await _posSessionService.GetAllSessionsAsync();
            AllSessions.Clear();
            foreach (var s in sessions) AllSessions.Add(s);
        }

        [RelayCommand]
        private async Task SelectSessionForView(PosSession? session)
        {
            if (session == null) return;
            SelectedSessionForView = session;
            SelectedSessionSummary = await _posSessionService.GetSessionSummaryAsync(session.Id);
        }

        [RelayCommand]
        private async Task ExportSessionToExcel()
        {
            if (SelectedSessionSummary == null) return;

            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
            {
                return;
            }

            var summary = SelectedSessionSummary;
            var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Session as Excel",
                DefaultExtension = ".xlsx",
                SuggestedFileName = $"{summary.Session.SessionNumber}",
                FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } } }
            });
            if (file == null) return;

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var balanceSheet = workbook.Worksheets.Add("Balances");
            var balanceHeaders = new[] { "Payment Method", "Opening", "Sales", "Cash In", "Cash Out", "Expected", "Counted", "Difference" };
            for (var i = 0; i < balanceHeaders.Length; i++)
            {
                balanceSheet.Cell(1, i + 1).Value = balanceHeaders[i];
                balanceSheet.Cell(1, i + 1).Style.Font.Bold = true;
            }
            var row = 2;
            foreach (var b in summary.Balances)
            {
                balanceSheet.Cell(row, 1).Value = b.PaymentMethodName;
                balanceSheet.Cell(row, 2).Value = b.OpeningBalance;
                balanceSheet.Cell(row, 3).Value = b.SalesTotal;
                balanceSheet.Cell(row, 4).Value = b.CashIn;
                balanceSheet.Cell(row, 5).Value = b.CashOut;
                balanceSheet.Cell(row, 6).Value = b.ExpectedBalance;
                balanceSheet.Cell(row, 7).Value = b.CountedClosingBalance ?? 0;
                balanceSheet.Cell(row, 8).Value = b.Difference ?? 0;
                row++;
            }
            balanceSheet.Columns().AdjustToContents();

            var ordersSheet = workbook.Worksheets.Add("Orders");
            var orderHeaders = new[] { "Reference", "Customer", "Date", "Total" };
            for (var i = 0; i < orderHeaders.Length; i++)
            {
                ordersSheet.Cell(1, i + 1).Value = orderHeaders[i];
                ordersSheet.Cell(1, i + 1).Style.Font.Bold = true;
            }
            row = 2;
            foreach (var o in summary.Orders)
            {
                ordersSheet.Cell(row, 1).Value = o.SalesOrder.SONumber;
                ordersSheet.Cell(row, 2).Value = o.CustomerName;
                ordersSheet.Cell(row, 3).Value = o.SalesOrder.OrderDate;
                ordersSheet.Cell(row, 4).Value = o.SalesOrder.TotalAmount;
                row++;
            }
            ordersSheet.Columns().AdjustToContents();

            workbook.SaveAs(file.Path.LocalPath);
        }

        [RelayCommand]
        private async Task Checkout()
        {
            if (CartItems.Count == 0) return;

            PaymentErrorMessage = null;
            if (CurrentSession == null)
            {
                PaymentErrorMessage = "No register session is open. Open one before ringing up sales.";
                return;
            }
            if (Tenders.Count == 0)
            {
                PaymentErrorMessage = "Add at least one payment (cash, mobile money, etc.) before completing the sale.";
                return;
            }
            if (RemainingDue > 0.001m)
            {
                PaymentErrorMessage = $"Payment is short by {RemainingDue:N2} {ActiveCheckoutCurrency}. Add another payment to cover the full amount.";
                return;
            }

            try
            {
                var user = UserSession.CurrentUser?.Username ?? "Cashier";
                var connection = _journalService.Database.Connection;
                var postedTotal = CheckoutTotalAmount;
                var journalScale = TotalAmount > 0 ? postedTotal / TotalAmount : 1m;

                // Make sure we have a valid customer
                Customer? actualCustomer = SelectedCustomer;
                var customerId = actualCustomer?.Id ?? 0;
                if (customerId == 0)
                {
                    var walkIn = await _customerService.EnsureWalkInCustomerAsync();
                    customerId = walkIn.Id;
                    actualCustomer = walkIn;
                }

                // 1. Create SalesOrder record
                var orderNumber = await _salesOrderService.GeneratePosNumberAsync();

                var order = new SalesOrder
                {
                    SONumber = orderNumber,
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    QuotationDate = DateTime.Now,
                    TotalAmount = CheckoutTotalAmount,
                    Currency = ActiveCheckoutCurrency,
                    Status = "Delivered",
                    BillingStatus = AutoCreateInvoice ? "Invoiced" : "Waiting Invoice",
                    DeliveryStatus = "Delivered",
                    IsPosSale = true,
                    PosPaymentMethodId = Tenders[0].Method.Id,
                    PosSessionId = CurrentSession.Id,
                    CreatedByUsername = user
                };

                await connection.InsertAsync(order);

                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(user, "Create", "SalesOrder", order.Id, order);
                }

                // 2. Process each item (insert SalesOrderItem, stock deduction, costing batch tracking)
                var itemsList = new List<SalesOrderItem>();
                foreach (var item in CartItems)
                {
                    var orderItem = new SalesOrderItem
                    {
                        SalesOrderId = order.Id,
                        ProductId = item.Product.Id,
                        QuantityOrdered = item.Quantity,
                        QuantityDelivered = item.Quantity,
                        QuantityInvoiced = AutoCreateInvoice ? item.Quantity : 0,
                        UnitPrice = item.UnitPrice,
                        TaxId = item.Product.SalesTaxId
                    };
                    await connection.InsertAsync(orderItem);
                    itemsList.Add(orderItem);

                    // Add Stock Movement OUT (reduces product stock quantity and decrements FIFO batch exactly once)
                    // Revenue/AR is always posted explicitly below (per payment tender), so the
                    // COGS-only path here is used regardless of AutoCreateInvoice.
                    await _inventoryService.AddStockMovementAsync(
                        item.Product.Id,
                        item.Quantity,
                        "OUT",
                        $"POS Sale: {order.SONumber}",
                        user,
                        customCost: null,
                        unitPrice: item.UnitPrice,
                        postSalesRevenueJournal: false
                    );
                }

                // POS sales are handed over immediately, so record the shipment now - without this,
                // Delivery Slip / Packing List printing (available from the Customer Orders list)
                // would find no delivery record and silently print nothing.
                var deliveryNote = new DeliveryNote
                {
                    DeliveryNoteNumber = $"DN-{order.SONumber}",
                    SalesOrderId = order.Id,
                    CustomerId = order.CustomerId,
                    ShipDate = order.OrderDate,
                    Status = "Shipped",
                    CreatedByUsername = user
                };
                await connection.InsertAsync(deliveryNote);
                foreach (var orderItem in itemsList)
                {
                    await connection.InsertAsync(new DeliveryNoteLine
                    {
                        DeliveryNoteId = deliveryNote.Id,
                        SalesOrderItemId = orderItem.Id,
                        ProductId = orderItem.ProductId,
                        Quantity = orderItem.QuantityDelivered
                    });
                }

                // 3. Double Entry Accounting Entries (always posted, regardless of AutoCreateInvoice -
                // that flag only controls which PDF is printed, not how the sale is booked)

                // a) Invoice Journal Entry: Debit AR / Credit Revenue for the full sale
                var salesJournal = await connection.Table<Journal>().Where(j => j.Type == "Sales").FirstOrDefaultAsync();
                if (salesJournal != null)
                {
                    var entryCount = await connection.Table<JournalEntry>().Where(e => e.JournalId == salesJournal.Id).CountAsync();
                    var entryNumber = $"{salesJournal.SequencePrefix}/{DateTime.Now.Year}/{(entryCount + 1):D5}";

                    var invoiceEntry = new JournalEntry
                    {
                        EntryNumber = entryNumber,
                        JournalId = salesJournal.Id,
                        Date = DateTime.Now,
                        Reference = $"POS Invoice: {order.SONumber}",
                        State = "Posted"
                    };
                    await connection.InsertAsync(invoiceEntry);

                    // Accounts
                    var arAccount = await connection.Table<Account>().Where(a => a.Code == "111000").FirstOrDefaultAsync();
                    int arAccountId = arAccount?.Id ?? 3; // Accounts Receivable

                    // Debit Accounts Receivable
                    await connection.InsertAsync(new JournalLine
                    {
                        JournalEntryId = invoiceEntry.Id,
                        AccountId = arAccountId,
                        Label = $"POS Invoice - {order.SONumber}",
                        Debit = postedTotal,
                        Credit = 0
                    });

                    // Credit Revenue for items
                    foreach (var item in CartItems)
                    {
                        int incomeAccountId = item.Product.IncomeAccountId ?? 0;
                        if (incomeAccountId == 0)
                        {
                            var revAccount = await connection.Table<Account>().Where(a => a.Code == "401000").FirstOrDefaultAsync();
                            incomeAccountId = revAccount?.Id ?? 13; // Product Sales Revenue
                        }

                        await connection.InsertAsync(new JournalLine
                        {
                            JournalEntryId = invoiceEntry.Id,
                            AccountId = incomeAccountId,
                            ProductId = item.Product.Id,
                            Label = $"POS Revenue - {item.Product.Name} (Qty: {item.Quantity})",
                            Debit = 0,
                            Credit = Math.Round(item.Subtotal * journalScale, 2)
                        });
                    }
                }

                // b) Payment Journal Entry per tender (Debit that tender's cash/bank/momo account, Credit AR)
                // so every payment method is separately trackable, and split payments (e.g. part cash,
                // part mobile money) are each booked to the right account.
                var arAccountForPayments = await connection.Table<Account>().Where(a => a.Code == "111000").FirstOrDefaultAsync();
                int arAccountIdForPayments = arAccountForPayments?.Id ?? 3;
                var remainingToApply = postedTotal;

                foreach (var tender in Tenders)
                {
                    if (remainingToApply <= 0) break;
                    var applied = Math.Min(tender.Amount, remainingToApply);
                    if (applied <= 0) continue;
                    remainingToApply -= applied;

                    await connection.InsertAsync(new PosSalePayment
                    {
                        SalesOrderId = order.Id,
                        PosPaymentMethodId = tender.Method.Id,
                        Amount = applied,
                        Date = DateTime.Now
                    });

                    var paymentJournal = await connection.Table<Journal>().Where(j => j.Id == tender.Method.JournalId).FirstOrDefaultAsync();
                    if (paymentJournal == null) continue;

                    var entryCount = await connection.Table<JournalEntry>().Where(e => e.JournalId == paymentJournal.Id).CountAsync();
                    var entryNumber = $"{paymentJournal.SequencePrefix}/{DateTime.Now.Year}/{(entryCount + 1):D5}";

                    var paymentEntry = new JournalEntry
                    {
                        EntryNumber = entryNumber,
                        JournalId = paymentJournal.Id,
                        Date = DateTime.Now,
                        Reference = $"POS Payment ({tender.Method.Name}): {order.SONumber}",
                        State = "Posted"
                    };
                    await connection.InsertAsync(paymentEntry);

                    // Default/Bank Account for the payment journal
                    int cashAccountId = paymentJournal.DefaultAccountId ?? paymentJournal.BankAccountId ?? 0;
                    if (cashAccountId == 0)
                    {
                        var cashAccountObj = await connection.Table<Account>().Where(a => a.Code.StartsWith("101") || a.Code.StartsWith("102")).FirstOrDefaultAsync();
                        cashAccountId = cashAccountObj?.Id ?? 1; // Fallback to Cash/Bank
                    }

                    // Debit Cash/Bank/Mobile Money
                    await connection.InsertAsync(new JournalLine
                    {
                        JournalEntryId = paymentEntry.Id,
                        AccountId = cashAccountId,
                        Label = $"POS {tender.Method.Name} Inflow - {order.SONumber}",
                        Debit = applied,
                        Credit = 0
                    });

                    // Credit Accounts Receivable (clears AR!)
                    await connection.InsertAsync(new JournalLine
                    {
                        JournalEntryId = paymentEntry.Id,
                        AccountId = arAccountIdForPayments,
                        Label = $"POS Payment Receipt ({tender.Method.Name}) - {order.SONumber}",
                        Debit = 0,
                        Credit = applied
                    });
                }

                // 4. Generate A4 Tax Invoice PDF or 80mm POS Receipt PDF
                if (AutoCreateInvoice)
                {
                    var allProducts = await _inventoryService.GetAllProductsAsync();
                    var allTaxes = await connection.Table<Tax>().ToListAsync();
                    var pdfService = new SalesOrderPdfService(_settingsService);
                    
                    LastReceiptPath = pdfService.GenerateSalesOrderPdf(
                        order, 
                        itemsList, 
                        allProducts, 
                        allTaxes, 
                        actualCustomer, 
                        asInvoice: true
                    );
                    LastReceiptText = $"Invoice Generated Successfully!\nSaved to: {LastReceiptPath}";
                }
                else
                {
                    LastReceiptPath = _receiptService.GenerateReceiptFromCart(user, CartItems, postedTotal, TotalTendered, ChangeDue);
                    LastReceiptText = $"Receipt Generated Successfully!\nSaved to: {LastReceiptPath}";
                }

                IsCheckoutSuccess = true;

                // Clear Cart
                CartItems.Clear();
                RecalculateTotal();
                Tenders.Clear();
                TenderAmount = 0;
                RaiseTenderTotalsChanged();
                IsPaymentPanelVisible = false;
                SelectedCustomer = null;
                CustomerSearchText = string.Empty;

                // Show Receipt Modal
                IsReceiptModalOpen = true;

                // Refresh Inventory List
                await LoadProducts(); 
            }
            catch (Exception ex)
            {
                IsCheckoutSuccess = false;
                LastReceiptText = $"Error during checkout: {ex.Message}";
                IsReceiptModalOpen = true;
            }
        }

        [RelayCommand]
        private void CloseReceipt()
        {
            IsReceiptModalOpen = false;
        }

        [RelayCommand]
        private void PrintReceipt()
        {
             if (!string.IsNullOrEmpty(LastReceiptPath) && System.IO.File.Exists(LastReceiptPath))
             {
                 try
                 {
                     new System.Diagnostics.Process
                     {
                         StartInfo = new System.Diagnostics.ProcessStartInfo(LastReceiptPath)
                         {
                             UseShellExecute = true
                         }
                     }.Start();
                 }
                 catch (Exception ex)
                 {
                     LastReceiptText += $"\nCould not open PDF automatically: {ex.Message}";
                 }
             }
        }

        [RelayCommand]
        private async Task OpenOrderDetails(SalesOrderListItem? item)
        {
            if (item == null) return;
            var so = item.SalesOrder;
            DetailedOrder = so;
            DetailedCustomerName = item.CustomerName;

            try
            {
                var dbItems = await _salesOrderService.GetItemsAsync(so.Id);
                var products = await _inventoryService.GetAllProductsAsync();
                var taxes = await _taxService.GetAllTaxesAsync();

                var rows = new List<POSDetailedOrderItemRow>();
                decimal subtotal = 0;
                decimal taxTotal = 0;
                var taxBreakdown = new Dictionary<int, (Tax Tax, decimal Amount)>();

                foreach (var it in dbItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == it.ProductId);
                    var taxId = it.TaxId ?? product?.SalesTaxId;
                    var tax = taxId.HasValue ? taxes.FirstOrDefault(t => t.Id == taxId.Value) : null;

                    var rowSubtotal = it.QuantityOrdered * it.UnitPrice;
                    decimal rowTotal = rowSubtotal;
                    decimal taxAmount = 0;

                    if (tax != null)
                    {
                        if (tax.IncludedInPrice == "Include")
                        {
                            decimal basePrice;
                            if (tax.Computation == "Percentage")
                            {
                                basePrice = rowSubtotal / (1 + (tax.Amount / 100));
                            }
                            else
                            {
                                basePrice = Math.Max(0, rowSubtotal - (it.QuantityOrdered * tax.Amount));
                            }
                            taxAmount = rowSubtotal - basePrice;
                            subtotal += basePrice;
                            rowTotal = rowSubtotal;
                        }
                        else
                        {
                            subtotal += rowSubtotal;
                            if (tax.Computation == "Percentage")
                            {
                                taxAmount = rowSubtotal * (tax.Amount / 100);
                            }
                            else
                            {
                                taxAmount = it.QuantityOrdered * tax.Amount;
                            }
                            rowTotal = rowSubtotal + taxAmount;
                        }

                        if (taxAmount > 0)
                        {
                            if (taxBreakdown.ContainsKey(tax.Id))
                            {
                                var existing = taxBreakdown[tax.Id];
                                taxBreakdown[tax.Id] = (tax, existing.Amount + taxAmount);
                            }
                            else
                            {
                                taxBreakdown[tax.Id] = (tax, taxAmount);
                            }
                            taxTotal += taxAmount;
                        }
                    }
                    else
                    {
                        subtotal += rowSubtotal;
                    }

                    rows.Add(new POSDetailedOrderItemRow
                    {
                        ProductName = product?.Name ?? $"Product ID: {it.ProductId}",
                        Quantity = it.QuantityOrdered,
                        UnitPrice = it.UnitPrice,
                        Discount = 0,
                        TaxName = tax != null ? $"{tax.Name} ({tax.Amount}%)" : "None",
                        Total = rowTotal
                    });
                }

                DetailedOrderItems = new ObservableCollection<POSDetailedOrderItemRow>(rows);
                DetailedSubtotal = subtotal;
                DetailedTaxes = taxTotal;
                DetailedTotal = subtotal + taxTotal;

                if (taxBreakdown.Count == 0)
                {
                    DetailedTaxBreakdownText = "Taxes: None";
                }
                else
                {
                    var parts = taxBreakdown.Values.Select(tb => 
                        $"{tb.Tax.Name} ({tb.Tax.Amount}%): {tb.Amount:N2} {(tb.Tax.IncludedInPrice == "Include" ? "Incl." : "")}");
                    DetailedTaxBreakdownText = "Taxes breakdown:\n" + string.Join("\n", parts);
                }

                var paymentRows = new List<POSDetailedPaymentRow>();
                var connection = _journalService.Database.Connection;
                var tenderRecords = await connection.Table<PosSalePayment>()
                    .Where(p => !p.IsDeleted && p.SalesOrderId == so.Id)
                    .ToListAsync();

                if (tenderRecords.Count > 0)
                {
                    var methods = await connection.Table<PosPaymentMethod>().ToListAsync();
                    foreach (var tenderRecord in tenderRecords)
                    {
                        var pm = methods.FirstOrDefault(p => p.Id == tenderRecord.PosPaymentMethodId);
                        paymentRows.Add(new POSDetailedPaymentRow
                        {
                            Date = tenderRecord.Date,
                            PaymentMethod = pm?.Name ?? "Unknown POS Payment Method",
                            Amount = tenderRecord.Amount
                        });
                    }
                }
                else if (so.PosPaymentMethodId.HasValue)
                {
                    // Legacy sale recorded before split-tender support: one payment method for the full amount.
                    var pm = await connection.Table<PosPaymentMethod>().Where(p => p.Id == so.PosPaymentMethodId.Value).FirstOrDefaultAsync();
                    paymentRows.Add(new POSDetailedPaymentRow
                    {
                        Date = so.OrderDate,
                        PaymentMethod = pm?.Name ?? "Unknown POS Payment Method",
                        Amount = so.TotalAmount
                    });
                }
                else
                {
                    paymentRows.Add(new POSDetailedPaymentRow
                    {
                        Date = so.OrderDate,
                        PaymentMethod = "POS Cash/Bank",
                        Amount = so.TotalAmount
                    });
                }
                DetailedPayments = new ObservableCollection<POSDetailedPaymentRow>(paymentRows);

                var returns = await _returnsService.GetCustomerReturnsForOrderAsync(so.Id);
                DetailedReturns = new ObservableCollection<CustomerReturn>(returns);
                OnPropertyChanged(nameof(HasReturns));

                IsOrderDetailsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading POS order details: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CloseOrderDetails()
        {
            IsOrderDetailsOpen = false;
        }

        [RelayCommand]
        private void ToggleCustomerGroup(CustomerOrderGroup? group)
        {
            if (group == null) return;
            group.IsExpanded = !group.IsExpanded;
        }

        // --- POS Refund ---
        [ObservableProperty] private bool _isReturnModalOpen;
        [ObservableProperty] private string _returnErrorMessage = string.Empty;
        [ObservableProperty] private SalesOrder _returnTargetOrder = new();
        public ObservableCollection<SalesOrderReturnRow> ReturnRows { get; } = new();

        [RelayCommand]
        private async Task OpenReturnOrder(SalesOrderListItem? item)
        {
            var target = item?.SalesOrder ?? DetailedOrder;
            if (target == null) return;

            ReturnErrorMessage = string.Empty;
            var orderItems = await _salesOrderService.GetItemsAsync(target.Id);
            var products = await _inventoryService.GetAllProductsAsync();

            ReturnRows.Clear();
            foreach (var it in orderItems)
            {
                if (it.QuantityDelivered <= 0) continue;

                var prod = products.FirstOrDefault(p => p.Id == it.ProductId);
                ReturnRows.Add(new SalesOrderReturnRow
                {
                    ItemId = it.Id,
                    ProductId = it.ProductId,
                    ProductName = prod?.Name ?? "Unknown Product",
                    QuantityDelivered = it.QuantityDelivered,
                    QuantityToReturn = it.QuantityDelivered,
                    RefundAmount = it.QuantityDelivered * it.UnitPrice,
                    Condition = "Resaleable",
                    Reason = "POS Refund"
                });
            }

            if (ReturnRows.Count == 0)
            {
                BarcodeStatusMessage = "No delivered items found on this order to return.";
                return;
            }

            ReturnTargetOrder = target;
            IsReturnModalOpen = true;
        }

        [RelayCommand]
        private async Task SubmitReturn()
        {
            ReturnErrorMessage = string.Empty;
            if (ReturnRows.Any(r => r.QuantityToReturn < 0))
            {
                ReturnErrorMessage = "Return quantity cannot be negative.";
                return;
            }
            if (ReturnRows.Any(r => r.QuantityToReturn > r.QuantityDelivered))
            {
                ReturnErrorMessage = "Return quantity cannot exceed delivered quantity.";
                return;
            }

            try
            {
                var payload = ReturnRows
                    .Where(r => r.QuantityToReturn > 0)
                    .Select(r => (r.ItemId, r.QuantityToReturn, r.Condition, r.Reason, r.RefundAmount))
                    .ToList();

                if (payload.Count == 0)
                {
                    ReturnErrorMessage = "Please specify at least one item and quantity to return.";
                    return;
                }

                await _returnsService.ProcessSalesOrderReturnAsync(ReturnTargetOrder.Id, payload, UserSession.CurrentUser?.Username ?? "Cashier");
                IsReturnModalOpen = false;

                // Refresh whatever's currently on screen so the refunded quantities show up immediately
                var updatedList = await _salesOrderService.GetPosSalesOrdersAsync();
                var updatedItem = updatedList.FirstOrDefault(o => o.SalesOrder.Id == ReturnTargetOrder.Id);
                if (updatedItem != null)
                {
                    await OpenOrderDetails(updatedItem);
                }

                await LoadPosOrdersAsync();
            }
            catch (Exception ex)
            {
                ReturnErrorMessage = $"Return failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CloseReturnModal()
        {
            IsReturnModalOpen = false;
        }
    }

    public class CustomerOrderGroup : ObservableObject
    {
        public CustomerOrderGroup(string customerName, List<SalesOrderListItem> orders)
        {
            CustomerName = customerName;
            Orders = new ObservableCollection<SalesOrderListItem>(orders);
        }

        public string CustomerName { get; }
        public ObservableCollection<SalesOrderListItem> Orders { get; }
        public int OrderCount => Orders.Count;
        public decimal TotalAmount => Orders.Sum(o => o.SalesOrder.TotalAmount);

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
    }

    public partial class PosOpeningBalanceRow : ObservableObject
    {
        public PosOpeningBalanceRow(PosPaymentMethod method)
        {
            Method = method;
        }

        public PosPaymentMethod Method { get; }
        public string MethodName => Method.Name;

        [ObservableProperty] private decimal _amount;
    }

    public partial class PosCloseBalanceRow : ObservableObject
    {
        public PosCloseBalanceRow(PosSessionBalanceRow summary)
        {
            Summary = summary;
        }

        public PosSessionBalanceRow Summary { get; }
        public string MethodName => Summary.PaymentMethodName;
        public decimal OpeningBalance => Summary.OpeningBalance;
        public decimal SalesTotal => Summary.SalesTotal;
        public decimal CashIn => Summary.CashIn;
        public decimal CashOut => Summary.CashOut;
        public decimal ExpectedBalance => Summary.ExpectedBalance;

        [ObservableProperty] private decimal _countedBalance;

        public decimal Difference => CountedBalance - ExpectedBalance;

        partial void OnCountedBalanceChanged(decimal value)
        {
            OnPropertyChanged(nameof(Difference));
        }
    }

    public class POSDetailedOrderItemRow
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; } = 0;
        public string TaxName { get; set; } = "None";
        public decimal Total { get; set; }
    }

    public class POSDetailedPaymentRow
    {
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
