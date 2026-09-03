using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace InventoryManagementSystem.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly InventoryService _inventoryService;
    private readonly UserService _userService;
    private readonly LicenseService _licenseService;
    private readonly HardwareIdService _hardwareIdService;
    private readonly AnalyticsService _analyticsService;
    private readonly ReceiptService _receiptService;
    private readonly UpdateService _updateService;
    private readonly SettingsService _settingsService;
    private readonly SupplierService _supplierService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly SalesOrderService _salesOrderService;
    private readonly ForecastingService _forecastingService;
    private readonly ExpiryService _expiryService;
    private readonly LocationService _locationService;
    private readonly ReturnsService _returnsService;
    private readonly AdvancedAnalyticsService _advancedAnalyticsService;
    private readonly BundleService _bundleService;
    private readonly AuditService _auditService;
    private readonly ReportingService _reportingService;
    private readonly CloudSyncService _cloudSyncService;
    private readonly DailyBriefingService _dailyBriefingService;
    private readonly TaxService _taxService;
    private readonly AccountService _accountService;
    private readonly JournalService _journalService;
    private readonly AccountingReportService _accountingReportService;
    private readonly ManufacturingService _manufacturingService;
    private readonly PaymentService _paymentService;
    private readonly IndustryTemplateService _industryTemplateService;
    private readonly CustomFieldService _customFieldService;
    private readonly CustomerService _customerService;
    private readonly BarcodeService _barcodeService;
    private readonly AgingReportService _agingReportService;
    private readonly VatExportService _vatExportService;
    private readonly BudgetReportService _budgetReportService;
    private readonly CurrencyService _currencyService;
    private readonly CycleCountService _cycleCountService;
    private readonly IntegrationWebhookService _integrationWebhookService;
    private readonly NotificationService _notificationService;
    private readonly MonthCloseService _monthCloseService;
    private readonly CompanyBranchService _companyBranchService;
    private readonly WorkflowApprovalService _workflowApprovalService;
    private readonly MrpPlanningService _mrpPlanningService;
    private readonly CrmPipelineService _crmPipelineService;
    private readonly MobileFieldService _mobileFieldService;
    private readonly SecurityComplianceService _securityComplianceService;
    private readonly DocumentAttachmentService _documentAttachmentService;
    private readonly RecurringInvoiceService _recurringInvoiceService;
    private readonly ExpenseService _expenseService;
    private readonly DamageWriteOffService _damageWriteOffService;
    private readonly PosSessionService _posSessionService;
    private readonly EmployeeService _employeeService;
    private readonly AttendanceService _attendanceService;
    private readonly LeaveService _leaveService;

    public IndustryTemplateService IndustryTemplateService => _industryTemplateService;
    public CustomFieldService CustomFieldService => _customFieldService;
    public CustomerService CustomerService => _customerService;

    // Navigation Stack
    private readonly System.Collections.Generic.Stack<ViewModelBase> _navigationStack = new();

    [ObservableProperty]
    private bool _canGoBack;
    
    public LanguageService Language { get; } 

    [ObservableProperty]
    private ViewModelBase _currentPage = default!;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _currentUserName = "";

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private Avalonia.Controls.GridLength _sidebarGridLength = new(0);

    // Update Properties
    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateStatusText = "Check Updates"; // Button Label

    [ObservableProperty]
    private string _updateVersion = "";

    [ObservableProperty]
    private string _releaseNotesUrl = "";

    public MainViewModel(
        InventoryService inventoryService,
        UserService userService,
        LicenseService licenseService,
        HardwareIdService hardwareIdService,
        AnalyticsService analyticsService,
        ReceiptService receiptService,
        LanguageService languageService,
        UpdateService updateService,
        SettingsService settingsService,
        SupplierService supplierService,
        PurchaseOrderService purchaseOrderService,
        SalesOrderService salesOrderService,
        ForecastingService forecastingService,
        ExpiryService expiryService,
        LocationService locationService,
        ReturnsService returnsService,
        AdvancedAnalyticsService advancedAnalyticsService,
        BundleService bundleService,
        AuditService auditService,
        ReportingService reportingService,
        CloudSyncService cloudSyncService,
        DailyBriefingService dailyBriefingService,
        TaxService taxService,
        AccountService accountService,
        JournalService journalService,
        AccountingReportService accountingReportService,
        ManufacturingService manufacturingService,
        PaymentService paymentService,
        IndustryTemplateService industryTemplateService,
        CustomFieldService customFieldService,
        CustomerService customerService,
        BarcodeService barcodeService,
        AgingReportService agingReportService,
        VatExportService vatExportService,
        BudgetReportService budgetReportService,
        CurrencyService currencyService,
        CycleCountService cycleCountService,
        IntegrationWebhookService integrationWebhookService,
        NotificationService notificationService,
        MonthCloseService monthCloseService,
        CompanyBranchService companyBranchService,
        WorkflowApprovalService workflowApprovalService,
        MrpPlanningService mrpPlanningService,
        CrmPipelineService crmPipelineService,
        MobileFieldService mobileFieldService,
        SecurityComplianceService securityComplianceService,
        DocumentAttachmentService documentAttachmentService,
        RecurringInvoiceService recurringInvoiceService,
        ExpenseService expenseService,
        DamageWriteOffService damageWriteOffService,
        PosSessionService posSessionService,
        EmployeeService employeeService,
        AttendanceService attendanceService,
        LeaveService leaveService)
    {
        _inventoryService = inventoryService;
        _userService = userService;
        _licenseService = licenseService;
        _hardwareIdService = hardwareIdService;
        _analyticsService = analyticsService;
        _receiptService = receiptService;
        Language = languageService;
        _updateService = updateService;
        _settingsService = settingsService;
        _supplierService = supplierService;
        _purchaseOrderService = purchaseOrderService;
        _salesOrderService = salesOrderService;
        _forecastingService = forecastingService;
        _expiryService = expiryService;
        _locationService = locationService;
        _returnsService = returnsService;
        _advancedAnalyticsService = advancedAnalyticsService;
        _bundleService = bundleService;
        _auditService = auditService;
        _reportingService = reportingService;
        _cloudSyncService = cloudSyncService;
        _dailyBriefingService = dailyBriefingService;
        _taxService = taxService;
        _accountService = accountService;
        _journalService = journalService;
        _accountingReportService = accountingReportService;
        _manufacturingService = manufacturingService;
        _paymentService = paymentService;
        _industryTemplateService = industryTemplateService;
        _customFieldService = customFieldService;
        _customerService = customerService;
        _barcodeService = barcodeService;
        _agingReportService = agingReportService;
        _vatExportService = vatExportService;
        _budgetReportService = budgetReportService;
        _currencyService = currencyService;
        _cycleCountService = cycleCountService;
        _integrationWebhookService = integrationWebhookService;
        _notificationService = notificationService;
        _monthCloseService = monthCloseService;
        _companyBranchService = companyBranchService;
        _workflowApprovalService = workflowApprovalService;
        _mrpPlanningService = mrpPlanningService;
        _crmPipelineService = crmPipelineService;
        _mobileFieldService = mobileFieldService;
        _securityComplianceService = securityComplianceService;
        _documentAttachmentService = documentAttachmentService;
        _recurringInvoiceService = recurringInvoiceService;
        _expenseService = expenseService;
        _damageWriteOffService = damageWriteOffService;
        _posSessionService = posSessionService;
        _employeeService = employeeService;
        _attendanceService = attendanceService;
        _leaveService = leaveService;

        // Check for updates on startup (fire and forget, silent)
        _ = CheckForUpdatesInternal(false);
        _ = RefreshCloudSyncStatusAsync();
        _ = RunPhase1BackgroundJobsAsync();

        // 1. Strict License Check: Lock app if status is not Active/Valid
        var status = _licenseService.CurrentLicense.Status;
        if (status != "Active" && status != "Valid")
        {
            GoToLicense();
            IsLoggedIn = false;
            SidebarGridLength = new Avalonia.Controls.GridLength(0);
            return;
        }

        // 2. Show login screen — each user signs in with username/password
        ShowLoginScreen();
    }

    private void ShowLoginScreen()
    {
        UserSession.Logout();
        _navigationStack.Clear();
        CanGoBack = false;
        IsLoggedIn = false;
        SidebarGridLength = new Avalonia.Controls.GridLength(0);
        CurrentPage = new LoginViewModel(_userService, _auditService, OnLoginSuccess);
    }

    private void OnLoginSuccess()
    {
        // Safety re-check
        if (_licenseService.CurrentLicense.Status != "Valid" && _licenseService.CurrentLicense.Status != "Active")
        {
            GoToLicense();
            return;
        }

        IsLoggedIn = true;
        SidebarGridLength = new Avalonia.Controls.GridLength(250);
        CurrentUserName = UserSession.CurrentUser?.Username ?? "Unknown";
        IsAdmin = UserSession.IsAdmin;

        if (!_settingsService.CurrentSettings.SetupCompleted)
        {
            ShowSetupWizard(EnterDashboardAfterLogin);
            return;
        }

        EnterDashboardAfterLogin();
    }

    private void ShowSetupWizard(Action onCompleted)
    {
        CurrentPage = new SetupWizardViewModel(_industryTemplateService, _settingsService, _customFieldService, Language, onCompleted);
        _navigationStack.Clear();
        CanGoBack = false;
    }

    public void RunSetupWizardFromSettings()
    {
        ShowSetupWizard(() =>
        {
            RefreshModuleGatedAccessProperties();
            GoToSettings();
        });
    }

    public void RefreshModuleGatedAccessProperties()
    {
        OnPropertyChanged(nameof(CanAccessManufacturing));
        OnPropertyChanged(nameof(CanAccessBundles));
        OnPropertyChanged(nameof(CanAccessExpiry));
        OnPropertyChanged(nameof(CanAccessLocations));
        OnPropertyChanged(nameof(CanAccessPOS));
    }

    private void EnterDashboardAfterLogin()
    {
        // Start fresh on Dashboard, clearing any login/license history
        CurrentPage = new DashboardViewModel(_inventoryService, _licenseService, Language, _settingsService, _dailyBriefingService, _salesOrderService, GoToInventory, GoToReports, GoToPOS);
        _navigationStack.Clear();
        CanGoBack = false;
        OnPropertyChanged(nameof(CanAccessPOS));
        OnPropertyChanged(nameof(CanAccessReports));
        OnPropertyChanged(nameof(CanAccessInventory));
        OnPropertyChanged(nameof(CanAccessSuppliers));
        OnPropertyChanged(nameof(CanAccessEmployees));
        OnPropertyChanged(nameof(CanAccessAttendance));
        OnPropertyChanged(nameof(CanAccessLeaveRequests));
        OnPropertyChanged(nameof(CanAccessCustomers));
        OnPropertyChanged(nameof(CanAccessPurchaseOrders));
        OnPropertyChanged(nameof(CanAccessAccountsPayable));
        OnPropertyChanged(nameof(CanAccessForecasting));
        OnPropertyChanged(nameof(CanAccessExpiry));
        OnPropertyChanged(nameof(CanAccessLocations));
        OnPropertyChanged(nameof(CanAccessReturns));
        OnPropertyChanged(nameof(CanAccessBundles));
        OnPropertyChanged(nameof(CanAccessManufacturing));
        OnPropertyChanged(nameof(CanAccessAudit));
        OnPropertyChanged(nameof(CanAccessAdvancedAnalytics));
        OnPropertyChanged(nameof(CanAccessAnalytics));
        OnPropertyChanged(nameof(CanAccessCloudSync));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanAccessSettings));
        OnPropertyChanged(nameof(CanAccessEnterprise));

        OnPropertyChanged(nameof(SidebarGridLength));
    }

    private async Task RunPhase1BackgroundJobsAsync()
    {
        try
        {
            await _recurringInvoiceService.RunDueAsync(DateTime.Today, "System");
        }
        catch
        {
            // Best effort — do not block app startup
        }

        try
        {
            await _notificationService.ProcessPendingNotificationsAsync();
        }
        catch
        {
            // Best effort
        }
    }

    private async Task RefreshCloudSyncStatusAsync()
    {
        if (!_licenseService.CanAccessCloudSync())
        {
            SyncStatusText = "Enterprise license required";
            IsCloudConnected = false;
            return;
        }

        var status = await _cloudSyncService.GetStatusAsync();
        IsCloudConnected = status.IsAuthenticated;
        SyncStatusText = status.StatusText;
        LastSyncDisplay = status.LastSyncDate?.ToLocalTime().ToString("g") ?? "Never";
        if (!string.IsNullOrWhiteSpace(status.OrganizationName))
        {
            CloudOrganizationName = status.OrganizationName;
        }
    }

    [RelayCommand]
    public async Task ConnectCloud()
    {
        if (!_licenseService.CanAccessCloudSync())
        {
            GoToLicense();
            return;
        }

        if (string.IsNullOrWhiteSpace(CloudEmail) || string.IsNullOrWhiteSpace(CloudPassword))
        {
            SyncStatusText = "Enter cloud email and password";
            return;
        }

        IsSyncing = true;
        SyncStatusText = "Connecting...";
        var result = await _cloudSyncService.ConfigureCloudLoginAsync(
            CloudEmail.Trim(),
            CloudPassword,
            CloudOrganizationName,
            register: false);

        if (!result.Success)
        {
            result = await _cloudSyncService.ConfigureCloudLoginAsync(
                CloudEmail.Trim(),
                CloudPassword,
                CloudOrganizationName,
                register: false);
        }

        IsSyncing = false;

        SyncStatusText = result.Message;
        if (result.Success)
        {
            IsCloudConnected = true;
            CloudPassword = string.Empty;
        }

        await RefreshCloudSyncStatusAsync();
    }

    [RelayCommand]
    public async Task SyncNow()
    {
        if (!_licenseService.CanAccessCloudSync())
        {
            GoToLicense();
            return;
        }

        if (!IsCloudConnected)
        {
            SyncStatusText = "Connect to cloud first";
            return;
        }

        IsSyncing = true;
        SyncStatusText = "Syncing...";
        var user = UserSession.CurrentUser?.Username ?? "user";
        var result = await _cloudSyncService.SyncNowAsync(user);
        IsSyncing = false;
        SyncStatusText = result.Message;
        await RefreshCloudSyncStatusAsync();
    }

    [RelayCommand]
    public async Task BackupToCloud()
    {
        if (!_licenseService.CanAccessCloudSync())
        {
            GoToLicense();
            return;
        }

        if (!IsCloudConnected)
        {
            SyncStatusText = "Connect to cloud first";
            return;
        }

        IsSyncing = true;
        SyncStatusText = "Uploading backup...";
        var user = UserSession.CurrentUser?.Username ?? "user";
        var success = await _cloudSyncService.BackupToCloudAsync(user, string.Empty);
        IsSyncing = false;
        SyncStatusText = success ? "Backup uploaded" : "Backup failed";
        await RefreshCloudSyncStatusAsync();
    }

    [RelayCommand]
    public async Task RestoreFromCloud()
    {
        if (!_licenseService.CanAccessCloudSync())
        {
            GoToLicense();
            return;
        }

        if (!IsCloudConnected)
        {
            SyncStatusText = "Connect to cloud first";
            return;
        }

        IsSyncing = true;
        SyncStatusText = "Restoring backup...";
        var user = UserSession.CurrentUser?.Username ?? "user";
        var success = await _cloudSyncService.RestoreFromCloudAsync(user, string.Empty);
        IsSyncing = false;
        SyncStatusText = success ? "Restore completed — restart recommended" : "Restore failed";
        await RefreshCloudSyncStatusAsync();
    }

    /// <summary>
    /// Creates a brand-new, isolated local database for a different organization — e.g. an
    /// employee who used this app at a previous employer and now needs to use it at a new one
    /// without mixing the two companies' records in one local file. Takes effect on next launch:
    /// the running app keeps using its current data until restarted.
    /// </summary>
    [RelayCommand]
    public void AddNewCompanyProfile()
    {
        if (!_licenseService.CanAccessCloudSync())
        {
            GoToLicense();
            return;
        }

        if (string.IsNullOrWhiteSpace(CloudOrganizationName))
        {
            SyncStatusText = "Enter the new company's name in Organization first";
            return;
        }

        CompanyProfileService.CreateProfileForNextLaunch(CloudOrganizationName);
        SyncStatusText = $"'{CloudOrganizationName}' company profile created — restart the app to switch to it";
    }

    [RelayCommand]
    public void GoBack()
    {
        if (_navigationStack.Count > 0)
        {
            var previousPage = _navigationStack.Pop();
            CurrentPage = previousPage; // Directly set backing field to avoid pushing to stack again
            // OnPropertyChanged(nameof(CurrentPage)); // Not needed if setting property

            
            // Update UI
            if (previousPage is DashboardViewModel)
            {
               SidebarGridLength = new Avalonia.Controls.GridLength(250);
            }
            // Add other checks if needed for sidebar visibility, though mostly Dashboard controls it
             
            CanGoBack = _navigationStack.Count > 0;
        }
    }

    // Business-driven module gating: defaults to enabled if the key isn't present in EnabledModules,
    // so existing installs and templates that don't mention a module aren't unexpectedly hidden.
    private bool IsModuleEnabled(string key) =>
        !_settingsService.CurrentSettings.EnabledModules.TryGetValue(key, out var enabled) || enabled;

    private bool HasRolePermission(string permission) =>
        UserSession.HasPermission(permission);

    // Permission flags for UI (license + role)
    public bool CanAccessPOS => _licenseService.CanAccessPOS() && IsModuleEnabled("POS") && HasRolePermission(RolePermissions.AccessPOS);
    public bool CanAccessReports => _licenseService.CanAccessAdvancedReports() && HasRolePermission(RolePermissions.ViewReports);
    public bool CanAccessInventory => HasRolePermission(RolePermissions.ManageInventory) || HasRolePermission(RolePermissions.ViewInventory);
    public bool CanAccessSuppliers => _licenseService.CanAccessSupplierManagement() && HasRolePermission(RolePermissions.ManageSuppliers);
    public bool CanAccessEmployees => _licenseService.CanAccessStaffManagement() && HasRolePermission(RolePermissions.ManageEmployees);
    public bool CanAccessAttendance => CanAccessEmployees;
    public bool CanAccessLeaveRequests => CanAccessEmployees;
    public bool CanAccessCustomers => HasRolePermission(RolePermissions.ManageCustomers) || HasRolePermission(RolePermissions.ViewInventory);
    public bool CanAccessPurchaseOrders => _licenseService.CanAccessPurchaseOrders() && HasRolePermission(RolePermissions.ManagePurchasing);
    public bool CanAccessAccountsPayable => CanAccessPurchaseOrders;
    public bool CanAccessForecasting => _licenseService.CanAccessForecasting() && HasRolePermission(RolePermissions.ManagePurchasing);
    public bool CanAccessExpiry => _licenseService.CanAccessExpiryTracking() && IsModuleEnabled("Expiry") && HasRolePermission(RolePermissions.ManageInventory);
    public bool CanAccessLocations => _licenseService.CanAccessMultiLocation() && IsModuleEnabled("MultiLocation") && HasRolePermission(RolePermissions.ManageInventory);
    public bool CanAccessReturns => _licenseService.CanAccessReturns() && HasRolePermission(RolePermissions.ProcessReturns);
    public bool CanAccessExpenses => HasRolePermission(RolePermissions.ManageExpenses);
    public bool CanAccessDamageWriteOff => HasRolePermission(RolePermissions.ManageInventory);
    public bool CanAccessBundles => _licenseService.CanAccessKitting() && IsModuleEnabled("BOM") && HasRolePermission(RolePermissions.ManageInventory);
    public bool CanAccessAudit => _licenseService.CanAccessAuditTrail() && HasRolePermission(RolePermissions.ViewAudit);
    public bool CanAccessAdvancedAnalytics => _licenseService.CanAccessAdvancedAnalytics() && HasRolePermission(RolePermissions.ViewReports);
    public bool CanAccessAnalytics => _licenseService.CanAccessAnalytics() && HasRolePermission(RolePermissions.ViewReports);
    public bool CanAccessManufacturing => IsModuleEnabled("Manufacturing") && HasRolePermission(RolePermissions.ManageManufacturing);
    public bool CanAccessCloudSync => _licenseService.CanAccessCloudSync() && HasRolePermission(RolePermissions.ManageSettings);
    public bool CanManageUsers => HasRolePermission(RolePermissions.ManageUsers);
    public bool CanAccessSettings => HasRolePermission(RolePermissions.ManageSettings);
    public bool CanAccessEnterprise => HasRolePermission(RolePermissions.ManageSettings) || HasRolePermission(RolePermissions.ViewReports);

    [ObservableProperty]
    private string _syncStatusText = "Cloud sync unavailable";

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private string _lastSyncDisplay = "Never";

    [ObservableProperty]
    private bool _isCloudConnected;

    [ObservableProperty]
    private string _cloudEmail = string.Empty;

    [ObservableProperty]
    private string _cloudPassword = string.Empty;

    [ObservableProperty]
    private string _cloudOrganizationName = "My Organization";

    private void NavigateTo(ViewModelBase newPage)
    {
        if (CurrentPage != null)
        {
            _navigationStack.Push(CurrentPage);
        }
        CurrentPage = newPage;
        CanGoBack = _navigationStack.Count > 0;
    }

    // --- Quick Navigation search: find any page or buried settings/report screen by name ---

    [ObservableProperty] private string _navSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<NavigationSearchItem> _matchedNavItems = new();

    private List<NavigationSearchItem>? _navigationIndex;

    private List<NavigationSearchItem> NavigationIndex => _navigationIndex ??= BuildNavigationIndex();

    private List<NavigationSearchItem> BuildNavigationIndex()
    {
        var items = new List<NavigationSearchItem>
        {
            new("Dashboard", "Dashboard", GoToDashboard),
            new("Point of Sale", "POS", GoToPOS),
            new("Inventory", "Inventory", GoToInventory),
            new("Manufacturing", "Manufacturing", GoToManufacturing),
            new("Request for Quotation (RFQ)", "Inventory > RFQ", GoToRfq),
            new("Purchase Orders", "Inventory > Purchase Orders", GoToPurchaseOrders),
            new("Sales Quotations", "Sales > Quotations", GoToSalesQuotations),
            new("Sales Orders", "Sales > Orders", GoToSalesOrders),
            new("Customers", "Customers", GoToCustomers),
            new("Suppliers", "Suppliers", GoToSuppliers),
            new("Accounts Payable", "Money Owed > Accounts Payable", GoToAccountsPayable),
            new("Staff", "Team > Staff", GoToEmployees),
            new("Attendance", "Team > Attendance & Leave", GoToAttendance),
            new("Leave Requests", "Team > Attendance & Leave", GoToLeaveRequests),
            new("Reports", "Reports", GoToReports),
            new("Analytics", "Analytics", GoToAnalytics),
            new("Advanced Analytics", "Analytics > Advanced", GoToAdvancedAnalytics),
            new("Forecasting", "Inventory > Forecasting", GoToForecasting),
            new("Reorder Dashboard", "Inventory > Reorder Dashboard", GoToReorderDashboard),
            new("Cycle Count", "Inventory > Cycle Count", GoToCycleCount),
            new("Expiry Dashboard", "Inventory > Expiry Dashboard", GoToExpiryDashboard),
            new("Locations", "Inventory > Locations", GoToLocations),
            new("Stock Transfer", "Inventory > Stock Transfer", GoToStockTransfer),
            new("Returns", "Returns", GoToReturns),
            new("Damage & Loss Write-off", "Inventory > Damage & Loss Write-off", GoToDamageWriteOff),
            new("Bundles", "Inventory > Bundles", GoToBundles),
            new("Expenses", "Expenses", GoToExpenses),
            new("Users", "Users", GoToUsers),
            new("License", "License", GoToLicense),
            new("Audit Trail", "Audit Trail", GoToAuditTrail),
            new("Enterprise", "Enterprise", GoToEnterprise),

            new("General Settings", "Settings > General", () => GoToSettingsTab("General", null)),
            new("Taxes", "Settings > Taxes", () => GoToSettingsTab("Taxes", null)),
            new("Chart of Accounts", "Settings > Accounting > Chart of Accounts", () => GoToSettingsTab("Accounting", "Chart of Accounts")),
            new("Journals", "Settings > Accounting > Journals", () => GoToSettingsTab("Accounting", "Journals")),
            new("Accounting Reports", "Settings > Accounting > Accounting Reports", () => GoToSettingsTab("Accounting", "Accounting Reports")),
            new("Payments Setup", "Settings > Payments", () => GoToSettingsTab("Payments", null)),
            new("Financial Settings", "Settings > Financial", () => GoToSettingsTab("Financial", null)),
            new("Business Setup", "Settings > Business Setup", () => GoToSettingsTab("BusinessSetup", null)),
            new("Custom Fields", "Settings > Custom Fields", () => GoToSettingsTab("CustomFields", null)),
            new("Terminology", "Settings > Terminology", () => GoToSettingsTab("Terminology", null)),
            new("Modules", "Settings > Modules", () => GoToSettingsTab("Modules", null)),
        };

        foreach (var report in ReportsViewModel.AllReports)
        {
            var key = report.Key;
            items.Add(new NavigationSearchItem(report.Title, $"Reports > {report.Category} > {report.Title}", () => GoToReport(key)));
        }

        return items;
    }

    partial void OnNavSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            MatchedNavItems = new ObservableCollection<NavigationSearchItem>();
            return;
        }

        var query = value.Trim();
        var matches = NavigationIndex
            .Where(i => i.MatchesQuery(query))
            .Take(30)
            .ToList();
        MatchedNavItems = new ObservableCollection<NavigationSearchItem>(matches);
    }

    [RelayCommand]
    private void GoToNavItem(NavigationSearchItem? item)
    {
        if (item == null) return;
        item.Navigate();
        NavSearchText = string.Empty;
        MatchedNavItems = new ObservableCollection<NavigationSearchItem>();
    }

    [RelayCommand]
    public void SwitchUser()
    {
        ShowLoginScreen();
    }

    [RelayCommand]
    public void Logout()
    {
        _navigationStack.Clear(); // Clear history on logout
        CanGoBack = false;

        // "Exit" Application
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    public void ChangeLanguage(string code)
    {
        Language.SetLanguage(code);
    }
    
    [RelayCommand]
    public void GoToDashboard()
    {
        NavigateTo(new DashboardViewModel(_inventoryService, _licenseService, Language, _settingsService, _dailyBriefingService, _salesOrderService, GoToInventory, GoToReports, GoToPOS));
        SidebarGridLength = new Avalonia.Controls.GridLength(250);
    }

    [RelayCommand]
    public void GoToInventory() => NavigateTo(new InventoryViewModel(_inventoryService, _licenseService, _settingsService, Language, _taxService, _accountService, GoToRfq, GoToPurchaseOrders, GoToSuppliers, GoToSalesQuotations, GoToSalesOrders, GoToCustomers, GoToCycleCount, GoToReorderDashboard, GoToForecasting, GoToLocations, CustomFieldService, _barcodeService, GoToDamageWriteOff, GoToPurchaseOrderDetails, GoToSalesOrderDetails, () => GoToReport("stock-status"), () => GoToReport("stock-history")));

    [RelayCommand]
    public void GoToManufacturing() => NavigateTo(new ManufacturingViewModel(_manufacturingService, _inventoryService, Language));

    [RelayCommand]
    public void GoToRfq()
    {
        if (!_licenseService.CanAccessPurchaseOrders())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new RfqViewModel(_purchaseOrderService, _supplierService, _inventoryService, _taxService, _settingsService, Language));
    }

    [RelayCommand]
    public void GoToPurchaseOrders()
    {
        GoToPurchaseOrderDetails(null);
    }

    public void GoToPurchaseOrderDetails(int? purchaseOrderId)
    {
        if (!_licenseService.CanAccessPurchaseOrders())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new PurchaseOrdersViewModel(_purchaseOrderService, _supplierService, _inventoryService, _taxService, _settingsService, _returnsService, _paymentService, _currencyService, Language, purchaseOrderId));
    }

    [RelayCommand]
    public void GoToSalesQuotations()
    {
        NavigateTo(new SalesViewModel(_salesOrderService, _customerService, _inventoryService, _taxService, _settingsService, _returnsService, _paymentService, _currencyService, Language, initialTab: 0, _documentAttachmentService, _recurringInvoiceService));
    }

    [RelayCommand]
    public void GoToSalesOrders()
    {
        GoToSalesOrderDetails(null);
    }

    public void GoToSalesOrderDetails(int? salesOrderId)
    {
        NavigateTo(new SalesViewModel(_salesOrderService, _customerService, _inventoryService, _taxService, _settingsService, _returnsService, _paymentService, _currencyService, Language, initialTab: 1, _documentAttachmentService, _recurringInvoiceService, salesOrderId));
    }

    [RelayCommand]
    public void GoToCustomers()
    {
        NavigateTo(new CustomersViewModel(_customerService, Language, _documentAttachmentService, _salesOrderService));
    }

    [RelayCommand]
    public void GoToReports()
    {
        GoToReport(null);
    }

    public void GoToReport(string? reportKey)
    {
        if (!_licenseService.CanAccessAdvancedReports())
        {
            // Redirect to License/Upgrade page if not premium
            GoToLicense();
            return;
        }
        NavigateTo(new ReportsViewModel(_inventoryService, _licenseService, _settingsService, Language, _accountingReportService, _accountService, _agingReportService, _vatExportService, _budgetReportService, _paymentService, _advancedAnalyticsService, _monthCloseService, _locationService, GoToPurchaseOrderDetails, GoToSalesOrderDetails, reportKey));
    }

    [RelayCommand]
    public void GoToPOS()
    {
        if (!_licenseService.CanAccessPOS())
        {
            // Redirect to License/Upgrade page if not premium
            GoToLicense();
            return;
        }
        NavigateTo(new POSViewModel(_inventoryService, _licenseService, _receiptService, _settingsService, Language, _salesOrderService, _customerService, _journalService, _taxService, _barcodeService, _currencyService, _returnsService, _posSessionService, _auditService));
    }

    [RelayCommand]
    public void GoToAnalytics()
    {
         if (!_licenseService.CanAccessAnalytics())
        {
             // Analytics is a premium feature
            GoToLicense();
            return;
        }
        NavigateTo(new AnalyticsViewModel(_analyticsService, Language));
    }

    [RelayCommand]
    public void GoToUsers()
    {
        if (CanManageUsers)
        {
            NavigateTo(new UsersViewModel(_userService));
        }
    }

    [RelayCommand]
    public void GoToSettings()
    {
        GoToSettingsTab(null, null);
    }

    public void GoToSettingsTab(string? tab, string? accountingSubTab)
    {
        if (!CanAccessSettings) return;
        NavigateTo(new SettingsViewModel(_settingsService, Language, _taxService, _accountService, _journalService, _accountingReportService, _paymentService, _customFieldService, _currencyService, _budgetReportService, RunSetupWizardFromSettings, RefreshModuleGatedAccessProperties, _notificationService, tab, accountingSubTab));
    }

    [RelayCommand]
    public void GoToSuppliers()
    {
        if (!_licenseService.CanAccessSupplierManagement())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new SuppliersViewModel(_supplierService, _inventoryService));
    }

    [RelayCommand]
    public void GoToAccountsPayable()
    {
        if (!_licenseService.CanAccessPurchaseOrders())
        {
            GoToLicense();
            return;
        }

        if (!HasRolePermission(RolePermissions.ManagePurchasing)) return;

        NavigateTo(new AccountsPayableViewModel(
            _agingReportService, _purchaseOrderService, _inventoryService, _taxService, _paymentService,
            GoToPurchaseOrderDetails));
    }

    [RelayCommand]
    public void GoToEmployees()
    {
        if (!_licenseService.CanAccessStaffManagement())
        {
            GoToLicense();
            return;
        }

        if (!HasRolePermission(RolePermissions.ManageEmployees)) return;

        NavigateTo(new EmployeesViewModel(_employeeService, _locationService));
    }

    [RelayCommand]
    public void GoToAttendance() => OpenAttendanceHub("Attendance");

    [RelayCommand]
    public void GoToLeaveRequests() => OpenAttendanceHub("Leave");

    private void OpenAttendanceHub(string tab)
    {
        if (!_licenseService.CanAccessStaffManagement())
        {
            GoToLicense();
            return;
        }

        if (!HasRolePermission(RolePermissions.ManageEmployees)) return;

        // Attendance and Leave Requests share one workspace; `tab` picks which one opens first.
        if (CurrentPage is AttendanceHubViewModel hub)
        {
            hub.ActiveTab = tab == "Leave" ? "Leave" : "Attendance";
            return;
        }

        NavigateTo(new AttendanceHubViewModel(
            _attendanceService, _leaveService, _employeeService, _locationService, tab));
    }



    [RelayCommand]
    public void GoToForecasting()
    {
        if (!_licenseService.CanAccessForecasting())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new ForecastingViewModel(_forecastingService));
    }

    [RelayCommand]
    public void GoToReorderDashboard()
    {
        if (!_licenseService.CanAccessForecasting())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new ReorderDashboardViewModel(_forecastingService, _purchaseOrderService));
    }

    [RelayCommand]
    public void GoToCycleCount()
    {
        if (!_licenseService.CanAccessMultiLocation())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new CycleCountViewModel(_cycleCountService, _locationService, _inventoryService));
    }

    [RelayCommand]
    public void GoToExpiryDashboard()
    {
        if (!_licenseService.CanAccessExpiryTracking())
        {
            GoToLicense();
            return;
        }

        NavigateTo(new ExpiryDashboardViewModel(_expiryService));
    }

    [RelayCommand]
    public void GoToLocations()
    {
        if (!_licenseService.CanAccessMultiLocation())
        {
            GoToLicense();
            return;
        }
        NavigateTo(new LocationsViewModel(_locationService));
    }

    [RelayCommand]
    public void GoToStockTransfer()
    {
        if (!_licenseService.CanAccessMultiLocation())
        {
            GoToLicense();
            return;
        }
        NavigateTo(new StockTransferViewModel(_locationService, _inventoryService));
    }

    [RelayCommand]
    public void GoToReturns()
    {
        if (!_licenseService.CanAccessReturns())
        {
            GoToLicense();
            return;
        }
        NavigateTo(new ReturnsViewModel(_returnsService, _inventoryService, _salesOrderService, _purchaseOrderService));
    }

    [RelayCommand]
    public void GoToExpenses()
    {
        if (!CanAccessExpenses)
        {
            return;
        }
        NavigateTo(new ExpenseViewModel(_expenseService));
    }

    [RelayCommand]
    public void GoToDamageWriteOff()
    {
        if (!CanAccessDamageWriteOff)
        {
            return;
        }
        NavigateTo(new DamageWriteOffViewModel(_damageWriteOffService, _inventoryService));
    }

    [RelayCommand]
    public void GoToAdvancedAnalytics()
    {
        if (!_licenseService.CanAccessAdvancedAnalytics())
        {
            GoToLicense();
            return;
        }
        NavigateTo(new AdvancedAnalyticsViewModel(_advancedAnalyticsService));
    }

    [RelayCommand]
    public void GoToBundles()
    {
        if (!_licenseService.CanAccessKitting())
        {
            GoToLicense();
            return;
        }
        NavigateTo(new BundleViewModel(_bundleService, _inventoryService));
    }

    [RelayCommand]
    public void GoToAuditTrail()
    {
        if (!_licenseService.CanAccessAuditTrail())
        {
            GoToLicense();
            return;
        }
        NavigateTo(new AuditTrailViewModel(_auditService));
    }

    [RelayCommand]
    public void GoToEnterprise()
    {
        if (!CanAccessEnterprise)
        {
            return;
        }
        NavigateTo(new EnterpriseViewModel(
            _companyBranchService, _workflowApprovalService, _mrpPlanningService,
            _crmPipelineService, _mobileFieldService, _securityComplianceService,
            _inventoryService));
    }

    private void OnActivationSuccess()
    {
        ShowLoginScreen();
    }

    [RelayCommand]
    public void GoToLicense() => NavigateTo(new LicenseViewModel(_licenseService, _hardwareIdService, OnActivationSuccess));

    [RelayCommand]
    public Task CheckForUpdates() => CheckForUpdatesInternal(true);

    private async Task CheckForUpdatesInternal(bool userInitiated)
    {
        if (UpdateStatusText == "Checking..." || UpdateStatusText == "Downloading...") return; // Prevent spam

        if (userInitiated) UpdateStatusText = "Checking...";
        
        var result = await _updateService.CheckForUpdatesAsync();
        
        if (result.Success && result.Version != null)
        {
            UpdateVersion = result.Version;
            ReleaseNotesUrl = result.ReleaseNotesUrl ?? "";
            IsUpdateAvailable = true;
            UpdateStatusText = "Update Found!";
        }
        else
        {
            IsUpdateAvailable = false;
            if (userInitiated)
            {
                if (result.IsDevMode)
                {
                     UpdateStatusText = "Dev Mode (Skipped)";
                }
                else
                {
                    UpdateStatusText = "Up to Date";
                }
                
                // Reset text after 3 seconds
                await Task.Delay(3000);
                UpdateStatusText = "Check Updates";
            }
        }
    }

    [RelayCommand]
    public void OpenReleaseNotes()
    {
        if (!string.IsNullOrEmpty(ReleaseNotesUrl))
        {
            try
            {
               // Cross-platform open url
               System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
               {
                   FileName = ReleaseNotesUrl,
                   UseShellExecute = true
               });
            }
            catch { /* handle error */ }
        }
    }

    [RelayCommand]
    public async Task InstallUpdate()
    {
        UpdateStatusText = "Downloading...";
        await _updateService.DownloadAndRestartAsync();
        UpdateStatusText = "Check Updates";
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        var app = Avalonia.Application.Current;
        if (app is not null)
        {
            var currentTheme = app.RequestedThemeVariant;
            app.RequestedThemeVariant = currentTheme == Avalonia.Styling.ThemeVariant.Dark 
                ? Avalonia.Styling.ThemeVariant.Light
                : Avalonia.Styling.ThemeVariant.Dark;
        }
    }
}

/// <summary>One destination in the global "Quick Navigation" search - a top-level page, or a
/// specific buried tab/report the user would otherwise have to know the click-path to reach.</summary>
public class NavigationSearchItem
{
    public NavigationSearchItem(string title, string path, System.Action navigate)
    {
        Title = title;
        Path = path;
        Navigate = navigate;
    }

    public string Title { get; }
    /// <summary>Breadcrumb shown under the title, e.g. "Settings &gt; Accounting &gt; Journals".</summary>
    public string Path { get; }
    public System.Action Navigate { get; }

    public bool MatchesQuery(string query) =>
        Title.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
        Path.Contains(query, System.StringComparison.OrdinalIgnoreCase);
}
