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
    public partial class LeaveRequestsViewModel : ViewModelBase
    {
        private readonly LeaveService _leaveService;
        private readonly EmployeeService _employeeService;

        [ObservableProperty]
        private ObservableCollection<LeaveRequest> _pendingRequests = new();

        [ObservableProperty]
        private ObservableCollection<LeaveRequest> _recentRequests = new();

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private ObservableCollection<LeaveType> _leaveTypes = new();

        [ObservableProperty]
        private ObservableCollection<LeaveBalanceRow> _selectedEmployeeBalances = new();

        [ObservableProperty]
        private Employee? _formEmployee;

        [ObservableProperty]
        private LeaveType? _formLeaveType;

        [ObservableProperty]
        private DateTime _formStartDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _formEndDate = DateTime.Today;

        [ObservableProperty]
        private string _formReason = string.Empty;

        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private string _reviewNotes = string.Empty;

        public LeaveRequestsViewModel(LeaveService leaveService, EmployeeService employeeService)
        {
            _leaveService = leaveService;
            _employeeService = employeeService;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            Employees = new ObservableCollection<Employee>(employees);

            var types = await _leaveService.GetLeaveTypesAsync();
            LeaveTypes = new ObservableCollection<LeaveType>(types);

            await LoadRequests();
        }

        [RelayCommand]
        private async Task LoadRequests()
        {
            var pending = await _leaveService.GetPendingLeaveRequestsAsync();
            PendingRequests = new ObservableCollection<LeaveRequest>(pending);
        }

        [RelayCommand]
        private void ShowAddLeaveRequestForm()
        {
            FormEmployee = null;
            FormLeaveType = LeaveTypes.FirstOrDefault();
            FormStartDate = DateTime.Today;
            FormEndDate = DateTime.Today;
            FormReason = string.Empty;
            IsFormVisible = true;
        }

        [RelayCommand]
        private void CloseForm()
        {
            IsFormVisible = false;
        }

        [RelayCommand]
        private async Task SubmitLeaveRequest()
        {
            if (FormEmployee == null || FormLeaveType == null) return;

            var username = UserSession.CurrentUser?.Username ?? "System";
            await _leaveService.SubmitLeaveRequestAsync(new LeaveRequest
            {
                EmployeeId = FormEmployee.Id,
                LeaveTypeId = FormLeaveType.Id,
                StartDate = FormStartDate,
                EndDate = FormEndDate,
                Reason = FormReason
            }, username);

            IsFormVisible = false;
            await LoadRequests();
            await LoadEmployeeRequestsAsync(FormEmployee);
        }

        [RelayCommand]
        private async Task ApproveRequest(LeaveRequest? request)
        {
            if (request == null) return;

            var employee = await _employeeService.GetEmployeeByIdAsync(request.EmployeeId);
            var locationId = employee?.LocationId ?? 0;
            var username = UserSession.CurrentUser?.Username ?? "System";

            await _leaveService.ApproveLeaveRequestAsync(request.Id, locationId, username, ReviewNotes);
            ReviewNotes = string.Empty;
            await LoadRequests();
        }

        [RelayCommand]
        private async Task RejectRequest(LeaveRequest? request)
        {
            if (request == null) return;

            var username = UserSession.CurrentUser?.Username ?? "System";
            await _leaveService.RejectLeaveRequestAsync(request.Id, username, ReviewNotes);
            ReviewNotes = string.Empty;
            await LoadRequests();
        }

        partial void OnFormEmployeeChanged(Employee? value)
        {
            _ = LoadEmployeeRequestsAsync(value);
        }

        private async Task LoadEmployeeRequestsAsync(Employee? employee)
        {
            if (employee == null)
            {
                RecentRequests = new ObservableCollection<LeaveRequest>();
                SelectedEmployeeBalances = new ObservableCollection<LeaveBalanceRow>();
                return;
            }

            var requests = await _leaveService.GetLeaveRequestsForEmployeeAsync(employee.Id);
            RecentRequests = new ObservableCollection<LeaveRequest>(requests);

            var balances = await _leaveService.GetLeaveBalancesForEmployeeAsync(employee.Id, DateTime.Today.Year);
            SelectedEmployeeBalances = new ObservableCollection<LeaveBalanceRow>(
                balances.Select(b => new LeaveBalanceRow
                {
                    LeaveTypeName = b.Type.Name,
                    Entitled = b.Entitled,
                    Used = b.Used,
                    Balance = b.Balance
                }));
        }
    }

    public class LeaveBalanceRow
    {
        public string LeaveTypeName { get; set; } = string.Empty;
        public int Entitled { get; set; }
        public int Used { get; set; }
        public int Balance { get; set; }
    }
}
