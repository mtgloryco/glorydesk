using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    /// <summary>
    /// One "Attendance &amp; Leave" workspace that hosts the daily attendance sheet and the
    /// leave-request queue as two tabs, so they live in a single module instead of two
    /// separate sidebar entries.
    /// </summary>
    public partial class AttendanceHubViewModel : ViewModelBase
    {
        public AttendanceViewModel Attendance { get; }
        public LeaveRequestsViewModel Leave { get; }

        [ObservableProperty]
        private string _activeTab = "Attendance"; // "Attendance" | "Leave"

        public bool IsAttendanceTab => ActiveTab == "Attendance";
        public bool IsLeaveTab => ActiveTab == "Leave";

        public ViewModelBase CurrentTab => IsLeaveTab ? Leave : Attendance;

        public AttendanceHubViewModel(
            AttendanceService attendanceService,
            LeaveService leaveService,
            EmployeeService employeeService,
            LocationService locationService,
            string initialTab = "Attendance")
        {
            Attendance = new AttendanceViewModel(attendanceService, employeeService, locationService);
            Leave = new LeaveRequestsViewModel(leaveService, employeeService);
            ActiveTab = initialTab == "Leave" ? "Leave" : "Attendance";
        }

        [RelayCommand]
        private void SwitchTab(string tab)
        {
            ActiveTab = tab == "Leave" ? "Leave" : "Attendance";
        }

        partial void OnActiveTabChanged(string value)
        {
            OnPropertyChanged(nameof(IsAttendanceTab));
            OnPropertyChanged(nameof(IsLeaveTab));
            OnPropertyChanged(nameof(CurrentTab));
        }
    }
}
