using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class AttendanceViewModel : ViewModelBase
    {
        private readonly AttendanceService _attendanceService;
        private readonly EmployeeService _employeeService;
        private readonly LocationService _locationService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<Location> _locations = new();

        [ObservableProperty]
        private Location? _selectedLocation;

        [ObservableProperty]
        private ObservableCollection<AttendanceRowViewModel> _rows = new();

        [ObservableProperty]
        private string _summaryText = string.Empty;

        public ObservableCollection<string> StatusOptions { get; } = new() { "Present", "Absent", "Late", "HalfDay", "OnLeave" };

        public AttendanceViewModel(AttendanceService attendanceService, EmployeeService employeeService, LocationService locationService)
        {
            _attendanceService = attendanceService;
            _employeeService = employeeService;
            _locationService = locationService;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var locations = await _locationService.GetAllLocationsAsync();
            Locations = new ObservableCollection<Location>(locations);
            await LoadRows();
        }

        [RelayCommand]
        private async Task LoadRows()
        {
            var employees = SelectedLocation != null
                ? await _employeeService.GetEmployeesByLocationAsync(SelectedLocation.Id)
                : await _employeeService.GetAllEmployeesAsync();

            var existing = await _attendanceService.GetAttendanceForDateAsync(SelectedDate, SelectedLocation?.Id);
            var byEmployee = existing.ToDictionary(a => a.EmployeeId);

            var rows = employees.Select(e => new AttendanceRowViewModel
            {
                EmployeeId = e.Id,
                EmployeeName = e.Name,
                Status = byEmployee.TryGetValue(e.Id, out var record) ? record.Status : "Present",
                Notes = byEmployee.TryGetValue(e.Id, out var r2) ? r2.Notes : string.Empty
            }).ToList();

            Rows = new ObservableCollection<AttendanceRowViewModel>(rows);
            UpdateSummary();
        }

        [RelayCommand]
        private async Task SaveAttendance()
        {
            var username = UserSession.CurrentUser?.Username ?? "System";
            var locationId = SelectedLocation?.Id ?? 0;

            var records = Rows.Select(row => new AttendanceRecord
            {
                EmployeeId = row.EmployeeId,
                Date = SelectedDate,
                Status = row.Status,
                Notes = row.Notes,
                LocationId = locationId
            });

            await _attendanceService.MarkBulkAttendanceAsync(records, username);
            await LoadRows();
        }

        private void UpdateSummary()
        {
            var present = Rows.Count(r => r.Status == "Present");
            var absent = Rows.Count(r => r.Status == "Absent");
            var late = Rows.Count(r => r.Status == "Late");
            var onLeave = Rows.Count(r => r.Status == "OnLeave");
            SummaryText = $"{Rows.Count} staff — {present} present, {absent} absent, {late} late, {onLeave} on leave";
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            LoadRowsCommand.Execute(null);
        }

        partial void OnSelectedLocationChanged(Location? value)
        {
            LoadRowsCommand.Execute(null);
        }
    }
}
