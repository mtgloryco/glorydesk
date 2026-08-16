using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.UI.ViewModels;
using InventoryManagementSystem.UI.Views;

namespace InventoryManagementSystem;

public partial class App : Application
{
    public static (
        InventoryService inventory, UserService user, LicenseService license, HardwareIdService hardware,
        AnalyticsService analytics, ReceiptService receipt, LanguageService language, UpdateService update,
        SettingsService settings, SupplierService supplier, PurchaseOrderService purchaseOrder, SalesOrderService salesOrder,
        ForecastingService forecasting, ExpiryService expiry, LocationService location, ReturnsService returns,
        AdvancedAnalyticsService advancedAnalytics, BundleService bundle, AuditService audit,
        ReportingService reporting, CloudSyncService cloudSync, DailyBriefingService briefing,
        TaxService tax, AccountService account, JournalService journal, AccountingReportService accountingReport,
        ManufacturingService manufacturing, PaymentService payment, IndustryTemplateService industryTemplateService,
        CustomFieldService customFieldService, CustomerService customerService,
        BarcodeService barcodeService, AgingReportService agingReportService,
        VatExportService vatExportService, BudgetReportService budgetReportService,
        CurrencyService currencyService, CycleCountService cycleCountService,
        IntegrationWebhookService integrationWebhookService, NotificationService notificationService,
        MonthCloseService monthCloseService, CompanyBranchService companyBranchService,
        WorkflowApprovalService workflowApprovalService, MrpPlanningService mrpPlanningService,
        CrmPipelineService crmPipelineService, MobileFieldService mobileFieldService,
        SecurityComplianceService securityComplianceService,
        DocumentAttachmentService documentAttachmentService,
        RecurringInvoiceService recurringInvoiceService,
        ExpenseService expenseService, DamageWriteOffService damageWriteOffService,
        PosSessionService posSessionService, EmployeeService employeeService,
        AttendanceService attendanceService, LeaveService leaveService)? PreInitializedServices { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Shared Service Initialization
        var services = PreInitializedServices ?? InitializeServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    services.inventory, services.user, services.license, services.hardware,
                    services.analytics, services.receipt, services.language, services.update,
                    services.settings, services.supplier, services.purchaseOrder, services.salesOrder, services.forecasting,
                    services.expiry, services.location, services.returns, services.advancedAnalytics,
                    services.bundle, services.audit, services.reporting, services.cloudSync,
                    services.briefing, services.tax, services.account, services.journal, services.accountingReport,
                    services.manufacturing, services.payment, services.industryTemplateService,
                    services.customFieldService, services.customerService, services.barcodeService, services.agingReportService,
                    services.vatExportService, services.budgetReportService, services.currencyService, services.cycleCountService,
                    services.integrationWebhookService, services.notificationService, services.monthCloseService,
                    services.companyBranchService, services.workflowApprovalService, services.mrpPlanningService,
                    services.crmPipelineService, services.mobileFieldService, services.securityComplianceService,
                    services.documentAttachmentService, services.recurringInvoiceService,
                    services.expenseService, services.damageWriteOffService, services.posSessionService,
                    services.employeeService, services.attendanceService, services.leaveService),
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel(
                    services.inventory, services.user, services.license, services.hardware,
                    services.analytics, services.receipt, services.language, services.update,
                    services.settings, services.supplier, services.purchaseOrder, services.salesOrder, services.forecasting,
                    services.expiry, services.location, services.returns, services.advancedAnalytics,
                    services.bundle, services.audit, services.reporting, services.cloudSync,
                    services.briefing, services.tax, services.account, services.journal, services.accountingReport,
                    services.manufacturing, services.payment, services.industryTemplateService,
                    services.customFieldService, services.customerService, services.barcodeService, services.agingReportService,
                    services.vatExportService, services.budgetReportService, services.currencyService, services.cycleCountService,
                    services.integrationWebhookService, services.notificationService, services.monthCloseService,
                    services.companyBranchService, services.workflowApprovalService, services.mrpPlanningService,
                    services.crmPipelineService, services.mobileFieldService, services.securityComplianceService,
                    services.documentAttachmentService, services.recurringInvoiceService,
                    services.expenseService, services.damageWriteOffService, services.posSessionService,
                    services.employeeService, services.attendanceService, services.leaveService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private (
        InventoryService inventory, UserService user, LicenseService license, HardwareIdService hardware,
        AnalyticsService analytics, ReceiptService receipt, LanguageService language, UpdateService update,
        SettingsService settings, SupplierService supplier, PurchaseOrderService purchaseOrder, SalesOrderService salesOrder,
        ForecastingService forecasting, ExpiryService expiry, LocationService location, ReturnsService returns,
        AdvancedAnalyticsService advancedAnalytics, BundleService bundle, AuditService audit,
        ReportingService reporting, CloudSyncService cloudSync, DailyBriefingService briefing,
        TaxService tax, AccountService account, JournalService journal, AccountingReportService accountingReport,
        ManufacturingService manufacturing, PaymentService payment, IndustryTemplateService industryTemplateService,
        CustomFieldService customFieldService, CustomerService customerService,
        BarcodeService barcodeService, AgingReportService agingReportService,
        VatExportService vatExportService, BudgetReportService budgetReportService,
        CurrencyService currencyService, CycleCountService cycleCountService,
        IntegrationWebhookService integrationWebhookService, NotificationService notificationService,
        MonthCloseService monthCloseService, CompanyBranchService companyBranchService,
        WorkflowApprovalService workflowApprovalService, MrpPlanningService mrpPlanningService,
        CrmPipelineService crmPipelineService, MobileFieldService mobileFieldService,
        SecurityComplianceService securityComplianceService,
        DocumentAttachmentService documentAttachmentService,
        RecurringInvoiceService recurringInvoiceService,
        ExpenseService expenseService, DamageWriteOffService damageWriteOffService,
        PosSessionService posSessionService, EmployeeService employeeService,
        AttendanceService attendanceService, LeaveService leaveService) InitializeServices()
    {
        // Initialize Database
        var dbService = new DatabaseService();
        var auditService = new AuditService(dbService);
        var userService = new UserService(dbService, auditService);
        var hardwareService = new HardwareIdService();
        var cryptoService = new LicenseCryptoService();
        var licenseService = new LicenseService(dbService, hardwareService, cryptoService);
        var settingsService = new SettingsService();
        var inventoryService = new InventoryService(dbService, licenseService, auditService, settingsService);
        var analyticsService = new AnalyticsService(dbService);
        var languageService = new LanguageService();
        var updateService = new UpdateService();
        var receiptService = new ReceiptService(settingsService);
        var supplierService = new SupplierService(dbService);
        var purchaseOrderService = new PurchaseOrderService(dbService, inventoryService, auditService, settingsService);
        var salesOrderService = new SalesOrderService(dbService, inventoryService, auditService, settingsService);
        var forecastingService = new ForecastingService(dbService);
        var expiryService = new ExpiryService(dbService);
        
        var locationService = new LocationService(dbService);
        var returnsService = new ReturnsService(dbService, auditService);
        var advancedAnalyticsService = new AdvancedAnalyticsService(dbService);
        var bundleService = new BundleService(dbService);
        var reportingService = new ReportingService(dbService, settingsService);
        var cloudApiClient = new CloudSyncApiClient();
        var cloudSyncService = new CloudSyncService(dbService, cloudApiClient, auditService);
        var dailyBriefingService = new DailyBriefingService(dbService);
        var taxService = new TaxService(dbService);
        var accountService = new AccountService(dbService);
        var journalService = new JournalService(dbService);
        var posSessionService = new PosSessionService(dbService);
        var accountingReportService = new AccountingReportService(dbService);
        var manufacturingService = new ManufacturingService(dbService, auditService);
        var customFieldService = new CustomFieldService(dbService);
        var customerService = new CustomerService(dbService);
        var industryTemplateService = new IndustryTemplateService(dbService);
        var barcodeService = new BarcodeService(dbService);
        var agingReportService = new AgingReportService(dbService);
        var vatExportService = new VatExportService(dbService);
        var budgetReportService = new BudgetReportService(dbService, auditService);
        var currencyService = new CurrencyService(dbService, auditService);
        var paymentService = new PaymentService(dbService, auditService, currencyService, settingsService);
        var cycleCountService = new CycleCountService(dbService, auditService);
        var integrationWebhookService = new IntegrationWebhookService(dbService, auditService);
        var notificationService = new NotificationService(dbService, paymentService, auditService, settingsService);
        var monthCloseService = new MonthCloseService(dbService, accountingReportService, paymentService, auditService);
        var companyBranchService = new CompanyBranchService(dbService, auditService);
        var workflowApprovalService = new WorkflowApprovalService(dbService, purchaseOrderService, auditService);
        var mrpPlanningService = new MrpPlanningService(dbService, auditService);
        var crmPipelineService = new CrmPipelineService(dbService, salesOrderService, auditService);
        var mobileFieldService = new MobileFieldService(dbService, auditService);
        var securityComplianceService = new SecurityComplianceService(dbService, auditService);
        var documentAttachmentService = new DocumentAttachmentService(dbService, auditService);
        var recurringInvoiceService = new RecurringInvoiceService(dbService, salesOrderService, auditService);
        var expenseService = new ExpenseService(dbService, auditService);
        var damageWriteOffService = new DamageWriteOffService(dbService, inventoryService, purchaseOrderService, manufacturingService);
        var employeeService = new EmployeeService(dbService, auditService);
        var attendanceService = new AttendanceService(dbService, auditService);
        var leaveService = new LeaveService(dbService, attendanceService, auditService);

        // Apply plain-English defaults when no custom word labels are saved yet
        var terminology = settingsService.CurrentSettings.TerminologyOverrides;
        if (terminology.Count == 0)
        {
            foreach (var kvp in PlainLanguagePresets.SimpleEnglish)
            {
                terminology[kvp.Key] = kvp.Value;
            }
            settingsService.SaveSettings();
        }
        languageService.SetTerminologyOverrides(terminology);

        // Initialize services on a background thread to prevent UI thread deadlock
        Task.Run(async () =>
        {
            await dbService.InitializeAsync(settingsService.CurrentSettings.CurrencySymbol);
            await userService.InitializeAsync();
            await licenseService.InitializeAsync();
        }).Wait();

        return (
            inventoryService, userService, licenseService, hardwareService,
            analyticsService, receiptService, languageService, updateService,
            settingsService, supplierService, purchaseOrderService, salesOrderService, forecastingService,
            expiryService, locationService, returnsService, advancedAnalyticsService,
            bundleService, auditService, reportingService, cloudSyncService, dailyBriefingService,
            taxService, accountService, journalService, accountingReportService, manufacturingService, paymentService,
            industryTemplateService, customFieldService, customerService, barcodeService, agingReportService,
            vatExportService, budgetReportService, currencyService, cycleCountService,
            integrationWebhookService, notificationService, monthCloseService,
            companyBranchService, workflowApprovalService, mrpPlanningService,
            crmPipelineService, mobileFieldService, securityComplianceService,
            documentAttachmentService, recurringInvoiceService,
            expenseService, damageWriteOffService, posSessionService, employeeService,
            attendanceService, leaveService);
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}