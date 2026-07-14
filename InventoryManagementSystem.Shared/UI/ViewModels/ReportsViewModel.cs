using System;
using System.Collections.ObjectModel;
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
    public partial class ReportsViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly LicenseService _licenseService;
        private readonly SettingsService _settingsService;
        private readonly AccountingReportService _accountingReportService;
        private readonly AgingReportService _agingReportService;

        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private ObservableCollection<ReportLineWrapper> _balanceSheetLines = new();
        [ObservableProperty] private ObservableCollection<ReportLineWrapper> _profitAndLossLines = new();
        [ObservableProperty] private ObservableCollection<AgingLine> _arAgingLines = new();
        [ObservableProperty] private ObservableCollection<AgingLine> _apAgingLines = new();
        [ObservableProperty] private AgingSummary _arAgingSummary = new();
        [ObservableProperty] private AgingSummary _apAgingSummary = new();
        [ObservableProperty] private bool _isLoadingReport;

        [ObservableProperty] private ObservableCollection<Product> _reportData = new();
        [ObservableProperty] private ObservableCollection<StockMovement> _stockHistoryData = new();
        [ObservableProperty] private ObservableCollection<MonthlyProfitReport> _monthlyProfitData = new();
        [ObservableProperty] private string _reportTitle = "Balance Sheet";
        [ObservableProperty] private bool _isLowStockReport;
        [ObservableProperty] private bool _isHistoryReport;
        [ObservableProperty] private bool _isProfitReport;

        public bool IsStockReport => !IsHistoryReport && !IsProfitReport;

        public bool IsBalanceSheetSelected => SelectedTabIndex == 0;
        public bool IsProfitAndLossSelected => SelectedTabIndex == 1;
        public bool IsStockStatusSelected => SelectedTabIndex == 2;
        public bool IsStockHistorySelected => SelectedTabIndex == 3;
        public bool IsArAgingSelected => SelectedTabIndex == 4;
        public bool IsApAgingSelected => SelectedTabIndex == 5;

        partial void OnSelectedTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(IsBalanceSheetSelected));
            OnPropertyChanged(nameof(IsProfitAndLossSelected));
            OnPropertyChanged(nameof(IsStockStatusSelected));
            OnPropertyChanged(nameof(IsStockHistorySelected));
            OnPropertyChanged(nameof(IsArAgingSelected));
            OnPropertyChanged(nameof(IsApAgingSelected));
            _ = LoadSelectedReportAsync();
        }

        public string CurrencySymbol => _settingsService.CurrentSettings.CurrencySymbol;
        public LanguageService Language { get; }

        public ReportsViewModel(
            InventoryService inventoryService, 
            LicenseService licenseService, 
            SettingsService settingsService, 
            LanguageService languageService,
            AccountingReportService accountingReportService,
            AgingReportService agingReportService)
        {
            _inventoryService = inventoryService;
            _licenseService = licenseService;
            _settingsService = settingsService;
            Language = languageService;
            _accountingReportService = accountingReportService;
            _agingReportService = agingReportService;
            
            SelectedTabIndex = 0; // Default to Balance Sheet
            _ = LoadSelectedReportAsync();
        }

        public async Task LoadSelectedReportAsync()
        {
            IsLoadingReport = true;
            try
            {
                if (SelectedTabIndex == 0)
                {
                    await LoadBalanceSheetAsync();
                }
                else if (SelectedTabIndex == 1)
                {
                    await LoadProfitAndLossAsync();
                }
                else if (SelectedTabIndex == 2)
                {
                    if (IsLowStockReport)
                        await LoadLowStockReport();
                    else
                        await LoadStockReport();
                }
                else if (SelectedTabIndex == 3)
                {
                    await LoadStockHistoryReport();
                }
                else if (SelectedTabIndex == 4)
                {
                    await LoadArAgingAsync();
                }
                else if (SelectedTabIndex == 5)
                {
                    await LoadApAgingAsync();
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
            ReportTitle = "Current Stock Report";
            IsLowStockReport = false;
            IsHistoryReport = false;
            IsProfitReport = false;
            OnPropertyChanged(nameof(IsStockReport));
            var list = await _inventoryService.GetAllProductsAsync();
            ReportData = new ObservableCollection<Product>(list);
        }

        [RelayCommand]
        private async Task LoadLowStockReport()
        {
            ReportTitle = "Low Stock Report (< 5 items)";
            IsLowStockReport = true;
            IsHistoryReport = false;
            IsProfitReport = false;
            OnPropertyChanged(nameof(IsStockReport));
            var list = await _inventoryService.GetLowStockProductsAsync(5);
            ReportData = new ObservableCollection<Product>(list);
        }

        [RelayCommand]
        private async Task LoadStockHistoryReport()
        {
            if (!_licenseService.CanAccessAdvancedReports())
            {
                ReportTitle = "History is a Premium Feature. Please Upgrade.";
                return;
            }

            ReportTitle = "Stock Movement History";
            IsLowStockReport = false;
            IsHistoryReport = true;
            IsProfitReport = false;
            OnPropertyChanged(nameof(IsStockReport));
            var list = await _inventoryService.GetRecentStockMovementsAsync(100);
            StockHistoryData = new ObservableCollection<StockMovement>(list);
        }

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
            sb.AppendLine("ID,Name,SKU,Category,Stock,Unit,Price,Cost");
            foreach (var p in ReportData)
            {
                sb.AppendLine($"{p.Id},{Escape(p.Name)},{Escape(p.SKU ?? "")},{Escape(p.Category)},{p.StockQuantity},{p.Unit},{p.Price},{p.Cost}");
            }

            await File.WriteAllTextAsync(file.Path.LocalPath, sb.ToString());

            ReportTitle += " (Exported)";
        }

        private string Escape(string val)
        {
            if (val.Contains(",")) return $"\"{val}\"";
            return val;
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
                BalanceDifference = Math.Abs(TotalDebit - TotalCredit);
                IsBalanced = BalanceDifference < 0.01m;

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
