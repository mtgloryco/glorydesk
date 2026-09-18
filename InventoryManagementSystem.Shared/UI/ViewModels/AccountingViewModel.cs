using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public class CustomerInvoiceRow
    {
        public int Id { get; init; }
        public string InvoiceNumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public DateTime InvoiceDate { get; init; }
        public DateTime DueDate { get; init; }
        public int DaysOverdue { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal Balance { get; init; }
        public string Status { get; init; } = "Unpaid";
        public string StatusColor { get; init; } = "#3B82F6";
        public bool CanPay => Balance > 0.01m;
    }

    public class VendorBillRow
    {
        public int Id { get; init; }
        public string BillNumber { get; init; } = string.Empty;
        public string SupplierName { get; init; } = string.Empty;
        public DateTime BillDate { get; init; }
        public DateTime DueDate { get; init; }
        public int DaysOverdue { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal Balance { get; init; }
        public string Status { get; init; } = "Unpaid";
        public string StatusColor { get; init; } = "#3B82F6";
        public bool CanPay => Balance > 0.01m;
    }

    public class AccountDisplayRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string AccountType { get; init; } = string.Empty;
        public string Currency { get; init; } = "RWF";
        public decimal Balance { get; init; }
        public string Group { get; init; } = string.Empty;
    }

    public class AccountingReportLineRow
    {
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public decimal Balance { get; init; }
        public int Level { get; init; }
        public Avalonia.Thickness Margin => new Avalonia.Thickness((Math.Max(1, Level) - 1) * 20, 2, 4, 2);
        public Avalonia.Media.FontWeight FontWeight => Level <= 1 ? Avalonia.Media.FontWeight.Bold : (Level == 2 ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal);
    }

    public class EligibleSalesOrderRow
    {
        public int Id { get; init; }
        public string SONumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string DeliveryStatus { get; init; } = "Pending";
        public string BillingStatus { get; init; } = "Waiting Invoice";
        public bool IsDelivered => DeliveryStatus == "Delivered";
        public string DeliveryStatusColor => IsDelivered ? "#10B981" : "#F59E0B";
    }

    public class EligiblePurchaseOrderRow
    {
        public int Id { get; init; }
        public string PONumber { get; init; } = string.Empty;
        public string SupplierName { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string ReceiptStatus { get; init; } = "Pending";
        public string BillingStatus { get; init; } = "Waiting Bill";
        public bool IsReceived => ReceiptStatus == "Received";
        public string ReceiptStatusColor => IsReceived ? "#10B981" : "#F59E0B";
    }

    public partial class AccountingInvoiceLineItem : ObservableObject
    {
        [ObservableProperty] private Product? _selectedProduct;
        [ObservableProperty] private decimal _quantity = 1;
        [ObservableProperty] private decimal _unitPrice;
        [ObservableProperty] private Tax? _selectedTax;

        public decimal LineTotal => Quantity * UnitPrice;
        public decimal TaxAmount
        {
            get
            {
                if (SelectedTax == null || SelectedTax.Amount <= 0) return 0m;
                if (SelectedTax.Computation == "Fixed")
                    return SelectedTax.Amount * Quantity;
                return Math.Round(LineTotal * (SelectedTax.Amount / 100m), 2);
            }
        }
        public decimal TotalWithTax => LineTotal + TaxAmount;

        public Action? OnLineChanged { get; set; }

        partial void OnSelectedProductChanged(Product? value)
        {
            if (value != null && UnitPrice == 0)
            {
                UnitPrice = value.Price;
            }
            NotifyCalculations();
        }

        partial void OnQuantityChanged(decimal value) => NotifyCalculations();
        partial void OnUnitPriceChanged(decimal value) => NotifyCalculations();
        partial void OnSelectedTaxChanged(Tax? value) => NotifyCalculations();

        private void NotifyCalculations()
        {
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(TotalWithTax));
            OnLineChanged?.Invoke();
        }
    }

    public partial class AccountingBillLineItem : ObservableObject
    {
        [ObservableProperty] private Product? _selectedProduct;
        [ObservableProperty] private decimal _quantity = 1;
        [ObservableProperty] private decimal _unitCost;
        [ObservableProperty] private Tax? _selectedTax;

        public decimal LineTotal => Quantity * UnitCost;
        public decimal TaxAmount
        {
            get
            {
                if (SelectedTax == null || SelectedTax.Amount <= 0) return 0m;
                if (SelectedTax.Computation == "Fixed")
                    return SelectedTax.Amount * Quantity;
                return Math.Round(LineTotal * (SelectedTax.Amount / 100m), 2);
            }
        }
        public decimal TotalWithTax => LineTotal + TaxAmount;

        public Action? OnLineChanged { get; set; }

        partial void OnSelectedProductChanged(Product? value)
        {
            if (value != null && UnitCost == 0)
            {
                UnitCost = value.Cost;
            }
            NotifyCalculations();
        }

        partial void OnQuantityChanged(decimal value) => NotifyCalculations();
        partial void OnUnitCostChanged(decimal value) => NotifyCalculations();
        partial void OnSelectedTaxChanged(Tax? value) => NotifyCalculations();

        private void NotifyCalculations()
        {
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(TotalWithTax));
            OnLineChanged?.Invoke();
        }
    }

    public partial class AccountingViewModel : ViewModelBase
    {
        private readonly SalesOrderService _salesOrderService;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly AgingReportService _agingReportService;
        private readonly PaymentService _paymentService;
        private readonly AccountService _accountService;
        private readonly AccountingReportService _accountingReportService;
        private readonly JournalService _journalService;
        private readonly TaxService _taxService;
        private readonly VatExportService _vatExportService;
        private readonly MonthCloseService _monthCloseService;
        private readonly CustomerService _customerService;
        private readonly SupplierService _supplierService;
        private readonly InventoryService _inventoryService;
        private readonly SettingsService _settingsService;
        private readonly LanguageService _languageService;
        private readonly LicenseService _licenseService;
        private readonly SalesOrderPdfService _pdfService;
        private readonly Action<int?>? _goToSalesOrderDetails;
        private readonly Action<int?>? _goToPurchaseOrderDetails;

        public LanguageService Language => _languageService;
        public string CurrencySymbol => _settingsService.CurrentSettings.CurrencySymbol;

        // Navigation Tabs
        [ObservableProperty] private string _selectedTab = "Invoices";
        public bool IsInvoicesTabActive => SelectedTab == "Invoices";
        public bool IsBillsTabActive => SelectedTab == "Bills";
        public bool IsChartOfAccountsTabActive => SelectedTab == "ChartOfAccounts";
        public bool IsReportsTabActive => SelectedTab == "Reports";

        partial void OnSelectedTabChanged(string value)
        {
            OnPropertyChanged(nameof(IsInvoicesTabActive));
            OnPropertyChanged(nameof(IsBillsTabActive));
            OnPropertyChanged(nameof(IsChartOfAccountsTabActive));
            OnPropertyChanged(nameof(IsReportsTabActive));

            if (value == "Invoices") _ = LoadInvoicesAsync();
            else if (value == "Bills") _ = LoadBillsAsync();
            else if (value == "ChartOfAccounts") _ = LoadAccountsAsync();
            else if (value == "Reports") _ = LoadFinancialReportAsync();
        }

        [RelayCommand]
        public void SwitchTab(string tab)
        {
            SelectedTab = tab;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TAB 1: CUSTOMER INVOICES (A/R)
        // ═══════════════════════════════════════════════════════════════════════════
        private List<CustomerInvoiceRow> _allInvoices = new();
        [ObservableProperty] private ObservableCollection<CustomerInvoiceRow> _invoices = new();
        [ObservableProperty] private CustomerInvoiceRow? _selectedInvoice;
        [ObservableProperty] private string _invoiceSearchText = string.Empty;
        [ObservableProperty] private string _selectedInvoiceFilter = "All";

        [ObservableProperty] private decimal _totalReceivable;
        [ObservableProperty] private decimal _totalOverdueReceivable;
        [ObservableProperty] private int _openInvoicesCount;
        [ObservableProperty] private int _paidInvoicesCount;

        partial void OnInvoiceSearchTextChanged(string value) => FilterInvoices();
        partial void OnSelectedInvoiceFilterChanged(string value) => FilterInvoices();

        [RelayCommand]
        public async Task LoadInvoicesAsync()
        {
            try
            {
                var orders = await _salesOrderService.GetAllSalesOrdersAsync();
                var invoiced = orders.Where(o => o.SalesOrder.BillingStatus == "Invoiced" && o.SalesOrder.Status != "Cancelled").ToList();
                var today = DateTime.Today;

                var list = new List<CustomerInvoiceRow>();
                decimal sumReceivable = 0;
                decimal sumOverdue = 0;
                int openCount = 0;
                int paidCount = 0;

                foreach (var item in invoiced)
                {
                    var so = item.SalesOrder;
                    var payments = await _paymentService.GetPaymentsForDocumentAsync("SalesOrder", so.Id);
                    var paid = payments.Sum(p => p.Amount);
                    var balance = Math.Max(0, so.TotalAmount - paid);

                    var dueDate = so.OrderDate.AddDays(30);
                    var daysOverdue = Math.Max(0, (today - dueDate.Date).Days);

                    string status;
                    string color;
                    if (balance <= 0.01m)
                    {
                        status = "Paid";
                        color = "#10B981"; // emerald
                        paidCount++;
                    }
                    else if (daysOverdue > 0)
                    {
                        status = "Overdue";
                        color = "#EF4444"; // red
                        sumReceivable += balance;
                        sumOverdue += balance;
                        openCount++;
                    }
                    else if (paid > 0)
                    {
                        status = "Partial";
                        color = "#F59E0B"; // amber
                        sumReceivable += balance;
                        openCount++;
                    }
                    else
                    {
                        status = "Unpaid";
                        color = "#3B82F6"; // blue
                        sumReceivable += balance;
                        openCount++;
                    }

                    list.Add(new CustomerInvoiceRow
                    {
                        Id = so.Id,
                        InvoiceNumber = so.SONumber,
                        CustomerName = item.CustomerName,
                        InvoiceDate = so.OrderDate,
                        DueDate = dueDate,
                        DaysOverdue = daysOverdue,
                        TotalAmount = so.TotalAmount,
                        PaidAmount = paid,
                        Balance = balance,
                        Status = status,
                        StatusColor = color
                    });
                }

                _allInvoices = list.OrderByDescending(i => i.InvoiceDate).ToList();
                TotalReceivable = sumReceivable;
                TotalOverdueReceivable = sumOverdue;
                OpenInvoicesCount = openCount;
                PaidInvoicesCount = paidCount;

                FilterInvoices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading invoices: {ex.Message}");
            }
        }

        private void FilterInvoices()
        {
            var query = InvoiceSearchText?.Trim().ToLower() ?? string.Empty;
            var filter = SelectedInvoiceFilter ?? "All";

            var filtered = _allInvoices.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(i =>
                    i.InvoiceNumber.ToLower().Contains(query) ||
                    i.CustomerName.ToLower().Contains(query));
            }

            if (filter == "Unpaid")
                filtered = filtered.Where(i => i.Balance > 0.01m);
            else if (filter == "Overdue")
                filtered = filtered.Where(i => i.Status == "Overdue");
            else if (filter == "Paid")
                filtered = filtered.Where(i => i.Status == "Paid");

            Invoices = new ObservableCollection<CustomerInvoiceRow>(filtered);
        }

        [RelayCommand]
        public void PrintInvoice(CustomerInvoiceRow? row)
        {
            if (row == null) return;
            try
            {
                _ = Task.Run(async () =>
                {
                    var orders = await _salesOrderService.GetAllSalesOrdersAsync();
                    var item = orders.FirstOrDefault(o => o.SalesOrder.Id == row.Id);
                    if (item == null) return;

                    var items = await _salesOrderService.GetItemsAsync(row.Id);
                    var products = await _inventoryService.GetAllProductsAsync();
                    var taxes = await _taxService.GetAllTaxesAsync();
                    var customers = await _customerService.GetAllCustomersAsync();
                    var customer = customers.FirstOrDefault(c => c.Id == item.SalesOrder.CustomerId);

                    var path = _pdfService.GenerateSalesOrderPdf(
                        item.SalesOrder, items, products, taxes, customer, asInvoice: true);

                    if (File.Exists(path) && !OperatingSystem.IsBrowser())
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                        {
                            UseShellExecute = true
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing invoice: {ex.Message}");
            }
        }

        // ── Invoices Tab: Dropdown Creation Flows ─────────────────────────────────
        // 1. From Sales Order Modal
        [ObservableProperty] private bool _isInvoiceFromSoModalOpen;
        [ObservableProperty] private ObservableCollection<EligibleSalesOrderRow> _eligibleSalesOrders = new();
        private List<EligibleSalesOrderRow> _allEligibleSalesOrders = new();
        [ObservableProperty] private EligibleSalesOrderRow? _selectedEligibleSalesOrder;
        [ObservableProperty] private string _soModalSearchText = string.Empty;
        [ObservableProperty] private string _invoiceFromSoErrorMessage = string.Empty;

        partial void OnSoModalSearchTextChanged(string value) => FilterEligibleSalesOrders();

        [RelayCommand]
        public async Task OpenInvoiceFromSalesOrderModalAsync()
        {
            InvoiceFromSoErrorMessage = string.Empty;
            SoModalSearchText = string.Empty;
            SelectedEligibleSalesOrder = null;

            try
            {
                var orders = await _salesOrderService.GetAllSalesOrdersAsync();
                _allEligibleSalesOrders = orders
                    .Where(o => o.SalesOrder.BillingStatus != "Invoiced" && o.SalesOrder.Status != "Cancelled")
                    .Select(o => new EligibleSalesOrderRow
                    {
                        Id = o.SalesOrder.Id,
                        SONumber = o.SalesOrder.SONumber,
                        CustomerName = o.CustomerName,
                        OrderDate = o.SalesOrder.OrderDate,
                        TotalAmount = o.SalesOrder.TotalAmount,
                        DeliveryStatus = o.SalesOrder.DeliveryStatus,
                        BillingStatus = o.SalesOrder.BillingStatus
                    })
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                FilterEligibleSalesOrders();
                IsInvoiceFromSoModalOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading eligible sales orders: {ex.Message}");
            }
        }

        private void FilterEligibleSalesOrders()
        {
            var query = SoModalSearchText?.Trim().ToLower() ?? string.Empty;
            var filtered = _allEligibleSalesOrders.AsEnumerable();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(o =>
                    o.SONumber.ToLower().Contains(query) ||
                    o.CustomerName.ToLower().Contains(query));
            }
            EligibleSalesOrders = new ObservableCollection<EligibleSalesOrderRow>(filtered);
            SelectedEligibleSalesOrder = EligibleSalesOrders.FirstOrDefault();
        }

        [RelayCommand]
        public async Task GenerateInvoiceFromSelectedSoAsync()
        {
            InvoiceFromSoErrorMessage = string.Empty;
            if (SelectedEligibleSalesOrder == null)
            {
                InvoiceFromSoErrorMessage = "Please select a sales order to invoice.";
                return;
            }

            try
            {
                var soId = SelectedEligibleSalesOrder.Id;
                var items = await _salesOrderService.GetItemsAsync(soId);

                // Deliver remaining items if needed to recognize revenue & AR in GL
                var undelivered = items
                    .Select(i => (itemId: i.Id, quantityDelivered: i.QuantityOrdered - i.QuantityDelivered))
                    .Where(x => x.quantityDelivered > 0)
                    .ToList();

                if (undelivered.Any())
                {
                    await _salesOrderService.DeliverSalesOrderAsync(soId, undelivered);
                }

                await _salesOrderService.InvoiceSalesOrderAsync(soId);

                IsInvoiceFromSoModalOpen = false;
                await LoadInvoicesAsync();
                _ = LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                InvoiceFromSoErrorMessage = $"Failed to create invoice: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CloseInvoiceFromSoModal()
        {
            IsInvoiceFromSoModalOpen = false;
        }

        // 2. Direct Customer Invoice Modal
        [ObservableProperty] private bool _isDirectInvoiceModalOpen;
        [ObservableProperty] private ObservableCollection<Customer> _availableCustomers = new();
        [ObservableProperty] private Customer? _selectedDirectCustomer;
        [ObservableProperty] private DateTime? _directInvoiceDate = DateTime.Today;
        [ObservableProperty] private DateTime? _directInvoiceDueDate = DateTime.Today.AddDays(30);
        [ObservableProperty] private string _directInvoiceReference = string.Empty;
        [ObservableProperty] private string _directInvoiceNotes = string.Empty;
        [ObservableProperty] private ObservableCollection<AccountingInvoiceLineItem> _directInvoiceLines = new();
        [ObservableProperty] private ObservableCollection<Product> _availableProducts = new();
        [ObservableProperty] private ObservableCollection<Tax> _availableTaxes = new();
        [ObservableProperty] private decimal _directInvoiceSubtotal;
        [ObservableProperty] private decimal _directInvoiceTaxTotal;
        [ObservableProperty] private decimal _directInvoiceGrandTotal;
        [ObservableProperty] private string _directInvoiceErrorMessage = string.Empty;

        [RelayCommand]
        public async Task OpenDirectInvoiceModalAsync()
        {
            DirectInvoiceErrorMessage = string.Empty;
            DirectInvoiceReference = string.Empty;
            DirectInvoiceNotes = string.Empty;
            DirectInvoiceDate = DateTime.Today;
            DirectInvoiceDueDate = DateTime.Today.AddDays(30);

            try
            {
                var customers = await _customerService.GetAllCustomersAsync();
                var products = await _inventoryService.GetAllProductsAsync();
                var taxes = await _taxService.GetAllTaxesAsync();

                AvailableCustomers = new ObservableCollection<Customer>(customers.Where(c => c.IsActive && !c.IsDeleted));
                SelectedDirectCustomer = AvailableCustomers.FirstOrDefault();

                AvailableProducts = new ObservableCollection<Product>(products.Where(p => !p.IsDeleted));
                AvailableTaxes = new ObservableCollection<Tax>(taxes.Where(t => t.IsActive && !t.IsDeleted));

                DirectInvoiceLines.Clear();
                AddDirectInvoiceLine();

                IsDirectInvoiceModalOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error preparing direct invoice: {ex.Message}");
            }
        }

        [RelayCommand]
        public void AddDirectInvoiceLine()
        {
            var firstProduct = AvailableProducts.FirstOrDefault();
            var firstTax = AvailableTaxes.FirstOrDefault();
            var line = new AccountingInvoiceLineItem
            {
                SelectedProduct = firstProduct,
                Quantity = 1,
                UnitPrice = firstProduct?.Price ?? 0m,
                SelectedTax = firstTax,
                OnLineChanged = RecalculateDirectInvoiceTotals
            };
            DirectInvoiceLines.Add(line);
            RecalculateDirectInvoiceTotals();
        }

        [RelayCommand]
        public void RemoveDirectInvoiceLine(AccountingInvoiceLineItem? line)
        {
            if (line != null && DirectInvoiceLines.Count > 1)
            {
                DirectInvoiceLines.Remove(line);
                RecalculateDirectInvoiceTotals();
            }
        }

        public void RecalculateDirectInvoiceTotals()
        {
            DirectInvoiceSubtotal = DirectInvoiceLines.Sum(l => l.LineTotal);
            DirectInvoiceTaxTotal = DirectInvoiceLines.Sum(l => l.TaxAmount);
            DirectInvoiceGrandTotal = DirectInvoiceSubtotal + DirectInvoiceTaxTotal;
        }

        [RelayCommand]
        public async Task SubmitDirectInvoiceAsync()
        {
            DirectInvoiceErrorMessage = string.Empty;

            if (SelectedDirectCustomer == null)
            {
                DirectInvoiceErrorMessage = "Please select a customer.";
                return;
            }

            var validLines = DirectInvoiceLines.Where(l => l.SelectedProduct != null && l.Quantity > 0).ToList();
            if (!validLines.Any())
            {
                DirectInvoiceErrorMessage = "Please add at least one line item with a product and quantity greater than zero.";
                return;
            }

            try
            {
                var so = new SalesOrder
                {
                    CustomerId = SelectedDirectCustomer.Id,
                    OrderDate = DirectInvoiceDate ?? DateTime.Today,
                    QuotationDate = DirectInvoiceDate ?? DateTime.Today,
                    DeliveryDate = DirectInvoiceDueDate ?? DateTime.Today.AddDays(30),
                    PaymentTerms = "Immediate Payment",
                    Notes = string.IsNullOrWhiteSpace(DirectInvoiceNotes) ? DirectInvoiceReference : DirectInvoiceNotes,
                    CreatedByUsername = UserSession.CurrentUser?.Username ?? "Accountant",
                    Status = "Confirmed",
                    BillingStatus = "Waiting Invoice",
                    DeliveryStatus = "Pending",
                    Company = "My Company",
                    Currency = CurrencySymbol
                };

                var items = validLines.Select(l => new SalesOrderItem
                {
                    ProductId = l.SelectedProduct!.Id,
                    QuantityOrdered = (int)Math.Max(1, Math.Round(l.Quantity)),
                    UnitPrice = l.UnitPrice,
                    TaxId = l.SelectedTax?.Id
                }).ToList();

                await _salesOrderService.CreateSalesOrderAsync(so, items);

                // Deliver and Invoice to post revenue, inventory, and AR to GL
                var deliveryLines = items.Select(i => (itemId: i.Id, quantityDelivered: i.QuantityOrdered)).ToList();
                await _salesOrderService.DeliverSalesOrderAsync(so.Id, deliveryLines);
                await _salesOrderService.InvoiceSalesOrderAsync(so.Id);

                IsDirectInvoiceModalOpen = false;
                await LoadInvoicesAsync();
                _ = LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                DirectInvoiceErrorMessage = $"Failed to create direct invoice: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CloseDirectInvoiceModal()
        {
            IsDirectInvoiceModalOpen = false;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TAB 2: VENDOR BILLS (A/P)
        // ═══════════════════════════════════════════════════════════════════════════
        private List<VendorBillRow> _allBills = new();
        [ObservableProperty] private ObservableCollection<VendorBillRow> _bills = new();
        [ObservableProperty] private VendorBillRow? _selectedBill;
        [ObservableProperty] private string _billSearchText = string.Empty;
        [ObservableProperty] private string _selectedBillFilter = "All";

        [ObservableProperty] private decimal _totalPayable;
        [ObservableProperty] private decimal _totalOverduePayable;
        [ObservableProperty] private int _openBillsCount;
        [ObservableProperty] private int _paidBillsCount;

        partial void OnBillSearchTextChanged(string value) => FilterBills();
        partial void OnSelectedBillFilterChanged(string value) => FilterBills();

        [RelayCommand]
        public async Task LoadBillsAsync()
        {
            try
            {
                var pos = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
                var billed = pos.Where(p => p.PurchaseOrder.BillingStatus != "Waiting Bill" && p.PurchaseOrder.Status != "Cancelled").ToList();
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                var supplierById = suppliers.ToDictionary(s => s.Id, s => s.Name);
                var today = DateTime.Today;

                var list = new List<VendorBillRow>();
                decimal sumPayable = 0;
                decimal sumOverdue = 0;
                int openCount = 0;
                int paidCount = 0;

                foreach (var item in billed)
                {
                    var po = item.PurchaseOrder;
                    var payments = await _paymentService.GetPaymentsForDocumentAsync("PurchaseOrder", po.Id);
                    var paid = payments.Sum(p => p.Amount);
                    var balance = Math.Max(0, po.TotalAmount - paid);

                    var dueDate = po.OrderDate.AddDays(30);
                    var daysOverdue = Math.Max(0, (today - dueDate.Date).Days);
                    var supplierName = supplierById.TryGetValue(po.SupplierId, out var name) ? name : "Unknown Supplier";

                    string status;
                    string color;
                    if (balance <= 0.01m)
                    {
                        status = "Paid";
                        color = "#10B981";
                        paidCount++;
                    }
                    else if (daysOverdue > 0)
                    {
                        status = "Overdue";
                        color = "#EF4444";
                        sumPayable += balance;
                        sumOverdue += balance;
                        openCount++;
                    }
                    else if (paid > 0)
                    {
                        status = "Partial";
                        color = "#F59E0B";
                        sumPayable += balance;
                        openCount++;
                    }
                    else
                    {
                        status = "Unpaid";
                        color = "#3B82F6";
                        sumPayable += balance;
                        openCount++;
                    }

                    list.Add(new VendorBillRow
                    {
                        Id = po.Id,
                        BillNumber = po.PONumber,
                        SupplierName = supplierName,
                        BillDate = po.OrderDate,
                        DueDate = dueDate,
                        DaysOverdue = daysOverdue,
                        TotalAmount = po.TotalAmount,
                        PaidAmount = paid,
                        Balance = balance,
                        Status = status,
                        StatusColor = color
                    });
                }

                _allBills = list.OrderByDescending(b => b.BillDate).ToList();
                TotalPayable = sumPayable;
                TotalOverduePayable = sumOverdue;
                OpenBillsCount = openCount;
                PaidBillsCount = paidCount;

                FilterBills();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading bills: {ex.Message}");
            }
        }

        private void FilterBills()
        {
            var query = BillSearchText?.Trim().ToLower() ?? string.Empty;
            var filter = SelectedBillFilter ?? "All";

            var filtered = _allBills.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(b =>
                    b.BillNumber.ToLower().Contains(query) ||
                    b.SupplierName.ToLower().Contains(query));
            }

            if (filter == "Unpaid")
                filtered = filtered.Where(b => b.Balance > 0.01m);
            else if (filter == "Overdue")
                filtered = filtered.Where(b => b.Status == "Overdue");
            else if (filter == "Paid")
                filtered = filtered.Where(b => b.Status == "Paid");

            Bills = new ObservableCollection<VendorBillRow>(filtered);
        }

        [RelayCommand]
        public void ViewPurchaseOrder(VendorBillRow? bill)
        {
            if (bill == null) return;
            _goToPurchaseOrderDetails?.Invoke(bill.Id);
        }

        // ── Bills Tab: Dropdown Creation Flows ────────────────────────────────────
        // 1. From Purchase Order Modal
        [ObservableProperty] private bool _isBillFromPoModalOpen;
        [ObservableProperty] private ObservableCollection<EligiblePurchaseOrderRow> _eligiblePurchaseOrders = new();
        private List<EligiblePurchaseOrderRow> _allEligiblePurchaseOrders = new();
        [ObservableProperty] private EligiblePurchaseOrderRow? _selectedEligiblePurchaseOrder;
        [ObservableProperty] private string _poModalSearchText = string.Empty;
        [ObservableProperty] private string _billFromPoErrorMessage = string.Empty;

        partial void OnPoModalSearchTextChanged(string value) => FilterEligiblePurchaseOrders();

        [RelayCommand]
        public async Task OpenBillFromPurchaseOrderModalAsync()
        {
            BillFromPoErrorMessage = string.Empty;
            PoModalSearchText = string.Empty;
            SelectedEligiblePurchaseOrder = null;

            try
            {
                var pos = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
                _allEligiblePurchaseOrders = pos
                    .Where(p => p.PurchaseOrder.BillingStatus == "Waiting Bill" && p.PurchaseOrder.Status != "Cancelled")
                    .Select(p => new EligiblePurchaseOrderRow
                    {
                        Id = p.PurchaseOrder.Id,
                        PONumber = p.PurchaseOrder.PONumber,
                        SupplierName = p.SupplierName,
                        OrderDate = p.PurchaseOrder.OrderDate,
                        TotalAmount = p.PurchaseOrder.TotalAmount,
                        ReceiptStatus = p.PurchaseOrder.ReceiptStatus,
                        BillingStatus = p.PurchaseOrder.BillingStatus
                    })
                    .OrderByDescending(p => p.OrderDate)
                    .ToList();

                FilterEligiblePurchaseOrders();
                IsBillFromPoModalOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading eligible purchase orders: {ex.Message}");
            }
        }

        private void FilterEligiblePurchaseOrders()
        {
            var query = PoModalSearchText?.Trim().ToLower() ?? string.Empty;
            var filtered = _allEligiblePurchaseOrders.AsEnumerable();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(p =>
                    p.PONumber.ToLower().Contains(query) ||
                    p.SupplierName.ToLower().Contains(query));
            }
            EligiblePurchaseOrders = new ObservableCollection<EligiblePurchaseOrderRow>(filtered);
            SelectedEligiblePurchaseOrder = EligiblePurchaseOrders.FirstOrDefault();
        }

        [RelayCommand]
        public async Task GenerateBillFromSelectedPoAsync()
        {
            BillFromPoErrorMessage = string.Empty;
            if (SelectedEligiblePurchaseOrder == null)
            {
                BillFromPoErrorMessage = "Please select a purchase order to bill.";
                return;
            }

            try
            {
                var poId = SelectedEligiblePurchaseOrder.Id;
                var items = await _purchaseOrderService.GetItemsAsync(poId);

                // Receive remaining items if needed so inventory and AP are recognized in GL
                var unreceived = items
                    .Select(i => (itemId: i.Id, quantityReceived: i.QuantityOrdered - i.QuantityReceived))
                    .Where(x => x.quantityReceived > 0)
                    .ToList();

                if (unreceived.Any())
                {
                    await _purchaseOrderService.ReceivePurchaseOrderAsync(poId, unreceived);
                }

                await _purchaseOrderService.CreateBillAsync(poId);

                IsBillFromPoModalOpen = false;
                await LoadBillsAsync();
                _ = LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                BillFromPoErrorMessage = $"Failed to create bill: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CloseBillFromPoModal()
        {
            IsBillFromPoModalOpen = false;
        }

        // 2. Direct Vendor Bill Modal
        [ObservableProperty] private bool _isDirectBillModalOpen;
        [ObservableProperty] private ObservableCollection<Supplier> _availableSuppliers = new();
        [ObservableProperty] private Supplier? _selectedDirectSupplier;
        [ObservableProperty] private DateTime? _directBillDate = DateTime.Today;
        [ObservableProperty] private DateTime? _directBillDueDate = DateTime.Today.AddDays(30);
        [ObservableProperty] private string _directBillReference = string.Empty;
        [ObservableProperty] private string _directBillNotes = string.Empty;
        [ObservableProperty] private ObservableCollection<AccountingBillLineItem> _directBillLines = new();
        [ObservableProperty] private decimal _directBillSubtotal;
        [ObservableProperty] private decimal _directBillTaxTotal;
        [ObservableProperty] private decimal _directBillGrandTotal;
        [ObservableProperty] private string _directBillErrorMessage = string.Empty;

        [RelayCommand]
        public async Task OpenDirectBillModalAsync()
        {
            DirectBillErrorMessage = string.Empty;
            DirectBillReference = string.Empty;
            DirectBillNotes = string.Empty;
            DirectBillDate = DateTime.Today;
            DirectBillDueDate = DateTime.Today.AddDays(30);

            try
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                var products = await _inventoryService.GetAllProductsAsync();
                var taxes = await _taxService.GetAllTaxesAsync();

                AvailableSuppliers = new ObservableCollection<Supplier>(suppliers.Where(s => s.IsActive && !s.IsDeleted));
                SelectedDirectSupplier = AvailableSuppliers.FirstOrDefault();

                AvailableProducts = new ObservableCollection<Product>(products.Where(p => !p.IsDeleted));
                AvailableTaxes = new ObservableCollection<Tax>(taxes.Where(t => t.IsActive && !t.IsDeleted));

                DirectBillLines.Clear();
                AddDirectBillLine();

                IsDirectBillModalOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error preparing direct bill: {ex.Message}");
            }
        }

        [RelayCommand]
        public void AddDirectBillLine()
        {
            var firstProduct = AvailableProducts.FirstOrDefault();
            var firstTax = AvailableTaxes.FirstOrDefault();
            var line = new AccountingBillLineItem
            {
                SelectedProduct = firstProduct,
                Quantity = 1,
                UnitCost = firstProduct?.Cost ?? 0m,
                SelectedTax = firstTax,
                OnLineChanged = RecalculateDirectBillTotals
            };
            DirectBillLines.Add(line);
            RecalculateDirectBillTotals();
        }

        [RelayCommand]
        public void RemoveDirectBillLine(AccountingBillLineItem? line)
        {
            if (line != null && DirectBillLines.Count > 1)
            {
                DirectBillLines.Remove(line);
                RecalculateDirectBillTotals();
            }
        }

        public void RecalculateDirectBillTotals()
        {
            DirectBillSubtotal = DirectBillLines.Sum(l => l.LineTotal);
            DirectBillTaxTotal = DirectBillLines.Sum(l => l.TaxAmount);
            DirectBillGrandTotal = DirectBillSubtotal + DirectBillTaxTotal;
        }

        [RelayCommand]
        public async Task SubmitDirectBillAsync()
        {
            DirectBillErrorMessage = string.Empty;

            if (SelectedDirectSupplier == null)
            {
                DirectBillErrorMessage = "Please select a supplier.";
                return;
            }

            var validLines = DirectBillLines.Where(l => l.SelectedProduct != null && l.Quantity > 0).ToList();
            if (!validLines.Any())
            {
                DirectBillErrorMessage = "Please add at least one line item with a product and quantity greater than zero.";
                return;
            }

            try
            {
                var po = new PurchaseOrder
                {
                    SupplierId = SelectedDirectSupplier.Id,
                    OrderDate = DirectBillDate ?? DateTime.Today,
                    ExpectedDeliveryDate = DirectBillDueDate ?? DateTime.Today.AddDays(30),
                    PaymentTerms = "Immediate Payment",
                    Notes = string.IsNullOrWhiteSpace(DirectBillNotes) ? DirectBillReference : DirectBillNotes,
                    CreatedByUsername = UserSession.CurrentUser?.Username ?? "Accountant",
                    Status = "Approved",
                    BillingStatus = "Waiting Bill",
                    ReceiptStatus = "Pending",
                    Company = "My Company",
                    Currency = CurrencySymbol
                };

                var items = validLines.Select(l => new PurchaseOrderItem
                {
                    ProductId = l.SelectedProduct!.Id,
                    QuantityOrdered = (int)Math.Max(1, Math.Round(l.Quantity)),
                    UnitCost = l.UnitCost,
                    TaxId = l.SelectedTax?.Id
                }).ToList();

                await _purchaseOrderService.CreatePurchaseOrderAsync(po, items);

                // Receive goods and Create Bill to post inventory, receipt and AP to GL
                var receiveLines = items.Select(i => (itemId: i.Id, quantityReceived: i.QuantityOrdered)).ToList();
                await _purchaseOrderService.ReceivePurchaseOrderAsync(po.Id, receiveLines);
                await _purchaseOrderService.CreateBillAsync(po.Id);

                IsDirectBillModalOpen = false;
                await LoadBillsAsync();
                _ = LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                DirectBillErrorMessage = $"Failed to create direct bill: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CloseDirectBillModal()
        {
            IsDirectBillModalOpen = false;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TAB 3: CHART OF ACCOUNTS & GENERAL LEDGER
        // ═══════════════════════════════════════════════════════════════════════════
        private List<AccountDisplayRow> _allAccounts = new();
        [ObservableProperty] private ObservableCollection<AccountDisplayRow> _accounts = new();
        [ObservableProperty] private AccountDisplayRow? _selectedAccount;
        [ObservableProperty] private string _accountSearchText = string.Empty;
        [ObservableProperty] private string _selectedAccountGroup = "All";

        [ObservableProperty] private decimal _totalAssetsBalance;
        [ObservableProperty] private decimal _totalLiabilitiesBalance;
        [ObservableProperty] private decimal _totalEquityBalance;
        [ObservableProperty] private decimal _totalIncomeBalance;
        [ObservableProperty] private decimal _totalExpenseBalance;

        // Ledger Drawer / Selection Details
        [ObservableProperty] private ObservableCollection<JournalEntryDetailRow> _selectedAccountLedger = new();
        [ObservableProperty] private decimal _selectedAccountTotalDebit;
        [ObservableProperty] private decimal _selectedAccountTotalCredit;
        [ObservableProperty] private bool _isLedgerDrawerOpen;

        partial void OnAccountSearchTextChanged(string value) => FilterAccounts();
        partial void OnSelectedAccountGroupChanged(string value) => FilterAccounts();

        partial void OnSelectedAccountChanged(AccountDisplayRow? value)
        {
            if (value != null)
            {
                _ = LoadLedgerForAccountAsync(value);
            }
            else
            {
                IsLedgerDrawerOpen = false;
                SelectedAccountLedger.Clear();
            }
        }

        [RelayCommand]
        public async Task LoadAccountsAsync()
        {
            try
            {
                var rawAccounts = await _accountService.GetAllAccountsAsync();
                var journalLines = await _journalService.Database.Connection.Table<JournalLine>().ToListAsync();

                var balancesByAccount = journalLines
                    .GroupBy(l => l.AccountId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(l => l.Debit - l.Credit)
                    );

                var list = new List<AccountDisplayRow>();
                decimal assets = 0;
                decimal liabilities = 0;
                decimal equity = 0;
                decimal income = 0;
                decimal expenses = 0;

                foreach (var a in rawAccounts)
                {
                    balancesByAccount.TryGetValue(a.Id, out var bal);
                    var group = !string.IsNullOrEmpty(a.Code) ? a.Code.Substring(0, 1) : "1";

                    // Account normal balance logic
                    decimal displayBalance;
                    if (group == "1")
                    {
                        displayBalance = bal; // Assets debit positive
                        assets += displayBalance;
                    }
                    else if (group == "2")
                    {
                        displayBalance = -bal; // Liabilities credit positive
                        liabilities += displayBalance;
                    }
                    else if (group == "3")
                    {
                        displayBalance = -bal; // Equity credit positive
                        equity += displayBalance;
                    }
                    else if (group == "4")
                    {
                        displayBalance = -bal; // Income credit positive
                        income += displayBalance;
                    }
                    else
                    {
                        displayBalance = bal; // Expense debit positive
                        expenses += displayBalance;
                    }

                    list.Add(new AccountDisplayRow
                    {
                        Id = a.Id,
                        Code = a.Code,
                        Name = a.Name,
                        AccountType = a.Type,
                        Currency = a.Currency ?? "RWF",
                        Balance = displayBalance,
                        Group = group
                    });
                }

                _allAccounts = list.OrderBy(a => a.Code).ToList();
                TotalAssetsBalance = assets;
                TotalLiabilitiesBalance = liabilities;
                TotalEquityBalance = equity;
                TotalIncomeBalance = income;
                TotalExpenseBalance = expenses;

                FilterAccounts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading accounts: {ex.Message}");
            }
        }

        private void FilterAccounts()
        {
            var query = AccountSearchText?.Trim().ToLower() ?? string.Empty;
            var group = SelectedAccountGroup ?? "All";

            var filtered = _allAccounts.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(a =>
                    a.Code.ToLower().Contains(query) ||
                    a.Name.ToLower().Contains(query) ||
                    a.AccountType.ToLower().Contains(query));
            }

            if (group != "All")
            {
                filtered = filtered.Where(a => a.Group == group);
            }

            Accounts = new ObservableCollection<AccountDisplayRow>(filtered);
        }

        public async Task LoadLedgerForAccountAsync(AccountDisplayRow account)
        {
            try
            {
                var lines = await _journalService.Database.Connection.Table<JournalLine>()
                    .Where(l => l.AccountId == account.Id)
                    .ToListAsync();
                var entries = await _journalService.Database.Connection.Table<JournalEntry>().ToListAsync();
                var entryById = entries.ToDictionary(e => e.Id, e => e);

                var displayRows = new List<JournalEntryDetailRow>();
                decimal running = 0;

                foreach (var line in lines.OrderBy(l => l.Id))
                {
                    entryById.TryGetValue(line.JournalEntryId, out var entry);
                    var diff = line.Debit - line.Credit;
                    running += diff;

                    displayRows.Add(new JournalEntryDetailRow
                    {
                        Date = entry?.Date ?? DateTime.Today,
                        EntryNumber = entry?.EntryNumber ?? "GL",
                        AccountDisplay = $"{account.Code} - {account.Name}",
                        PartnerName = "-",
                        Label = line.Label ?? entry?.Reference ?? "-",
                        Debit = line.Debit,
                        Credit = line.Credit,
                        Matching = running.ToString("N2", CultureInfo.InvariantCulture)
                    });
                }

                displayRows.Reverse(); // Newest first
                SelectedAccountLedger = new ObservableCollection<JournalEntryDetailRow>(displayRows);
                SelectedAccountTotalDebit = lines.Sum(l => l.Debit);
                SelectedAccountTotalCredit = lines.Sum(l => l.Credit);
                IsLedgerDrawerOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ledger: {ex.Message}");
            }
        }

        [RelayCommand]
        public void CloseLedgerDrawer()
        {
            IsLedgerDrawerOpen = false;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TAB 4: FINANCIAL REPORTS (P&L, Balance Sheet, VAT)
        // ═══════════════════════════════════════════════════════════════════════════
        [ObservableProperty] private string _selectedReportKind = "ProfitAndLoss"; // "ProfitAndLoss", "BalanceSheet", "VatReturn"
        public bool IsPnlReportSelected => SelectedReportKind == "ProfitAndLoss";
        public bool IsBsReportSelected => SelectedReportKind == "BalanceSheet";
        public bool IsVatReportSelected => SelectedReportKind == "VatReturn";

        [ObservableProperty] private ObservableCollection<AccountingReportLineRow> _reportStatementLines = new();
        [ObservableProperty] private decimal _reportNetIncome;
        [ObservableProperty] private decimal _reportTotalRevenue;
        [ObservableProperty] private decimal _reportTotalExpense;
        [ObservableProperty] private VatReturnSummary? _reportVatSummary;
        [ObservableProperty] private bool _isLoadingReport;

        partial void OnSelectedReportKindChanged(string value)
        {
            OnPropertyChanged(nameof(IsPnlReportSelected));
            OnPropertyChanged(nameof(IsBsReportSelected));
            OnPropertyChanged(nameof(IsVatReportSelected));
            _ = LoadFinancialReportAsync();
        }

        [RelayCommand]
        public async Task LoadFinancialReportAsync()
        {
            IsLoadingReport = true;
            try
            {
                if (SelectedReportKind == "VatReturn")
                {
                    var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    var end = DateTime.Today;
                    ReportVatSummary = await _vatExportService.ComputeVatReturnAsync(start, end);
                }
                else
                {
                    var reportName = SelectedReportKind == "ProfitAndLoss" ? "Profit and Loss" : "Balance Sheet";
                    var reports = await _accountingReportService.GetAllReportsAsync();
                    var target = reports.FirstOrDefault(r => r.Name.Equals(reportName, StringComparison.OrdinalIgnoreCase))
                                 ?? reports.FirstOrDefault();

                    if (target != null)
                    {
                        var lines = await _accountingReportService.ComputeReportBalancesAsync(target.Id);
                        ReportStatementLines = new ObservableCollection<AccountingReportLineRow>(lines.Select(l => new AccountingReportLineRow
                        {
                            Name = l.Name,
                            Code = l.Code,
                            Balance = l.Balance,
                            Level = l.Level
                        }));

                        if (SelectedReportKind == "ProfitAndLoss")
                        {
                            var incomeLines = lines.Where(l => l.Code.StartsWith("4") || l.Name.Contains("Revenue") || l.Name.Contains("Income")).ToList();
                            var expenseLines = lines.Where(l => l.Code.StartsWith("5") || l.Name.Contains("Expense") || l.Name.Contains("Cost")).ToList();

                            ReportTotalRevenue = incomeLines.Sum(l => l.Balance);
                            ReportTotalExpense = expenseLines.Sum(l => l.Balance);
                            ReportNetIncome = lines.LastOrDefault()?.Balance ?? (ReportTotalRevenue - ReportTotalExpense);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading financial report: {ex.Message}");
            }
            finally
            {
                IsLoadingReport = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PAYMENT RECORDING MODAL (Works for Invoices and Bills)
        // ═══════════════════════════════════════════════════════════════════════════
        [ObservableProperty] private bool _isPaymentModalOpen;
        [ObservableProperty] private string _paymentDocType = "SalesOrder"; // "SalesOrder" or "PurchaseOrder"
        [ObservableProperty] private int _paymentDocId;
        [ObservableProperty] private string _paymentDocNumber = string.Empty;
        [ObservableProperty] private string _paymentPartnerName = string.Empty;
        [ObservableProperty] private decimal _paymentMaxAmount;
        [ObservableProperty] private decimal _paymentAmount;
        [ObservableProperty] private string _paymentMethod = "Bank";
        [ObservableProperty] private string _paymentReference = string.Empty;
        [ObservableProperty] private string _paymentErrorMessage = string.Empty;
        [ObservableProperty] private ObservableCollection<BankAccount> _availableBankAccounts = new();
        [ObservableProperty] private BankAccount? _selectedBankAccount;

        public List<string> PaymentMethods { get; } = new() { "Bank", "Cash", "Mobile Money" };

        [RelayCommand]
        public async Task OpenInvoicePaymentModal(CustomerInvoiceRow? invoice)
        {
            if (invoice == null) return;
            PaymentDocType = "SalesOrder";
            PaymentDocId = invoice.Id;
            PaymentDocNumber = invoice.InvoiceNumber;
            PaymentPartnerName = invoice.CustomerName;
            PaymentMaxAmount = invoice.Balance;
            PaymentAmount = invoice.Balance;
            PaymentMethod = "Bank";
            PaymentReference = $"INV-{invoice.InvoiceNumber}";
            PaymentErrorMessage = string.Empty;

            await LoadBankAccountsAsync();
            IsPaymentModalOpen = true;
        }

        [RelayCommand]
        public async Task OpenBillPaymentModal(VendorBillRow? bill)
        {
            if (bill == null) return;
            PaymentDocType = "PurchaseOrder";
            PaymentDocId = bill.Id;
            PaymentDocNumber = bill.BillNumber;
            PaymentPartnerName = bill.SupplierName;
            PaymentMaxAmount = bill.Balance;
            PaymentAmount = bill.Balance;
            PaymentMethod = "Bank";
            PaymentReference = $"BILL-{bill.BillNumber}";
            PaymentErrorMessage = string.Empty;

            await LoadBankAccountsAsync();
            IsPaymentModalOpen = true;
        }

        private async Task LoadBankAccountsAsync()
        {
            try
            {
                var banks = await _paymentService.GetAllBankAccountsAsync();
                AvailableBankAccounts = new ObservableCollection<BankAccount>(banks);
                SelectedBankAccount = AvailableBankAccounts.FirstOrDefault();
            }
            catch { }
        }

        [RelayCommand]
        public async Task SubmitPaymentAsync()
        {
            PaymentErrorMessage = string.Empty;
            if (PaymentAmount <= 0)
            {
                PaymentErrorMessage = "Amount must be greater than zero.";
                return;
            }

            if (PaymentAmount > PaymentMaxAmount + 0.01m)
            {
                PaymentErrorMessage = $"Amount cannot exceed remaining balance ({PaymentMaxAmount:N2} {CurrencySymbol}).";
                return;
            }

            try
            {
                var username = UserSession.CurrentUser?.Username ?? "Accountant";
                await _paymentService.RecordInvoicePaymentAsync(
                    documentType: PaymentDocType,
                    documentId: PaymentDocId,
                    amount: PaymentAmount,
                    paymentMethod: PaymentMethod,
                    username: username,
                    bankAccountId: SelectedBankAccount?.Id,
                    reference: PaymentReference);

                IsPaymentModalOpen = false;

                // Refresh active tab
                if (PaymentDocType == "SalesOrder")
                {
                    await LoadInvoicesAsync();
                }
                else
                {
                    await LoadBillsAsync();
                }

                // Also update accounts in the background
                _ = LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                PaymentErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        public void ClosePaymentModal()
        {
            IsPaymentModalOpen = false;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // NEW ACCOUNT CREATION MODAL
        // ═══════════════════════════════════════════════════════════════════════════
        [ObservableProperty] private bool _isAccountModalOpen;
        [ObservableProperty] private string _newAccountCode = string.Empty;
        [ObservableProperty] private string _newAccountName = string.Empty;
        [ObservableProperty] private string _newAccountType = "Asset: Current Asset";
        [ObservableProperty] private string _newAccountCurrency = "RWF";
        [ObservableProperty] private string _newAccountDescription = string.Empty;
        [ObservableProperty] private string _newAccountErrorMessage = string.Empty;

        public ObservableCollection<string> AccountTypeOptions { get; } = new()
        {
            "Asset: Receivable",
            "Asset: Bank and Cash",
            "Asset: Current Asset",
            "Asset: Fixed Asset",
            "Asset: Non-current Asset",
            "Asset: Pre Payments",
            "Liability: Payable",
            "Liability: Credit Card",
            "Liability: Current Liability",
            "Liability: Non-current Liability",
            "Equity: Equity",
            "Equity: Current Year Earnings",
            "Income: Income",
            "Income: Other Incomes",
            "Expense: Expenses",
            "Expense: Other Expenses",
            "Expense: Cost of Revenue"
        };

        [RelayCommand]
        public void OpenNewAccountModal()
        {
            NewAccountCode = string.Empty;
            NewAccountName = string.Empty;
            NewAccountType = "Asset: Current Asset";
            NewAccountCurrency = CurrencySymbol;
            NewAccountDescription = string.Empty;
            NewAccountErrorMessage = string.Empty;
            IsAccountModalOpen = true;
        }

        [RelayCommand]
        public async Task SubmitNewAccountAsync()
        {
            NewAccountErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(NewAccountCode))
            {
                NewAccountErrorMessage = "Account Code is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(NewAccountName))
            {
                NewAccountErrorMessage = "Account Name is required.";
                return;
            }

            try
            {
                var account = new Account
                {
                    Code = NewAccountCode.Trim(),
                    Name = NewAccountName.Trim(),
                    Type = NewAccountType,
                    Currency = NewAccountCurrency,
                    Description = NewAccountDescription,
                    IsActive = true
                };

                await _accountService.AddAccountAsync(account);
                IsAccountModalOpen = false;
                await LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                NewAccountErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        public void CloseAccountModal()
        {
            IsAccountModalOpen = false;
        }

        // ── Navigation Commands ──────────────────────────────────────────────────
        [RelayCommand]
        public void GoToCreateSalesOrder() => _goToSalesOrderDetails?.Invoke(null);

        [RelayCommand]
        public void GoToCreatePurchaseOrder() => _goToPurchaseOrderDetails?.Invoke(null);

        public AccountingViewModel(
            SalesOrderService salesOrderService,
            PurchaseOrderService purchaseOrderService,
            AgingReportService agingReportService,
            PaymentService paymentService,
            AccountService accountService,
            AccountingReportService accountingReportService,
            JournalService journalService,
            TaxService taxService,
            VatExportService vatExportService,
            MonthCloseService monthCloseService,
            CustomerService customerService,
            SupplierService supplierService,
            InventoryService inventoryService,
            SettingsService settingsService,
            LanguageService languageService,
            LicenseService licenseService,
            Action<int?>? goToSalesOrderDetails = null,
            Action<int?>? goToPurchaseOrderDetails = null)
        {
            _salesOrderService = salesOrderService;
            _purchaseOrderService = purchaseOrderService;
            _agingReportService = agingReportService;
            _paymentService = paymentService;
            _accountService = accountService;
            _accountingReportService = accountingReportService;
            _journalService = journalService;
            _taxService = taxService;
            _vatExportService = vatExportService;
            _monthCloseService = monthCloseService;
            _customerService = customerService;
            _supplierService = supplierService;
            _inventoryService = inventoryService;
            _settingsService = settingsService;
            _languageService = languageService;
            _licenseService = licenseService;
            _pdfService = new SalesOrderPdfService(settingsService);
            _goToSalesOrderDetails = goToSalesOrderDetails;
            _goToPurchaseOrderDetails = goToPurchaseOrderDetails;

            _ = LoadInvoicesAsync();
        }
    }
}
