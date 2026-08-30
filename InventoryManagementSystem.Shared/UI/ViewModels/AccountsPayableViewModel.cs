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
    /// <summary>
    /// Accounts Payable workspace: every supplier we owe money to, drill into their open
    /// vendor bills (and the linked purchase order), see the tax breakdown, and record a
    /// payment without leaving the screen.
    /// </summary>
    public partial class AccountsPayableViewModel : ViewModelBase
    {
        private readonly AgingReportService _agingReportService;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly InventoryService _inventoryService;
        private readonly TaxService _taxService;
        private readonly PaymentService _paymentService;
        private readonly Action<int?>? _goToPurchaseOrder;

        [ObservableProperty] private ObservableCollection<ApSupplierRow> _suppliers = new();
        [ObservableProperty] private ApSupplierRow? _selectedSupplier;
        [ObservableProperty] private ObservableCollection<ApBillRow> _bills = new();
        [ObservableProperty] private ApBillRow? _selectedBill;
        [ObservableProperty] private ObservableCollection<ApBillLineRow> _billLines = new();
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;

        [ObservableProperty] private decimal _totalPayable;
        [ObservableProperty] private decimal _totalOverdue;
        [ObservableProperty] private int _openBillCount;

        // Selected bill summary
        [ObservableProperty] private decimal _billSubtotal;
        [ObservableProperty] private decimal _billTaxTotal;
        [ObservableProperty] private decimal _billRecordedTotal;
        [ObservableProperty] private decimal _billPaid;
        [ObservableProperty] private decimal _billOpenBalance;
        [ObservableProperty] private string _billCurrency = string.Empty;
        [ObservableProperty] private string _billTaxNote = string.Empty;

        // Payment modal
        [ObservableProperty] private bool _isPaymentModalOpen;
        [ObservableProperty] private decimal _paymentAmount;
        [ObservableProperty] private string _paymentMethod = "Bank";
        [ObservableProperty] private string _paymentReference = string.Empty;
        [ObservableProperty] private string _paymentError = string.Empty;

        public List<string> PaymentMethods { get; } = new() { "Bank", "Cash", "Mobile Money" };

        public bool CanPaySelectedBill => SelectedBill != null && BillOpenBalance > 0.01m;

        public AccountsPayableViewModel(
            AgingReportService agingReportService,
            PurchaseOrderService purchaseOrderService,
            InventoryService inventoryService,
            TaxService taxService,
            PaymentService paymentService,
            Action<int?>? goToPurchaseOrder = null)
        {
            _agingReportService = agingReportService;
            _purchaseOrderService = purchaseOrderService;
            _inventoryService = inventoryService;
            _taxService = taxService;
            _paymentService = paymentService;
            _goToPurchaseOrder = goToPurchaseOrder;
            _ = LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            var lines = await _agingReportService.GetAccountsPayableAgingAsync();
            var pos = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            var poById = pos.ToDictionary(p => p.PurchaseOrder.Id, p => p.PurchaseOrder);

            var groups = lines
                .GroupBy(l => poById.TryGetValue(l.DocumentId, out var po) ? po.SupplierId : 0)
                .Select(g => new ApSupplierRow
                {
                    SupplierId = g.Key,
                    SupplierName = g.Select(l => l.PartnerName).FirstOrDefault() ?? "Unknown Supplier",
                    BillCount = g.Count(),
                    OpenBalance = g.Sum(l => l.OpenBalance),
                    OverdueAmount = g.Where(l => l.DaysOverdue > 0).Sum(l => l.OpenBalance),
                    OldestDueDate = g.Min(l => l.DueDate),
                    Lines = g
                        .OrderByDescending(l => l.DaysOverdue)
                        .Select(l => new ApBillRow
                        {
                            PoId = l.DocumentId,
                            PoNumber = l.DocumentNumber,
                            OrderDate = l.DocumentDate,
                            DueDate = l.DueDate,
                            DaysOverdue = l.DaysOverdue,
                            Bucket = l.AgingBucket,
                            RecordedTotal = l.TotalAmount,
                            OpenBalance = l.OpenBalance,
                            Currency = poById.TryGetValue(l.DocumentId, out var po2) ? po2.Currency : string.Empty,
                        })
                        .ToList(),
                })
                .OrderByDescending(s => s.OverdueAmount)
                .ThenByDescending(s => s.OpenBalance)
                .ToList();

            TotalPayable = groups.Sum(s => s.OpenBalance);
            TotalOverdue = groups.Sum(s => s.OverdueAmount);
            OpenBillCount = groups.Sum(s => s.BillCount);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var q = SearchText.Trim().ToLowerInvariant();
                groups = groups.Where(s => s.SupplierName.ToLowerInvariant().Contains(q)).ToList();
            }

            var keepSupplierId = SelectedSupplier?.SupplierId;
            Suppliers = new ObservableCollection<ApSupplierRow>(groups);
            SelectedSupplier = groups.FirstOrDefault(s => s.SupplierId == keepSupplierId) ?? groups.FirstOrDefault();
        }

        partial void OnSelectedSupplierChanged(ApSupplierRow? value)
        {
            var keepBillPoId = SelectedBill?.PoId;
            Bills = new ObservableCollection<ApBillRow>(value?.Lines ?? new List<ApBillRow>());
            SelectedBill = Bills.FirstOrDefault(b => b.PoId == keepBillPoId) ?? Bills.FirstOrDefault();
        }

        partial void OnSelectedBillChanged(ApBillRow? value)
        {
            _ = LoadBillDetailAsync(value);
            OnPropertyChanged(nameof(CanPaySelectedBill));
        }

        private async Task LoadBillDetailAsync(ApBillRow? bill)
        {
            BillLines = new ObservableCollection<ApBillLineRow>();
            BillSubtotal = BillTaxTotal = BillRecordedTotal = BillPaid = BillOpenBalance = 0;
            BillCurrency = bill?.Currency ?? string.Empty;
            BillTaxNote = string.Empty;
            OnPropertyChanged(nameof(CanPaySelectedBill));

            if (bill == null) return;

            var items = await _purchaseOrderService.GetItemsAsync(bill.PoId);
            var products = await _inventoryService.GetAllProductsAsync();
            var taxes = await _taxService.GetAllTaxesAsync();

            decimal subtotal = 0, taxTotal = 0;
            var rows = new List<ApBillLineRow>();
            foreach (var it in items)
            {
                var product = products.FirstOrDefault(p => p.Id == it.ProductId);
                var lineNet = it.QuantityOrdered * it.UnitCost;
                var tax = it.TaxId.HasValue ? taxes.FirstOrDefault(t => t.Id == it.TaxId.Value) : null;

                decimal lineSub = lineNet;
                decimal lineTax = 0;
                if (tax != null)
                {
                    if (tax.IncludedInPrice == "Include")
                    {
                        lineSub = tax.Computation == "Percentage"
                            ? lineNet / (1 + (tax.Amount / 100))
                            : Math.Max(0, lineNet - (it.QuantityOrdered * tax.Amount));
                        lineTax = lineNet - lineSub;
                    }
                    else
                    {
                        lineTax = tax.Computation == "Percentage"
                            ? lineNet * (tax.Amount / 100)
                            : it.QuantityOrdered * tax.Amount;
                    }
                }

                subtotal += lineSub;
                taxTotal += lineTax;

                rows.Add(new ApBillLineRow
                {
                    ProductName = product?.Name ?? $"Product #{it.ProductId}",
                    Quantity = it.QuantityOrdered,
                    UnitCost = it.UnitCost,
                    TaxLabel = tax == null
                        ? "—"
                        : $"{tax.Name} ({(tax.Computation == "Percentage" ? tax.Amount.ToString("0.##") + "%" : tax.Amount.ToString("N2"))}{(tax.IncludedInPrice == "Include" ? " incl" : "")})",
                    TaxAmount = lineTax,
                    LineTotal = lineSub + lineTax,
                });
            }

            BillLines = new ObservableCollection<ApBillLineRow>(rows);
            BillSubtotal = subtotal;
            BillTaxTotal = taxTotal;
            BillRecordedTotal = bill.RecordedTotal;
            BillPaid = await _paymentService.GetAmountPaidAsync("PurchaseOrder", bill.PoId);
            BillOpenBalance = await _paymentService.GetOpenBalanceAsync("PurchaseOrder", bill.PoId);

            BillTaxNote = taxTotal > 0.005m
                ? $"Line tax shown for reference ({taxTotal:N2}). The recorded bill total and the amount payable follow the vendor bill amount."
                : string.Empty;

            OnPropertyChanged(nameof(CanPaySelectedBill));
        }

        partial void OnBillOpenBalanceChanged(decimal value) => OnPropertyChanged(nameof(CanPaySelectedBill));

        partial void OnSearchTextChanged(string value) => _ = LoadAsync();

        [RelayCommand]
        private void ViewPurchaseOrder(ApBillRow? bill)
        {
            var target = bill ?? SelectedBill;
            if (target != null)
            {
                _goToPurchaseOrder?.Invoke(target.PoId);
            }
        }

        [RelayCommand]
        private void OpenPayBill()
        {
            if (!CanPaySelectedBill) return;
            PaymentAmount = BillOpenBalance;
            PaymentMethod = "Bank";
            PaymentReference = string.Empty;
            PaymentError = string.Empty;
            IsPaymentModalOpen = true;
        }

        [RelayCommand]
        private void CancelPayment() => IsPaymentModalOpen = false;

        [RelayCommand]
        private async Task SubmitPayment()
        {
            PaymentError = string.Empty;
            if (SelectedBill == null) return;
            if (PaymentAmount <= 0)
            {
                PaymentError = "Payment amount must be greater than zero.";
                return;
            }

            try
            {
                await _paymentService.RecordInvoicePaymentAsync(
                    "PurchaseOrder",
                    SelectedBill.PoId,
                    PaymentAmount,
                    PaymentMethod,
                    UserSession.CurrentUser?.Username ?? "System",
                    reference: PaymentReference);

                IsPaymentModalOpen = false;
                StatusMessage = $"Recorded {PaymentAmount:N2} {SelectedBill.Currency} against {SelectedBill.PoNumber}.";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                PaymentError = ex.Message;
            }
        }
    }

    public class ApSupplierRow
    {
        public int SupplierId { get; init; }
        public string SupplierName { get; init; } = string.Empty;
        public int BillCount { get; init; }
        public decimal OpenBalance { get; init; }
        public decimal OverdueAmount { get; init; }
        public DateTime OldestDueDate { get; init; }
        public bool HasOverdue => OverdueAmount > 0.01m;
        public List<ApBillRow> Lines { get; init; } = new();
    }

    public class ApBillRow
    {
        public int PoId { get; init; }
        public string PoNumber { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public DateTime DueDate { get; init; }
        public int DaysOverdue { get; init; }
        public string Bucket { get; init; } = "Current";
        public decimal RecordedTotal { get; init; }
        public decimal OpenBalance { get; init; }
        public string Currency { get; init; } = string.Empty;
        public bool IsOverdue => DaysOverdue > 0;
    }

    public class ApBillLineRow
    {
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitCost { get; init; }
        public string TaxLabel { get; init; } = "—";
        public decimal TaxAmount { get; init; }
        public decimal LineTotal { get; init; }
    }
}
