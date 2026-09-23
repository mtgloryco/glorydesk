using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class Phase6Tests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private AuditService _audit = null!;
    private EmployeeService _employeeService = null!;
    private LocationService _locationService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _audit = new AuditService(_db);
        _employeeService = new EmployeeService(_db, _audit);
        _locationService = new LocationService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task AddEmployee_PersistsAndAppearsInActiveList()
    {
        var location = new Location { Name = "Main Store", Type = "Store" };
        await _locationService.AddLocationAsync(location);

        var employee = new Employee
        {
            Name = "Aline Uwase",
            Position = "Cashier",
            Department = "Sales",
            LocationId = location.Id,
            EmploymentType = "Full-time",
            PayFrequency = "Monthly",
            BaseSalary = 150000m
        };

        await _employeeService.AddEmployeeAsync(employee, "tester");

        var all = await _employeeService.GetAllEmployeesAsync();
        Assert.Single(all);
        Assert.Equal("Aline Uwase", all[0].Name);
        Assert.True(all[0].IsActive);
        Assert.Equal("Active", all[0].Status);
    }

    [Fact]
    public async Task UpdateEmployee_ChangesPersistAndAreAudited()
    {
        var employee = new Employee { Name = "Jean Bosco", Position = "Stock Clerk", BaseSalary = 100000m };
        await _employeeService.AddEmployeeAsync(employee, "tester");

        employee.Position = "Store Supervisor";
        employee.BaseSalary = 180000m;
        await _employeeService.UpdateEmployeeAsync(employee, "tester");

        var reloaded = await _employeeService.GetEmployeeByIdAsync(employee.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Store Supervisor", reloaded!.Position);
        Assert.Equal(180000m, reloaded.BaseSalary);

        var logs = await _db.Connection.Table<AuditLog>().ToListAsync();
        Assert.Contains(logs, l => l.EntityType == "Employee" && l.Action == "Update");
    }

    [Fact]
    public async Task DeactivateEmployee_RemovesFromActiveListButKeepsRecord()
    {
        var employee = new Employee { Name = "Eric Habimana", Position = "Driver" };
        await _employeeService.AddEmployeeAsync(employee, "tester");

        await _employeeService.DeactivateEmployeeAsync(employee.Id, "tester");

        var active = await _employeeService.GetAllEmployeesAsync();
        Assert.Empty(active);

        var all = await _employeeService.GetAllEmployeesAsync(includeInactive: true);
        Assert.Single(all);
        Assert.False(all[0].IsActive);
        Assert.Equal("Terminated", all[0].Status);
        Assert.NotNull(all[0].TerminationDate);
    }

    [Fact]
    public async Task ReactivateEmployee_ByEditingStatusBackToActive_RestoresActiveList()
    {
        var employee = new Employee { Name = "Solange Mukamana", Position = "Cashier" };
        await _employeeService.AddEmployeeAsync(employee, "tester");
        await _employeeService.DeactivateEmployeeAsync(employee.Id, "tester");

        Assert.Empty(await _employeeService.GetAllEmployeesAsync());

        var terminated = await _employeeService.GetEmployeeByIdAsync(employee.Id);
        Assert.NotNull(terminated);
        terminated!.Status = "Active";
        await _employeeService.UpdateEmployeeAsync(terminated, "tester");

        var active = await _employeeService.GetAllEmployeesAsync();
        Assert.Single(active);
        Assert.True(active[0].IsActive);
        Assert.Null(active[0].TerminationDate);
    }

    [Fact]
    public async Task GetEmployeesByLocation_OnlyReturnsMatchingActiveStaff()
    {
        var storeA = new Location { Name = "Store A", Type = "Store" };
        await _locationService.AddLocationAsync(storeA);
        var storeB = new Location { Name = "Store B", Type = "Store" };
        await _locationService.AddLocationAsync(storeB);

        await _employeeService.AddEmployeeAsync(new Employee { Name = "Staff A1", LocationId = storeA.Id }, "tester");
        await _employeeService.AddEmployeeAsync(new Employee { Name = "Staff A2", LocationId = storeA.Id }, "tester");
        await _employeeService.AddEmployeeAsync(new Employee { Name = "Staff B1", LocationId = storeB.Id }, "tester");

        var storeAStaff = await _employeeService.GetEmployeesByLocationAsync(storeA.Id);

        Assert.Equal(2, storeAStaff.Count);
        Assert.All(storeAStaff, e => Assert.Equal(storeA.Id, e.LocationId));
    }

    [Fact]
    public void ManageEmployees_IsGrantedToAdminAndManagerOnly()
    {
        Assert.True(RolePermissions.HasPermission("Admin", RolePermissions.ManageEmployees));
        Assert.True(RolePermissions.HasPermission("Manager", RolePermissions.ManageEmployees));
        Assert.False(RolePermissions.HasPermission("Cashier", RolePermissions.ManageEmployees));
        Assert.False(RolePermissions.HasPermission("Staff", RolePermissions.ManageEmployees));
        Assert.False(RolePermissions.HasPermission("Accountant", RolePermissions.ManageEmployees));
    }
}
