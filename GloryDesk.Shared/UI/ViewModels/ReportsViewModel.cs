using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public class ReportNavItem
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    public partial class ReportsViewModel : ViewModelBase
    {
        private static readonly ReportNavItem[] ReportCatalog =
        {
            new() { Key = "balance-sheet", Title = "What You Own vs Owe", Category = "Money Overview", Description = "Snapshot of assets, debts, and owner value" },
            new() { Key = "profit-loss", Title = "Income vs Expenses", Category = "Money Overview", Description = "Money in and money out for the period" },
            new() { Key = "budget-vs-actual", Title = "Budget vs Reality", Category = "Money Overview", Description = "Compare planned spending to what actually happened" },
            new() { Key = "general-ledger", Title = "Account Ledger", Category = "Money Overview", Description = "Every transaction posted to one account, with running balance" },
            new() { Key = "stock-status", Title = "Stock", Category = "Stock", Description = "On hand, free to use, incoming and outgoing per product" },
            new() { Key = "stock-history", Title = "Moves History", Category = "Stock", Description = "Every recorded stock move, newest first" },
            new() { Key = "ar-aging", Title = "Unpaid Customer Bills", Category = "Money Owed", Description = "Which customers still owe you, and for how long" },
            new() { Key = "ap-aging", Title = "Unpaid Supplier Bills", Category = "Money Owed", Description = "Which supplier bills you still need to pay" },
            new() { Key = "vat-return", Title = "Sales Tax Summary", Category = "Tax & Bank", Description = "Sales tax collected vs tax paid on purchases" },
            new() { Key = "bank-reconciliation", Title = "Match Bank to Payments", Category = "Tax & Bank", Description = "Connect your bank lines to recorded payments" },
            new() { Key = "abc-analysis", Title = "Best-Selling Products", Category = "Insights", Description = "Products ranked by revenue importance (A/B/C)" },
            new() { Key = "dead-stock", Title = "Slow-Moving Stock", Category = "Insights", Description = "Products in stock that are not selling" },
            new() { Key = "margin-by-category", Title = "Profit by Category", Category = "Insights", Description = "How much profit each product group makes" },
            new() { Key = "month-close", Title = "Month End Summary", Category = "Insights", Description = "Totals check and unpaid bills for month end" },
        };

        private readonly InventoryService _inventoryService;
        private readonly LicenseService _licenseService;
        private readonly SettingsService _settingsService;
        private readonly AccountingReportService _accountingReportService;
        private readonly AccountService _accountService;
        private readonly AgingReportService _agingReportService;
        private readonly VatExportService _vatExportService;
        private readonly BudgetReportService _budgetReportService;
        private readonly PaymentService _paymentService;
        private readonly AdvancedAnalyticsService _advancedAnalyticsService;
        private readonly MonthCloseService _monthCloseService;
        private readonly LocationService _locationService;
        private readonly ReportExportService _reportExportService = new();
        private readonly Action<int?>? _goToPurchaseOrderDetails;
        private readonly Action<int?>? _goToSalesOrderDetails;

        private static readonly HashSet<string> ExportableReportKeys = new()
        {
            "balance-sheet", "profit-loss", "ar-aging", "ap-aging",
            "budget-vs-actual", "general-ledger", "vat-return", "stock-status",
        };

        /// <summary>Whether the currently selected report supports "Export PDF / XLSX".</summary>
        public bool IsCurrentReportExportable =>
            SelectedReportNavItem != null && ExportableReportKeys.Contains(SelectedReportNavItem.Key);

        [ObservableProperty] private string _selectedCategory = "Money Overview";
        [ObservableProperty] private ReportNavItem? _selectedReportNavItem;
        [ObservableProperty] private ObservableCollection<ReportNavItem> _reportsInCategory = new();
        [ObservableProperty] private ObservableCollection<ReportLineWrapper> _balanceSheetLines = new();
        [ObservableProperty] private ObservableCollection<ReportLineWrapper> _profitAndLossLines = new();
        [ObservableProperty] private ObservableCollection<AgingLine> _arAgingLines = new();
        [ObservableProperty] private ObservableCollection<AgingLine> _apAgingLines = new();
        [ObservableProperty] private AgingSummary _arAgingSummary = new();
        [ObservableProperty] private AgingSummary _apAgingSummary = new();
        [ObservableProperty] private bool _isLoadingReport;

        // --- General Ledger (Account Ledger) ---
        private List<Account> _allLedgerAccounts = new();
        [ObservableProperty] private string _ledgerAccountSearchText = string.Empty;
        [ObservableProperty] private Account? _selectedLedgerAccount;
        [ObservableProperty] private ObservableCollection<Account> _matchedLedgerAccounts = new();
        [ObservableProperty] private DateTimeOffset? _ledgerFromDate;
        [ObservableProperty] private DateTimeOffset? _ledgerToDate;
        [ObservableProperty] private ObservableCollection<AccountLedgerLine> _ledgerLines = new();
        [ObservableProperty] private decimal _ledgerOpeningBalance;
        [ObservableProperty] private decimal _ledgerClosingBalance;

        [ObservableProperty] private ObservableCollection<Product> _reportData = new();
        [ObservableProperty] private ObservableCollection<StockMovement> _stockHistoryData = new();
        [ObservableProperty] private ObservableCollection<MonthlyProfitReport> _monthlyProfitData = new();
        [ObservableProperty] private string _reportTitle = "What You Own vs Owe";
        [ObservableProperty] private bool _isLowStockReport;
        [ObservableProperty] private bool _isHistoryReport;
        [ObservableProperty] private bool _isProfitReport;

        public bool IsStockReport => !IsHistoryReport && !IsProfitReport;

        public bool IsBalanceSheetSelected => SelectedReportNavItem?.Key == "balance-sheet";
        public bool IsProfitAndLossSelected => SelectedReportNavItem?.Key == "profit-loss";
        public bool IsStockStatusSelected => SelectedReportNavItem?.Key == "stock-status";
        public bool IsStockHistorySelected => SelectedReportNavItem?.Key == "stock-history";
        public bool IsArAgingSelected => SelectedReportNavItem?.Key == "ar-aging";
        public bool IsApAgingSelected => SelectedReportNavItem?.Key == "ap-aging";
        public bool IsVatReturnSelected => SelectedReportNavItem?.Key == "vat-return";
        public bool IsBudgetVsActualSelected => SelectedReportNavItem?.Key == "budget-vs-actual";
        public bool IsBankReconciliationSelected => SelectedReportNavItem?.Key == "bank-reconciliation";
        public bool IsGeneralLedgerSelected => SelectedReportNavItem?.Key == "general-ledger";

        public List<string> ReportCategories { get; } = ReportCatalog
            .Select(r => r.Category)
            .Distinct()
            .ToList();

        [ObservableProperty] private DateTime _vatPeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        [ObservableProperty] private DateTime _vatPeriodEnd = DateTime.Today;
        [ObservableProperty] private VatReturnSummary? _vatSummary;

        [ObservableProperty] private int _budgetFiscalYear = DateTime.Today.Year;
        [ObservableProperty] private int _budgetPeriodMonth;
        [ObservableProperty] private ObservableCollection<BudgetVsActualLine> _budgetVsActualLines = new();
        [ObservableProperty] private decimal _budgetTotalBudget;
        [ObservableProperty] private decimal _budgetTotalActual;
        [ObservableProperty] private decimal _budgetTotalVariance;

        [ObservableProperty] private ObservableCollection<BankAccountDisplayRow> _reconBankAccounts = new();
        [ObservableProperty] private BankAccountDisplayRow? _selectedReconBankAccount;
        [ObservableProperty] private ObservableCollection<ReconciliationCandidate> _unreconciledPayments = new();
        [ObservableProperty] private ObservableCollection<ReconciliationCandidate> _unreconciledStatementLines = new();
        [ObservableProperty] private ReconciliationCandidate? _selectedReconPayment;
        [ObservableProperty] private ReconciliationCandidate? _selectedReconLine;
        [ObservableProperty] private string _reconStatusMessage = string.Empty;

        [ObservableProperty] private ObservableCollection<AbcAnalysisLine> _abcAnalysisLines = new();
        [ObservableProperty] private ObservableCollection<ProductRecommendation> _deadStockLines = new();
        [ObservableProperty] private ObservableCollection<CategoryMarginLine> _categoryMarginLines = new();
        [ObservableProperty] private MonthCloseSummary? _monthCloseSummary;

        [ObservableProperty] private bool _isImportStatementModalOpen;
        [ObservableProperty] private DateTime _importStatementDate = DateTime.Today;
        [ObservableProperty] private decimal _importOpeningBalance;
        [ObservableProperty] private decimal _importClosingBalance;
        [ObservableProperty] private DateTime _importLineDate = DateTime.Today;
        [ObservableProperty] private string _importLineDescription = string.Empty;
        [ObservableProperty] private decimal _importLineAmount;
        [ObservableProperty] private string _importLineReference = string.Empty;
        [ObservableProperty] private string _importErrorMessage = string.Empty;
        [ObservableProperty] private string _importCsvContent = string.Empty;
        [ObservableProperty] private ObservableCollection<BankMatchSuggestion> _bankSuggestions = new();

        partial void OnSelectedCategoryChanged(string value)
        {
            RefreshReportsInCategory();
        }

        partial void OnSelectedReportNavItemChanged(ReportNavItem? value)
        {
            NotifyReportSelectionProperties();
            if (value != null)
            {
                ReportTitle = value.Title;
                _ = LoadSelectedReportAsync();
            }
        }

        private void RefreshReportsInCategory()
        {
            var items = ReportCatalog
                .Where(r => r.Category == SelectedCategory)
                .ToList();

            ReportsInCategory = new ObservableCollection<ReportNavItem>(items);
            SelectedReportNavItem = items.FirstOrDefault();
        }

        private void NotifyReportSelectionProperties()
        {
            OnPropertyChanged(nameof(IsBalanceSheetSelected));
            OnPropertyChanged(nameof(IsProfitAndLossSelected));
            OnPropertyChanged(nameof(IsStockStatusSelected));
            OnPropertyChanged(nameof(IsStockHistorySelected));
            OnPropertyChanged(nameof(IsArAgingSelected));
            OnPropertyChanged(nameof(IsApAgingSelected));
            OnPropertyChanged(nameof(IsVatReturnSelected));
            OnPropertyChanged(nameof(IsBudgetVsActualSelected));
            OnPropertyChanged(nameof(IsBankReconciliationSelected));
            OnPropertyChanged(nameof(IsAbcAnalysisSelected));
            OnPropertyChanged(nameof(IsDeadStockSelected));
            OnPropertyChanged(nameof(IsMarginByCategorySelected));
            OnPropertyChanged(nameof(IsMonthCloseSelected));
            OnPropertyChanged(nameof(IsGeneralLedgerSelected));
            OnPropertyChanged(nameof(IsCurrentReportExportable));
            OnPropertyChanged(nameof(IsStockAreaSelected));
        }

        public bool IsAbcAnalysisSelected => SelectedReportNavItem?.Key == "abc-analysis";
        public bool IsDeadStockSelected => SelectedReportNavItem?.Key == "dead-stock";
        public bool IsMarginByCategorySelected => SelectedReportNavItem?.Key == "margin-by-category";
        public bool IsMonthCloseSelected => SelectedReportNavItem?.Key == "month-close";

        partial void OnSelectedReconBankAccountChanged(BankAccountDisplayRow? value)
        {
            if (IsBankReconciliationSelected)
            {
                _ = LoadBankReconciliationAsync();
            }
        }

        public string CurrencySymbol => _settingsService.CurrentSettings.CurrencySymbol;
        public LanguageService Language { get; }

        public ReportsViewModel(
            InventoryService inventoryService, 
            LicenseService licenseService, 
            SettingsService settingsService, 
            LanguageService languageService,
            AccountingReportService accountingReportService,
            AccountService accountService,
            AgingReportService agingReportService,
            VatExportService vatExportService,
            BudgetReportService budgetReportService,
            PaymentService paymentService,
            AdvancedAnalyticsService advancedAnalyticsService,
            MonthCloseService monthCloseService,
            LocationService locationService,
            Action<int?>? goToPurchaseOrderDetails = null,
            Action<int?>? goToSalesOrderDetails = null,
            string? initialReportKey = null)
        {
            _inventoryService = inventoryService;
            _licenseService = licenseService;
            _settingsService = settingsService;
            Language = languageService;
            _accountingReportService = accountingReportService;
            _accountService = accountService;
            _agingReportService = agingReportService;
            _vatExportService = vatExportService;
            _budgetReportService = budgetReportService;
            _paymentService = paymentService;
            _advancedAnalyticsService = advancedAnalyticsService;
            _monthCloseService = monthCloseService;
            _locationService = locationService;
            _goToPurchaseOrderDetails = goToPurchaseOrderDetails;
            _goToSalesOrderDetails = goToSalesOrderDetails;

            var initialItem = !string.IsNullOrEmpty(initialReportKey)
                ? ReportCatalog.FirstOrDefault(r => r.Key == initialReportKey)
                : null;

            if (initialItem != null)
            {
                SelectedCategory = initialItem.Category;
                SelectedReportNavItem = initialItem;
            }
            else
            {
                RefreshReportsInCategory();
            }
        }

        /// <summary>Full report catalog, exposed for building deep links (e.g. a global nav search).</summary>
        public static IReadOnlyList<ReportNavItem> AllReports => ReportCatalog;

        [RelayCommand]
        private void OpenApBill(AgingLine? line)
        {
            if (line != null)
            {
                _goToPurchaseOrderDetails?.Invoke(line.DocumentId);
            }
        }

        [RelayCommand]
        private void OpenArBill(AgingLine? line)
        {
            if (line != null)
            {
                _goToSalesOrderDetails?.Invoke(line.DocumentId);
            }
        }

        public async Task LoadSelectedReportAsync()
        {
            IsLoadingReport = true;
            try
            {
                switch (SelectedReportNavItem?.Key)
                {
                    case "balance-sheet":
                        await LoadBalanceSheetAsync();
                        break;
                    case "profit-loss":
                        await LoadProfitAndLossAsync();
                        break;
                    case "stock-status":
                        if (IsLowStockReport)
                            await LoadLowStockReport();
                        else
                            await LoadStockReport();
                        break;
                    case "stock-history":
                        await LoadStockHistoryReport();
                        break;
                    case "ar-aging":
                        await LoadArAgingAsync();
                        break;
                    case "ap-aging":
                        await LoadApAgingAsync();
                        break;
                    case "vat-return":
                        await LoadVatReturnAsync();
                        break;
                    case "budget-vs-actual":
                        await LoadBudgetVsActualAsync();
                        break;
                    case "bank-reconciliation":
                        await LoadBankReconciliationAsync();
                        break;
                    case "abc-analysis":
                        await LoadAbcAnalysisAsync();
                        break;
                    case "dead-stock":
                        await LoadDeadStockAsync();
                        break;
                    case "margin-by-category":
                        await LoadMarginByCategoryAsync();
                        break;
                    case "month-close":
                        await LoadMonthCloseAsync();
                        break;
                    case "general-ledger":
                        await LoadGeneralLedgerAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportTitle = $"Error loading report: {ex.Message}";
            }
            finally
            {
                IsLoadingReport = false;
            }
        }

        [RelayCommand]
        public async Task LoadBalanceSheetAsync()
        {
            ReportTitle = "Balance Sheet";
            var reports = await _accountingReportService.GetAllReportsAsync();
            var bsReport = reports.FirstOrDefault(r => r.Name == "Balance Sheet");
            if (bsReport == null)
            {
                ReportTitle = "Balance Sheet Config Not Found";
                return;
            }

            var results = await _accountingReportService.ComputeReportBalancesAsync(bsReport.Id);
            BalanceSheetLines.Clear();
            foreach (var r in results)
            {
                BalanceSheetLines.Add(new ReportLineWrapper(r, CurrencySymbol));
            }
        }

        [RelayCommand]
        public async Task LoadProfitAndLossAsync()
        {
            ReportTitle = "Profit and Loss Statement";
            var reports = await _accountingReportService.GetAllReportsAsync();
            var pnlReport = reports.FirstOrDefault(r => r.Name == "Profit and Loss");
            if (pnlReport == null)
            {
                ReportTitle = "Profit & Loss Config Not Found";
                return;
            }

            var results = await _accountingReportService.ComputeReportBalancesAsync(pnlReport.Id);
            ProfitAndLossLines.Clear();
            foreach (var r in results)
            {
                ProfitAndLossLines.Add(new ReportLineWrapper(r, CurrencySymbol));
            }
        }

        [RelayCommand]
        private async Task LoadStockReport()
        {
            ReportTitle = "Stock";
            IsLowStockReport = false;
            IsHistoryReport = false;
            IsProfitReport = false;
            OnPropertyChanged(nameof(IsStockReport));
            var rows = await _inventoryService.GetStockReportRowsAsync();
            StockRows = new ObservableCollection<StockReportRow>(rows);
        }

        [RelayCommand]
        private async Task LoadLowStockReport()
        {
            ReportTitle = "Stock — low on hand (< 5)";
            IsLowStockReport = true;
            IsHistoryReport = false;
            IsProfitReport = false;
            OnPropertyChanged(nameof(IsStockReport));
            var rows = await _inventoryService.GetStockReportRowsAsync();
            StockRows = new ObservableCollection<StockReportRow>(rows.Where(r => r.OnHand < 5));
        }

        [RelayCommand]
        private async Task LoadStockHistoryReport()
        {
            if (!_licenseService.CanAccessAdvancedReports())
            {
                ReportTitle = "History is a Premium Feature. Please Upgrade.";
                return;
            }

            ReportTitle = string.IsNullOrWhiteSpace(MovesFilterProductName)
                ? "Moves History"
                : $"Moves History — {MovesFilterProductName}";
            IsLowStockReport = false;
            IsHistoryReport = true;
            IsProfitReport = false;
            OnPropertyChanged(nameof(IsStockReport));
            _allMoveRows = await _inventoryService.GetStockMovesAsync(MovesFilterProductId);
            MovesPage = 1;
            ApplyMovesView();
        }

        // --- Stock report area: Odoo-style Stock grid + Moves History ---

        [ObservableProperty] private ObservableCollection<StockReportRow> _stockRows = new();
        [ObservableProperty] private int? _movesFilterProductId;
        [ObservableProperty] private string _movesFilterProductName = string.Empty;

        public bool IsStockAreaSelected => IsStockStatusSelected || IsStockHistorySelected;

        // Moves History: List (paginated) or Grouped view over the full filtered set.
        private List<StockMoveHistoryRow> _allMoveRows = new();

        [ObservableProperty] private string _movesViewMode = "List";   // "List" | "Grouped"
        [ObservableProperty] private string _movesGroupBy = "Product"; // Product | Reference | Direction | Status
        public bool IsMovesListView => MovesViewMode == "List";
        public bool IsMovesGroupedView => MovesViewMode == "Grouped";
        public List<string> MovesGroupByOptions { get; } = new() { "Product", "Reference", "Direction", "Status" };

        public int MovesPageSize { get; } = 80;
        [ObservableProperty] private int _movesPage = 1;
        [ObservableProperty] private int _movesTotalCount;
        [ObservableProperty] private string _movesPageLabel = string.Empty;
        public bool MovesCanPrev => MovesPage > 1;
        public bool MovesCanNext => MovesPage * MovesPageSize < MovesTotalCount;

        [ObservableProperty] private ObservableCollection<StockMoveHistoryRow> _movesPagedRows = new();
        [ObservableProperty] private ObservableCollection<MoveGroup> _movesGroups = new();

        private void ApplyMovesView()
        {
            MovesTotalCount = _allMoveRows.Count;

            var maxPage = Math.Max(1, (int)Math.Ceiling(_allMoveRows.Count / (double)MovesPageSize));
            if (MovesPage > maxPage) MovesPage = maxPage;
            if (MovesPage < 1) MovesPage = 1;

            var pageRows = _allMoveRows.Skip((MovesPage - 1) * MovesPageSize).Take(MovesPageSize).ToList();
            MovesPagedRows = new ObservableCollection<StockMoveHistoryRow>(pageRows);

            var first = _allMoveRows.Count == 0 ? 0 : (MovesPage - 1) * MovesPageSize + 1;
            var last = Math.Min(MovesPage * MovesPageSize, _allMoveRows.Count);
            MovesPageLabel = $"{first}-{last} / {_allMoveRows.Count}";
            OnPropertyChanged(nameof(MovesCanPrev));
            OnPropertyChanged(nameof(MovesCanNext));

            Func<StockMoveHistoryRow, string> keySelector = MovesGroupBy switch
            {
                "Reference" => r => ReferenceGroupKey(r.Reference),
                "Direction" => r => r.IsOutbound ? "Outgoing" : "Incoming",
                "Status" => r => r.Status,
                _ => r => r.DisplayProduct,
            };
            var groups = _allMoveRows
                .GroupBy(keySelector)
                .OrderByDescending(g => g.Count())
                .Select(g => new MoveGroup(g.Key, g.ToList()))
                .ToList();
            MovesGroups = new ObservableCollection<MoveGroup>(groups);
        }

        private static string ReferenceGroupKey(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return "(none)";
            var colon = reference.IndexOf(':');
            if (colon > 0) return reference[..colon].Trim();
            var dash = reference.IndexOf('-');
            return dash > 0 ? reference[..dash].Trim() : reference.Trim();
        }

        [RelayCommand]
        private void SetMovesViewMode(string mode) => MovesViewMode = mode == "Grouped" ? "Grouped" : "List";

        partial void OnMovesViewModeChanged(string value)
        {
            OnPropertyChanged(nameof(IsMovesListView));
            OnPropertyChanged(nameof(IsMovesGroupedView));
        }

        partial void OnMovesGroupByChanged(string value) => ApplyMovesView();

        [RelayCommand]
        private void MovesNextPage()
        {
            if (!MovesCanNext) return;
            MovesPage++;
            ApplyMovesView();
        }

        [RelayCommand]
        private void MovesPrevPage()
        {
            if (!MovesCanPrev) return;
            MovesPage--;
            ApplyMovesView();
        }

        [RelayCommand]
        private void ToggleMoveGroup(MoveGroup? group)
        {
            if (group != null) group.IsExpanded = !group.IsExpanded;
        }

        // Replenishment popup (a product's still-incoming / still-outgoing document lines)
        [ObservableProperty] private bool _isReplenishmentModalOpen;
        [ObservableProperty] private string _replenishmentTitle = string.Empty;
        [ObservableProperty] private int _replenishmentOnHand;
        [ObservableProperty] private int _replenishmentIncoming;
        [ObservableProperty] private int _replenishmentOutgoing;
        [ObservableProperty] private int _replenishmentForecasted;
        [ObservableProperty] private ObservableCollection<ReplenishmentSourceLine> _replenishmentIncomingLines = new();
        [ObservableProperty] private ObservableCollection<ReplenishmentSourceLine> _replenishmentOutgoingLines = new();

        // Locations popup (a product's on-hand quantity per warehouse location)
        [ObservableProperty] private bool _isLocationsModalOpen;
        [ObservableProperty] private string _locationsTitle = string.Empty;
        [ObservableProperty] private ObservableCollection<ProductLocationRow> _productLocations = new();

        [RelayCommand]
        private void SwitchStockReport(string key)
        {
            if (key == "stock-history")
            {
                MovesFilterProductId = null;
                MovesFilterProductName = string.Empty;
            }
            SelectStockNav(key);
        }

        [RelayCommand]
        private void OpenProductHistory(StockReportRow? row)
        {
            if (row == null) return;
            MovesFilterProductId = row.ProductId;
            MovesFilterProductName = row.DisplayName;
            SelectStockNav("stock-history");
        }

        [RelayCommand]
        private void ClearMovesFilter()
        {
            MovesFilterProductId = null;
            MovesFilterProductName = string.Empty;
            _ = LoadStockHistoryReport();
        }

        private void SelectStockNav(string key)
        {
            var catalogItem = ReportCatalog.FirstOrDefault(r => r.Key == key);
            if (catalogItem == null) return;
            if (SelectedCategory != catalogItem.Category)
            {
                SelectedCategory = catalogItem.Category;
            }
            var navItem = ReportsInCategory.FirstOrDefault(r => r.Key == key);
            if (navItem == null) return;
            if (ReferenceEquals(navItem, SelectedReportNavItem))
            {
                _ = LoadSelectedReportAsync();
            }
            else
            {
                SelectedReportNavItem = navItem;
            }
        }

        [RelayCommand]
        private async Task OpenReplenishment(StockReportRow? row)
        {
            if (row == null) return;
            var forecast = await _inventoryService.GetReplenishmentForecastAsync(row.ProductId);
            ReplenishmentTitle = row.DisplayName;
            ReplenishmentOnHand = forecast.OnHand;
            ReplenishmentIncoming = forecast.Incoming;
            ReplenishmentOutgoing = forecast.Outgoing;
            ReplenishmentForecasted = forecast.ForecastedAvailable;
            ReplenishmentIncomingLines = new ObservableCollection<ReplenishmentSourceLine>(forecast.IncomingSources);
            ReplenishmentOutgoingLines = new ObservableCollection<ReplenishmentSourceLine>(forecast.OutgoingSources);
            IsReplenishmentModalOpen = true;
        }

        [RelayCommand]
        private void CloseReplenishmentModal() => IsReplenishmentModalOpen = false;

        [RelayCommand]
        private async Task OpenProductLocations(StockReportRow? row)
        {
            if (row == null) return;
            var locations = await _locationService.GetAllLocationsAsync();
            var byId = locations.ToDictionary(l => l.Id, l => l.Name);
            var stock = await _locationService.GetProductLocationsAsync(row.ProductId);

            LocationsTitle = row.DisplayName;
            ProductLocations = new ObservableCollection<ProductLocationRow>(stock
                .Select(s => new ProductLocationRow
                {
                    LocationName = byId.TryGetValue(s.LocationId, out var n) ? n : $"Location #{s.LocationId}",
                    Quantity = s.Quantity,
                    ReorderPoint = s.ReorderPoint,
                })
                .OrderByDescending(r => r.Quantity));
            IsLocationsModalOpen = true;
        }

        [RelayCommand]
        private void CloseLocationsModal() => IsLocationsModalOpen = false;

        [RelayCommand]
        private async Task LoadMonthlyProfitReport()
        {
            if (!_licenseService.CanAccessProfitAndLoss())
            {
                ReportTitle = "Profit Reports are a Premium Feature. Please Upgrade.";
                return;
            }

            ReportTitle = "Monthly Profit & Loss Summary";
            IsLowStockReport = false;
            IsHistoryReport = false;
            IsProfitReport = true;
            OnPropertyChanged(nameof(IsStockReport));
            var list = await _inventoryService.GetMonthlyProfitSummaryAsync();
            MonthlyProfitData = new ObservableCollection<MonthlyProfitReport>(list);
        }

        [RelayCommand]
        private async Task LoadArAgingAsync()
        {
            ReportTitle = "Accounts Receivable Aging";
            var lines = await _agingReportService.GetAccountsReceivableAgingAsync();
            ArAgingLines = new ObservableCollection<AgingLine>(lines);
            ArAgingSummary = _agingReportService.Summarize(lines);
        }

        [RelayCommand]
        private async Task LoadApAgingAsync()
        {
            ReportTitle = "Accounts Payable Aging";
            var lines = await _agingReportService.GetAccountsPayableAgingAsync();
            ApAgingLines = new ObservableCollection<AgingLine>(lines);
            ApAgingSummary = _agingReportService.Summarize(lines);
        }

        [RelayCommand]
        public async Task LoadGeneralLedgerAsync()
        {
            ReportTitle = "Account Ledger";

            if (_allLedgerAccounts.Count == 0)
            {
                _allLedgerAccounts = await _accountService.GetAllAccountsAsync();
                MatchedLedgerAccounts = new ObservableCollection<Account>(_allLedgerAccounts.Take(8));
            }

            if (SelectedLedgerAccount == null)
            {
                LedgerLines.Clear();
                LedgerOpeningBalance = 0;
                LedgerClosingBalance = 0;
                return;
            }

            var result = await _accountingReportService.GetAccountLedgerAsync(
                SelectedLedgerAccount.Id,
                LedgerFromDate?.Date,
                LedgerToDate?.Date);

            LedgerLines = new ObservableCollection<AccountLedgerLine>(result.Lines);
            LedgerOpeningBalance = result.OpeningBalance;
            LedgerClosingBalance = result.ClosingBalance;
        }

        partial void OnLedgerAccountSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || (SelectedLedgerAccount != null && value == LedgerAccountDisplayName(SelectedLedgerAccount)))
            {
                MatchedLedgerAccounts = new ObservableCollection<Account>(_allLedgerAccounts.Take(8));
                return;
            }

            var query = value.ToLower();
            var matches = _allLedgerAccounts
                .Where(a => a.Name.ToLower().Contains(query) || a.Code.ToLower().Contains(query))
                .Take(8)
                .ToList();
            MatchedLedgerAccounts = new ObservableCollection<Account>(matches);
            SelectedLedgerAccount = null;
        }

        [RelayCommand]
        private void SelectLedgerAccount(Account? account)
        {
            if (account == null) return;
            SelectedLedgerAccount = account;
            LedgerAccountSearchText = LedgerAccountDisplayName(account);
            MatchedLedgerAccounts.Clear();
            _ = LoadGeneralLedgerAsync();
        }

        private static string LedgerAccountDisplayName(Account account) => $"{account.Code} - {account.Name}";

        partial void OnLedgerFromDateChanged(DateTimeOffset? value) => _ = LoadGeneralLedgerAsync();
        partial void OnLedgerToDateChanged(DateTimeOffset? value) => _ = LoadGeneralLedgerAsync();

        [RelayCommand]
        private async Task LoadVatReturnAsync()
        {
            ReportTitle = $"VAT Return ({VatPeriodStart:yyyy-MM-dd} to {VatPeriodEnd:yyyy-MM-dd})";
            VatSummary = await _vatExportService.ComputeVatReturnAsync(VatPeriodStart, VatPeriodEnd);
        }

        [RelayCommand]
        private async Task LoadBudgetVsActualAsync()
        {
            ReportTitle = $"Budget vs Actual — FY {BudgetFiscalYear}";
            int? month = BudgetPeriodMonth is >= 1 and <= 12 ? BudgetPeriodMonth : null;
            var lines = await _budgetReportService.GetBudgetVsActualAsync(BudgetFiscalYear, month);
            BudgetVsActualLines = new ObservableCollection<BudgetVsActualLine>(lines);
            BudgetTotalBudget = lines.Sum(l => l.BudgetAmount);
            BudgetTotalActual = lines.Sum(l => l.ActualAmount);
            BudgetTotalVariance = lines.Sum(l => l.Variance);
        }

        [RelayCommand]
        private async Task LoadBankReconciliationAsync()
        {
            ReportTitle = "Bank Reconciliation";
            ReconStatusMessage = string.Empty;

            var banks = await _paymentService.GetAllBanksAsync();
            var accounts = await _paymentService.GetAllBankAccountsAsync();
            ReconBankAccounts = new ObservableCollection<BankAccountDisplayRow>(
                accounts.Select(a => new BankAccountDisplayRow
                {
                    Account = a,
                    BankName = banks.FirstOrDefault(b => b.Id == a.BankId)?.Name ?? "Unknown Bank"
                }));

            if (SelectedReconBankAccount == null && ReconBankAccounts.Count > 0)
            {
                SelectedReconBankAccount = ReconBankAccounts[0];
            }

            if (SelectedReconBankAccount == null)
            {
                ReconStatusMessage = "Add a bank account in Settings → Payments Setup first.";
                UnreconciledPayments = new ObservableCollection<ReconciliationCandidate>();
                UnreconciledStatementLines = new ObservableCollection<ReconciliationCandidate>();
                return;
            }

            var payments = await _paymentService.GetUnreconciledPaymentsAsync(SelectedReconBankAccount.Id);
            var lines = await _paymentService.GetUnreconciledStatementLinesAsync(SelectedReconBankAccount.Id);
            UnreconciledPayments = new ObservableCollection<ReconciliationCandidate>(payments);
            UnreconciledStatementLines = new ObservableCollection<ReconciliationCandidate>(lines);
            ReconStatusMessage = $"{payments.Count} unreconciled payment(s), {lines.Count} unreconciled statement line(s).";
        }

        [RelayCommand]
        private void OpenImportStatement()
        {
            if (SelectedReconBankAccount == null)
            {
                ReconStatusMessage = "Select a bank account first.";
                return;
            }

            ImportStatementDate = DateTime.Today;
            ImportOpeningBalance = 0;
            ImportClosingBalance = 0;
            ImportLineDate = DateTime.Today;
            ImportLineDescription = string.Empty;
            ImportLineAmount = 0;
            ImportLineReference = string.Empty;
            ImportErrorMessage = string.Empty;
            IsImportStatementModalOpen = true;
        }

        [RelayCommand]
        private async Task SubmitImportStatement()
        {
            ImportErrorMessage = string.Empty;
            if (SelectedReconBankAccount == null)
            {
                ImportErrorMessage = "Select a bank account first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ImportLineDescription))
            {
                ImportErrorMessage = "Line description is required.";
                return;
            }

            if (ImportLineAmount == 0)
            {
                ImportErrorMessage = "Line amount cannot be zero.";
                return;
            }

            try
            {
                await _paymentService.ImportBankStatementAsync(
                    SelectedReconBankAccount.Id,
                    ImportStatementDate,
                    ImportOpeningBalance,
                    ImportClosingBalance,
                    new[] { (ImportLineDate, ImportLineDescription.Trim(), ImportLineAmount, ImportLineReference.Trim()) });

                IsImportStatementModalOpen = false;
                ReconStatusMessage = "Bank statement imported.";
                await LoadBankReconciliationAsync();
            }
            catch (Exception ex)
            {
                ImportErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private void CloseImportStatement()
        {
            IsImportStatementModalOpen = false;
        }

        [RelayCommand]
        private async Task ImportStatementCsv()
        {
            ImportErrorMessage = string.Empty;
            if (SelectedReconBankAccount == null)
            {
                ReconStatusMessage = "Select a bank account first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ImportCsvContent))
            {
                ReconStatusMessage = "Paste CSV content (Date,Description,Amount,Reference).";
                return;
            }

            try
            {
                await _paymentService.ImportBankStatementCsvAsync(
                    SelectedReconBankAccount.Id,
                    ImportCsvContent,
                    ImportStatementDate,
                    ImportOpeningBalance,
                    ImportClosingBalance);
                ReconStatusMessage = "CSV statement imported.";
                ImportCsvContent = string.Empty;
                await LoadBankReconciliationAsync();
            }
            catch (Exception ex)
            {
                ReconStatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task LoadBankSuggestions()
        {
            BankSuggestions.Clear();
            if (SelectedReconBankAccount == null) return;
            var suggestions = await _paymentService.SuggestMatchesAsync(SelectedReconBankAccount.Id);
            foreach (var s in suggestions) BankSuggestions.Add(s);
            ReconStatusMessage = suggestions.Count == 0
                ? "No suggestions found."
                : $"{suggestions.Count} suggested match(es).";
        }

        [RelayCommand]
        private async Task AcceptBankSuggestions()
        {
            if (SelectedReconBankAccount == null) return;
            try
            {
                var count = await _paymentService.AcceptSuggestedMatchesAsync(
                    SelectedReconBankAccount.Id,
                    UserSession.CurrentUser?.Username ?? "System");
                ReconStatusMessage = $"Accepted {count} suggested match(es).";
                await LoadBankReconciliationAsync();
                await LoadBankSuggestions();
            }
            catch (Exception ex)
            {
                ReconStatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task MatchReconciliation()
        {
            ReconStatusMessage = string.Empty;
            if (SelectedReconPayment?.Payment == null || SelectedReconLine?.StatementLine == null)
            {
                ReconStatusMessage = "Select one payment and one statement line to match.";
                return;
            }

            try
            {
                await _paymentService.MatchPaymentToStatementLineAsync(
                    SelectedReconPayment.Payment.Id,
                    SelectedReconLine.StatementLine.Id,
                    UserSession.CurrentUser?.Username ?? "System");
                ReconStatusMessage = "Payment matched to bank statement line.";
                await LoadBankReconciliationAsync();
            }
            catch (Exception ex)
            {
                ReconStatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task ExportVatToCsv()
        {
            if (VatSummary == null)
            {
                await LoadVatReturnAsync();
            }

            if (VatSummary == null) return;

            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
            {
                ReportTitle = "Error: Cannot access file system.";
                return;
            }

            var storageProvider = desktop.MainWindow.StorageProvider;
            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save VAT Return As",
                DefaultExtension = ".csv",
                SuggestedFileName = $"VAT_Return_{VatPeriodStart:yyyyMMdd}_{VatPeriodEnd:yyyyMMdd}",
                FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
            });

            if (file == null) return;

            var csv = _vatExportService.BuildVatReturnCsv(VatSummary);
            await File.WriteAllTextAsync(file.Path.LocalPath, csv);
            ReportTitle += " (VAT Exported)";
        }

        [RelayCommand]
        private async Task ExportToCsv()
        {
            if (!_licenseService.CanAccessExport())
            {
                ReportTitle = "Export is a Premium Feature. Please Upgrade.";
                return;
            }

            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
            {
                ReportTitle = "Error: Cannot access file system.";
                return;
            }

            var storageProvider = desktop.MainWindow.StorageProvider;
            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Report As",
                DefaultExtension = ".csv",
                SuggestedFileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmmss}",
                FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
            });

            if (file == null) return;

            // Simple Export implementation
            var sb = new StringBuilder();
            sb.AppendLine("SKU,Name,Category,TotalValue,UnitCost,SalesPrice,OnHand,FreeToUse,Incoming,Outgoing,Unit");
            foreach (var r in StockRows)
            {
                sb.AppendLine($"{Escape(r.Sku)},{Escape(r.Name)},{Escape(r.Category)},{r.TotalValue},{r.UnitCost},{r.SalesPrice},{r.OnHand},{r.FreeToUse},{r.Incoming},{r.Outgoing},{r.Unit}");
            }

            await File.WriteAllTextAsync(file.Path.LocalPath, sb.ToString());

            ReportTitle += " (Exported)";
        }

        private string Escape(string val)
        {
            if (val.Contains(",")) return $"\"{val}\"";
            return val;
        }

        // --- Export the current accounting/stock report as a shareable PDF or XLSX ---

        [RelayCommand]
        private Task ExportCurrentReportPdf() => ExportCurrentReportAsync(asPdf: true);

        [RelayCommand]
        private Task ExportCurrentReportXlsx() => ExportCurrentReportAsync(asPdf: false);

        private async Task ExportCurrentReportAsync(bool asPdf)
        {
            if (!_licenseService.CanAccessExport())
            {
                ReportTitle = "Export is a Premium Feature. Please Upgrade.";
                return;
            }

            if (IsGeneralLedgerSelected && SelectedLedgerAccount == null)
            {
                ReportTitle = "Pick a ledger account before exporting.";
                return;
            }

            // Make sure the on-screen figures are current before we serialise them.
            await LoadSelectedReportAsync();

            var model = BuildCurrentReportExportModel();
            if (model == null)
            {
                ReportTitle = "This report can't be exported yet.";
                return;
            }

            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow == null)
            {
                ReportTitle = "Error: Cannot access file system.";
                return;
            }

            var ext = asPdf ? ".pdf" : ".xlsx";
            var typeName = asPdf ? "PDF Document" : "Excel Workbook";
            var pattern = asPdf ? "*.pdf" : "*.xlsx";

            var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = $"Export {model.Title}",
                DefaultExtension = ext,
                SuggestedFileName = $"{FileStamp(model.Title)}_{DateTime.Now:yyyyMMdd}",
                FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType(typeName) { Patterns = new[] { pattern } } }
            });
            if (file == null) return;

            try
            {
                var bytes = asPdf ? _reportExportService.BuildPdf(model) : _reportExportService.BuildXlsx(model);
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(bytes.AsMemory());
                ReportTitle = $"{model.Title} — exported";
            }
            catch (Exception ex)
            {
                ReportTitle = $"Export failed: {ex.Message}";
            }
        }

        private static string FileStamp(string title)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var clean = new string(title.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray());
            return string.Join("_", clean.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string M(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture);

        private ReportExportModel? BuildCurrentReportExportModel()
        {
            var company = _settingsService.CurrentSettings.StoreName;

            return SelectedReportNavItem?.Key switch
            {
                "balance-sheet" => StatementModel("Balance Sheet", company, BalanceSheetLines),
                "profit-loss" => StatementModel("Profit and Loss Statement", company, ProfitAndLossLines),
                "ar-aging" => AgingModel("Accounts Receivable Aging", company, ArAgingLines, ArAgingSummary),
                "ap-aging" => AgingModel("Accounts Payable Aging", company, ApAgingLines, ApAgingSummary),
                "budget-vs-actual" => BudgetModel(company),
                "general-ledger" => LedgerModel(company),
                "vat-return" => VatModel(company),
                "stock-status" => StockModel(company),
                _ => null,
            };
        }

        private ReportExportModel StatementModel(string title, string company, IEnumerable<ReportLineWrapper> lines)
        {
            var rows = lines.Select(w => new ReportExportRow
            {
                Cells = new[] { w.Name, w.Result.HasComputations ? M(w.Result.Balance) : string.Empty },
                Indent = Math.Max(0, w.Result.Level - 1),
                Bold = w.Result.Level is >= 1 and <= 2,
            }).ToList();

            return new ReportExportModel
            {
                Title = title,
                CompanyName = company,
                Subtitle = $"As at {DateTime.Today:yyyy-MM-dd}  ·  Amounts in {CurrencySymbol}",
                Columns = new[]
                {
                    new ReportExportColumn("Line", width: 4),
                    new ReportExportColumn("Balance", rightAlign: true, width: 1.4),
                },
                Rows = rows,
            };
        }

        private ReportExportModel AgingModel(string title, string company, IEnumerable<AgingLine> lines, AgingSummary summary)
        {
            var rows = lines.Select(l => new ReportExportRow
            {
                Cells = new[]
                {
                    l.PartnerName, l.DocumentNumber, l.DocumentDate.ToString("yyyy-MM-dd"),
                    l.DueDate.ToString("yyyy-MM-dd"), l.DaysOverdue.ToString(CultureInfo.InvariantCulture),
                    M(l.TotalAmount), M(l.OpenBalance), l.AgingBucket,
                }
            }).ToList();

            rows.Add(new ReportExportRow
            {
                Bold = true,
                Cells = new[] { "Total Open", "", "", "", "", "", M(summary.TotalOpen), "" },
            });

            return new ReportExportModel
            {
                Title = title,
                CompanyName = company,
                Subtitle = $"Generated {DateTime.Today:yyyy-MM-dd}  ·  Current {M(summary.Current)} · 1–30 {M(summary.Days1To30)} · 31–60 {M(summary.Days31To60)} · 61–90 {M(summary.Days61To90)} · 90+ {M(summary.Over90)}",
                Columns = new[]
                {
                    new ReportExportColumn("Partner", width: 2),
                    new ReportExportColumn("Document", width: 1.4),
                    new ReportExportColumn("Doc Date"),
                    new ReportExportColumn("Due Date"),
                    new ReportExportColumn("Days", rightAlign: true, width: 0.7),
                    new ReportExportColumn("Total", rightAlign: true),
                    new ReportExportColumn("Open", rightAlign: true),
                    new ReportExportColumn("Bucket", width: 0.9),
                },
                Rows = rows,
            };
        }

        private ReportExportModel BudgetModel(string company)
        {
            var rows = BudgetVsActualLines.Select(l => new ReportExportRow
            {
                Cells = new[]
                {
                    l.AccountCode, l.AccountName, M(l.BudgetAmount), M(l.ActualAmount), M(l.Variance),
                    $"{l.VariancePercent.ToString("N1", CultureInfo.CurrentCulture)}%",
                }
            }).ToList();

            rows.Add(new ReportExportRow
            {
                Bold = true,
                Cells = new[] { "", "Total", M(BudgetTotalBudget), M(BudgetTotalActual), M(BudgetTotalVariance), "" },
            });

            return new ReportExportModel
            {
                Title = $"Budget vs Actual — FY {BudgetFiscalYear}",
                CompanyName = company,
                Subtitle = (BudgetPeriodMonth is >= 1 and <= 12 ? $"Month {BudgetPeriodMonth}" : "Full year")
                           + $"  ·  Amounts in {CurrencySymbol}",
                Columns = new[]
                {
                    new ReportExportColumn("Code", width: 0.8),
                    new ReportExportColumn("Account", width: 2.4),
                    new ReportExportColumn("Budget", rightAlign: true),
                    new ReportExportColumn("Actual", rightAlign: true),
                    new ReportExportColumn("Variance", rightAlign: true),
                    new ReportExportColumn("Var %", rightAlign: true, width: 0.8),
                },
                Rows = rows,
            };
        }

        private ReportExportModel? LedgerModel(string company)
        {
            if (SelectedLedgerAccount == null) return null;

            var rows = new List<ReportExportRow>
            {
                new() { Bold = true, Cells = new[] { "", "", "", "Opening balance", "", "", M(LedgerOpeningBalance) } },
            };
            rows.AddRange(LedgerLines.Select(l => new ReportExportRow
            {
                Cells = new[]
                {
                    l.Date.ToString("yyyy-MM-dd"), l.EntryNumber, l.Reference, l.Label,
                    M(l.Debit), M(l.Credit), M(l.RunningBalance),
                }
            }));
            rows.Add(new ReportExportRow { Bold = true, Cells = new[] { "", "", "", "Closing balance", "", "", M(LedgerClosingBalance) } });

            var range = (LedgerFromDate, LedgerToDate) switch
            {
                ({ } f, { } t) => $"{f:yyyy-MM-dd} to {t:yyyy-MM-dd}",
                ({ } f, null) => $"from {f:yyyy-MM-dd}",
                (null, { } t) => $"until {t:yyyy-MM-dd}",
                _ => "all dates",
            };

            return new ReportExportModel
            {
                Title = $"Account Ledger — {SelectedLedgerAccount.Code} {SelectedLedgerAccount.Name}",
                CompanyName = company,
                Subtitle = $"{range}  ·  Amounts in {CurrencySymbol}",
                Columns = new[]
                {
                    new ReportExportColumn("Date"),
                    new ReportExportColumn("Entry #", width: 1.1),
                    new ReportExportColumn("Reference", width: 1.4),
                    new ReportExportColumn("Label", width: 2.4),
                    new ReportExportColumn("Debit", rightAlign: true),
                    new ReportExportColumn("Credit", rightAlign: true),
                    new ReportExportColumn("Balance", rightAlign: true),
                },
                Rows = rows,
            };
        }

        private ReportExportModel? VatModel(string company)
        {
            if (VatSummary == null) return null;
            var s = VatSummary;

            var rows = new List<ReportExportRow>
            {
                new() { Cells = new[] { "Taxable sales", M(s.TaxableSales) } },
                new() { Cells = new[] { "Output VAT (on sales)", M(s.OutputVat) } },
                new() { Cells = new[] { "Taxable purchases", M(s.TaxablePurchases) } },
                new() { Cells = new[] { "Input VAT (on purchases)", M(s.InputVat) } },
                new() { Bold = true, Cells = new[] { "Net VAT payable", M(s.NetVatPayable) } },
            };

            return new ReportExportModel
            {
                Title = "VAT Return",
                CompanyName = company,
                Subtitle = $"{s.PeriodStart:yyyy-MM-dd} to {s.PeriodEnd:yyyy-MM-dd}  ·  Amounts in {CurrencySymbol}",
                Columns = new[]
                {
                    new ReportExportColumn("Item", width: 3),
                    new ReportExportColumn("Amount", rightAlign: true),
                },
                Rows = rows,
            };
        }

        private ReportExportModel StockModel(string company)
        {
            var rows = StockRows.Select(r => new ReportExportRow
            {
                Cells = new[]
                {
                    r.Sku, r.Name, r.Category,
                    M(r.TotalValue), M(r.UnitCost), M(r.SalesPrice),
                    r.OnHand.ToString(CultureInfo.InvariantCulture),
                    r.FreeToUse.ToString(CultureInfo.InvariantCulture),
                    r.Incoming.ToString(CultureInfo.InvariantCulture),
                    r.Outgoing.ToString(CultureInfo.InvariantCulture),
                    r.Unit,
                }
            }).ToList();

            return new ReportExportModel
            {
                Title = IsLowStockReport ? "Stock — low on hand" : "Stock",
                CompanyName = company,
                Subtitle = $"Generated {DateTime.Today:yyyy-MM-dd}  ·  {StockRows.Count} product(s)  ·  Total value {M(StockRows.Sum(r => r.TotalValue))}",
                Columns = new[]
                {
                    new ReportExportColumn("SKU", width: 1.1),
                    new ReportExportColumn("Name", width: 2.4),
                    new ReportExportColumn("Category", width: 1.3),
                    new ReportExportColumn("Total Value", rightAlign: true),
                    new ReportExportColumn("Unit Cost", rightAlign: true),
                    new ReportExportColumn("Sales Price", rightAlign: true),
                    new ReportExportColumn("On Hand", rightAlign: true, width: 0.8),
                    new ReportExportColumn("Free to Use", rightAlign: true, width: 0.9),
                    new ReportExportColumn("Incoming", rightAlign: true, width: 0.8),
                    new ReportExportColumn("Outgoing", rightAlign: true, width: 0.8),
                    new ReportExportColumn("Unit", width: 0.7),
                },
                Rows = rows,
            };
        }

        // --- Details Modal Logic ---
        [ObservableProperty] private StockMovement? _selectedStockMovement;
        [ObservableProperty] private bool _isDetailsModalOpen;

        [RelayCommand]
        private void OpenDetails()
        {
            if (SelectedStockMovement != null)
            {
                IsDetailsModalOpen = true;
            }
        }

        [RelayCommand]
        private void CloseDetails()
        {
            IsDetailsModalOpen = false;
        }

        [RelayCommand]
        private async Task CopyToClipboard(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(text);
            }
        }

        // --- Journal Entries drill-down logic ---
        [ObservableProperty] private ObservableCollection<JournalEntryDetailRow> _detailedJournalLines = new();
        [ObservableProperty] private bool _isJournalModalOpen;
        [ObservableProperty] private string _detailedJournalTitle = string.Empty;
        [ObservableProperty] private decimal _totalDebit;
        [ObservableProperty] private decimal _totalCredit;
        [ObservableProperty] private bool _isBalanced;
        [ObservableProperty] private decimal _balanceDifference;

        [RelayCommand]
        private async Task OpenJournalEntries(ReportLineWrapper wrapper)
        {
            if (wrapper == null || !wrapper.Result.HasComputations) return;

            DetailedJournalTitle = $"Journal Entries - {wrapper.Name}";
            IsLoadingReport = true;
            try
            {
                var lines = await _accountingReportService.GetJournalLinesForReportLineAsync(wrapper.Result.LineId);
                DetailedJournalLines = new ObservableCollection<JournalEntryDetailRow>(lines);
                
                TotalDebit = lines.Sum(l => l.Debit);
                TotalCredit = lines.Sum(l => l.Credit);
                BalanceDifference = wrapper.Result.Balance;
                IsBalanced = true;

                IsJournalModalOpen = true;
            }
            catch (Exception ex)
            {
                ReportTitle = $"Error loading journal entries: {ex.Message}";
            }
            finally
            {
                IsLoadingReport = false;
            }
        }

        [RelayCommand]
        private void CloseJournalModal()
        {
            IsJournalModalOpen = false;
        }

        private async Task LoadAbcAnalysisAsync()
        {
            ReportTitle = "ABC Analysis";
            AbcAnalysisLines.Clear();
            var lines = await _advancedAnalyticsService.GetAbcAnalysisAsync();
            foreach (var line in lines) AbcAnalysisLines.Add(line);
        }

        private async Task LoadDeadStockAsync()
        {
            ReportTitle = "Dead Stock Report (90 days)";
            DeadStockLines.Clear();
            var lines = await _advancedAnalyticsService.GetDeadStockReportAsync();
            foreach (var line in lines) DeadStockLines.Add(line);
        }

        private async Task LoadMarginByCategoryAsync()
        {
            ReportTitle = "Margin by Category";
            CategoryMarginLines.Clear();
            var lines = await _advancedAnalyticsService.GetMarginByCategoryAsync();
            foreach (var line in lines) CategoryMarginLines.Add(line);
        }

        private async Task LoadMonthCloseAsync()
        {
            var now = DateTime.Today;
            ReportTitle = $"Month Close Summary — {now:MMMM yyyy}";
            MonthCloseSummary = await _monthCloseService.GetMonthCloseSummaryAsync(now.Year, now.Month);
        }
    }

    public class ProductLocationRow
    {
        public string LocationName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReorderPoint { get; set; }
    }

    /// <summary>A collapsible group of stock moves in the Grouped view of Moves History.</summary>
    public partial class MoveGroup : ObservableObject
    {
        public MoveGroup(string key, System.Collections.Generic.List<StockMoveHistoryRow> rows)
        {
            Key = string.IsNullOrWhiteSpace(key) ? "(none)" : key;
            Rows = new ObservableCollection<StockMoveHistoryRow>(rows);
            Count = rows.Count;
            TotalIn = rows.Where(r => !r.IsOutbound).Sum(r => r.Quantity);
            TotalOut = rows.Where(r => r.IsOutbound).Sum(r => r.Quantity);
        }

        public string Key { get; }
        public ObservableCollection<StockMoveHistoryRow> Rows { get; }
        public int Count { get; }
        public int TotalIn { get; }
        public int TotalOut { get; }
        public string Summary => $"{Count} move(s)   +{TotalIn} / -{TotalOut}";
        public string Glyph => IsExpanded ? "▾" : "▸";

        [ObservableProperty] private bool _isExpanded;

        partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Glyph));
    }

    public class ReportLineWrapper
    {
        public ReportLineResult Result { get; }
        public string CurrencySymbol { get; }
        
        public Avalonia.Thickness Margin => new Avalonia.Thickness((Result.Level - 1) * 20, 6, 10, 6);
        public bool IsHeader => Result.Level == 1;
        public bool IsSubHeader => Result.Level == 2;
        public string Name => Result.Name;
        public string FormattedBalance
        {
            get
            {
                if (!Result.HasComputations)
                {
                    return string.Empty;
                }
                if (Result.Balance < 0)
                {
                    return $"({Math.Abs(Result.Balance):N0}) {CurrencySymbol}";
                }
                return $"{Result.Balance:N0} {CurrencySymbol}";
            }
        }

        public ReportLineWrapper(ReportLineResult result, string currencySymbol)
        {
            Result = result;
            CurrencySymbol = currencySymbol;
        }
    }
}
