using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class Phase1Tests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private BarcodeService _barcodeService = null!;
    private AgingReportService _agingService = null!;
    private ReturnsService _returnsService = null!;
    private AuditService _auditService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _barcodeService = new BarcodeService(_db);
        _agingService = new AgingReportService(_db);
        _auditService = new AuditService(_db);
        _returnsService = new ReturnsService(_db, _auditService);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task BarcodeService_FindsProductByExactSku()
    {
        var product = new Product { Name = "Scan Test", SKU = "BAR-12345", Price = 100, Cost = 50, StockQuantity = 10 };
        await _db.Connection.InsertAsync(product);

        var found = await _barcodeService.FindProductByBarcodeAsync("BAR-12345");
        Assert.NotNull(found);
        Assert.Equal(product.Id, found!.Id);
    }

    [Fact]
    public void RolePermissions_CashierCannotManageUsers()
    {
        Assert.True(RolePermissions.HasPermission("Cashier", RolePermissions.AccessPOS));
        Assert.False(RolePermissions.HasPermission("Cashier", RolePermissions.ManageUsers));
    }

    [Fact]
    public void RolePermissions_ManagerCanAccessReportsButNotUsers()
    {
        Assert.True(RolePermissions.HasPermission("Manager", RolePermissions.ViewReports));
        Assert.False(RolePermissions.HasPermission("Manager", RolePermissions.ManageUsers));
    }

    [Fact]
    public async Task ReturnsService_CreatesCreditNoteOnCustomerReturn()
    {
        var product = new Product { Name = "Return Item", SKU = "RET-1", Price = 200, Cost = 80, StockQuantity = 5 };
        await _db.Connection.InsertAsync(product);

        var ret = new CustomerReturn
        {
            ReturnNumber = "RET-TEST-1",
            ProductId = product.Id,
            Quantity = 1,
            RefundAmount = 200,
            ProcessedByUsername = "admin",
            Condition = "Destroyed",
            Reason = "Damaged"
        };

        await _returnsService.ProcessCustomerReturnAsync(ret);

        var notes = await _returnsService.GetCreditNotesAsync();
        Assert.Single(notes);
        Assert.Equal(200, notes[0].Amount);
    }

    [Fact]
    public async Task AgingReportService_ComputesReceivableBuckets()
    {
        var customer = new Customer { Name = "Credit Customer" };
        await _db.Connection.InsertAsync(customer);

        await _db.Connection.InsertAsync(new SalesOrder
        {
            SONumber = "SO-AGING-1",
            CustomerId = customer.Id,
            Status = "Delivered",
            BillingStatus = "Invoiced",
            OrderDate = DateTime.Today.AddDays(-60),
            PaymentTerms = "30 days",
            TotalAmount = 1000,
            IsPosSale = false
        });

        var lines = await _agingService.GetAccountsReceivableAgingAsync();
        var line = Assert.Single(lines, l => l.DocumentNumber == "SO-AGING-1");
        Assert.True(line.DaysOverdue > 0);
        Assert.Equal(1000, line.OpenBalance);
        Assert.Contains(line.AgingBucket, new[] { "1-30", "31-60", "61-90", "90+" });
    }

    [Fact]
    public void UserAccessService_CustomPermissionsOverrideRole()
    {
        var user = new User
        {
            Role = "Cashier",
            PermissionsJson = UserAccessService.SerializePermissions(new[] { RolePermissions.ViewReports })
        };

        Assert.False(UserAccessService.HasPermission(user, RolePermissions.AccessPOS));
        Assert.True(UserAccessService.HasPermission(user, RolePermissions.ViewReports));
    }
}
