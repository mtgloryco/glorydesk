using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class Phase7Tests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private AuditService _audit = null!;
    private EmployeeService _employeeService = null!;
    private LocationService _locationService = null!;
    private AttendanceService _attendanceService = null!;
    private LeaveService _leaveService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _audit = new AuditService(_db);
        _employeeService = new EmployeeService(_db, _audit);
        _locationService = new LocationService(_db);
        _attendanceService = new AttendanceService(_db, _audit);
        _leaveService = new LeaveService(_db, _attendanceService, _audit);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    private async Task<Employee> AddEmployeeAsync(string name, int locationId = 0)
    {
        var employee = new Employee { Name = name, LocationId = locationId };
        await _employeeService.AddEmployeeAsync(employee, "tester");
        return employee;
    }

    [Fact]
    public async Task LeaveTypes_AreSeededOnFirstRun()
    {
        var types = await _leaveService.GetLeaveTypesAsync();
        Assert.Contains(types, t => t.Name == "Annual");
        Assert.Contains(types, t => t.Name == "Sick");
        Assert.Contains(types, t => t.Name == "Unpaid");
    }

    [Fact]
    public async Task MarkAttendance_ThenRemark_UpdatesInPlaceInsteadOfDuplicating()
    {
        var employee = await AddEmployeeAsync("Aline Uwase");
        var day = new DateTime(2026, 8, 1);

        await _attendanceService.MarkAttendanceAsync(new AttendanceRecord
        {
            EmployeeId = employee.Id,
            Date = day,
            Status = "Present"
        }, "tester");

        await _attendanceService.MarkAttendanceAsync(new AttendanceRecord
        {
            EmployeeId = employee.Id,
            Date = day,
            Status = "Late",
            Notes = "Traffic"
        }, "tester");

        var records = await _attendanceService.GetAttendanceForDateAsync(day);
        Assert.Single(records);
        Assert.Equal("Late", records[0].Status);
        Assert.Equal("Traffic", records[0].Notes);
    }

    [Fact]
    public async Task MonthlySummary_CountsStatusesCorrectly()
    {
        var employee = await AddEmployeeAsync("Jean Bosco");

        await _attendanceService.MarkAttendanceAsync(new AttendanceRecord { EmployeeId = employee.Id, Date = new DateTime(2026, 8, 1), Status = "Present" }, "tester");
        await _attendanceService.MarkAttendanceAsync(new AttendanceRecord { EmployeeId = employee.Id, Date = new DateTime(2026, 8, 2), Status = "Present" }, "tester");
        await _attendanceService.MarkAttendanceAsync(new AttendanceRecord { EmployeeId = employee.Id, Date = new DateTime(2026, 8, 3), Status = "Absent" }, "tester");
        await _attendanceService.MarkAttendanceAsync(new AttendanceRecord { EmployeeId = employee.Id, Date = new DateTime(2026, 8, 4), Status = "Late" }, "tester");

        var summary = await _attendanceService.GetMonthlySummaryAsync(employee.Id, 2026, 8);

        Assert.Equal(4, summary.MarkedDays);
        Assert.Equal(2, summary.PresentDays);
        Assert.Equal(1, summary.AbsentDays);
        Assert.Equal(1, summary.LateDays);
    }

    [Fact]
    public async Task SubmitLeaveRequest_ComputesDaysCountAndDefaultsToPending()
    {
        var employee = await AddEmployeeAsync("Eric Habimana");
        var types = await _leaveService.GetLeaveTypesAsync();
        var annual = types.First(t => t.Name == "Annual");

        var request = await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = annual.Id,
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 14),
            Reason = "Family trip"
        }, "tester");

        Assert.Equal(5, request.DaysCount);
        Assert.Equal("Pending", request.Status);

        var pending = await _leaveService.GetPendingLeaveRequestsAsync();
        Assert.Single(pending);
    }

    [Fact]
    public async Task ApproveLeaveRequest_MarksAttendanceOnLeaveForEveryDay()
    {
        var location = new Location { Name = "Main Store", Type = "Store" };
        await _locationService.AddLocationAsync(location);
        var employee = await AddEmployeeAsync("Solange Mukamana", location.Id);
        var types = await _leaveService.GetLeaveTypesAsync();
        var annual = types.First(t => t.Name == "Annual");

        var request = await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = annual.Id,
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 12)
        }, "tester");

        await _leaveService.ApproveLeaveRequestAsync(request.Id, location.Id, "manager");

        var day1 = await _attendanceService.GetAttendanceForDateAsync(new DateTime(2026, 8, 10));
        var day2 = await _attendanceService.GetAttendanceForDateAsync(new DateTime(2026, 8, 11));
        var day3 = await _attendanceService.GetAttendanceForDateAsync(new DateTime(2026, 8, 12));

        Assert.All(new[] { day1, day2, day3 }, records =>
        {
            Assert.Single(records);
            Assert.Equal("OnLeave", records[0].Status);
        });

        var pending = await _leaveService.GetPendingLeaveRequestsAsync();
        Assert.Empty(pending);
    }

    [Fact]
    public async Task ApproveLeaveRequest_Twice_ThrowsBecauseAlreadyDecided()
    {
        var employee = await AddEmployeeAsync("Divine Iradukunda");
        var types = await _leaveService.GetLeaveTypesAsync();
        var sick = types.First(t => t.Name == "Sick");

        var request = await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = sick.Id,
            StartDate = new DateTime(2026, 8, 5),
            EndDate = new DateTime(2026, 8, 5)
        }, "tester");

        await _leaveService.ApproveLeaveRequestAsync(request.Id, 0, "manager");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _leaveService.ApproveLeaveRequestAsync(request.Id, 0, "manager"));
    }

    [Fact]
    public async Task LeaveBalance_ReducesByApprovedDaysOnly_NotByRejectedOrPending()
    {
        var employee = await AddEmployeeAsync("Patrick Nshimiyimana");
        var types = await _leaveService.GetLeaveTypesAsync();
        var annual = types.First(t => t.Name == "Annual");

        var approved = await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = annual.Id,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 5) // 5 days
        }, "tester");
        await _leaveService.ApproveLeaveRequestAsync(approved.Id, 0, "manager");

        var rejected = await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = annual.Id,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 10) // 10 days, but rejected — should not count
        }, "tester");
        await _leaveService.RejectLeaveRequestAsync(rejected.Id, "manager");

        await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = annual.Id,
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 9, 2) // still pending — should not count
        }, "tester");

        var balance = await _leaveService.GetLeaveBalanceAsync(employee.Id, annual.Id, 2026);

        Assert.Equal(annual.DefaultDaysPerYear - 5, balance);
    }
}
