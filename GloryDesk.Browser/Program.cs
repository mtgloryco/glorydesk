using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Infrastructure;

[assembly: SupportedOSPlatform("browser")]

namespace InventoryManagementSystem;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        // Initialize SQLite Raw provider before database service starts
        SQLitePCL.Batteries.Init();

        // 1. Manually instantiate services (matching App.axaml.cs DI setup)
        var dbService = new DatabaseService(CompanyProfileService.ResolveActiveDatabasePath());
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
        var posSessionService = new PosSessionService(dbService);
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

        // 2. Initialize database, user, and license services asynchronously (non-blocking)
        await dbService.InitializeAsync(settingsService.CurrentSettings.CurrencySymbol);
        await userService.InitializeAsync();
        await licenseService.InitializeAsync();

        // 3. Save references to App class so it skips manual startup initialization
        App.PreInitializedServices = (
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

        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
