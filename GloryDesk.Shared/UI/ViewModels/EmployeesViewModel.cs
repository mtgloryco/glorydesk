using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class EmployeesViewModel : ViewModelBase
    {
        private readonly EmployeeService _employeeService;
        private readonly LocationService _locationService;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private ObservableCollection<Location> _locations = new();

        [ObservableProperty]
        private Employee _currentEmployee = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private Location? _selectedEmployeeLocation;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showInactive;

        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isDeleteConfirmationOpen;

        [ObservableProperty]
        private Employee? _employeeToDeactivate;

        public ObservableCollection<string> EmploymentTypes { get; } = new() { "Full-time", "Part-time", "Contract" };
        public ObservableCollection<string> PayFrequencies { get; } = new() { "Monthly", "Weekly", "Daily" };
        public ObservableCollection<string> StatusOptions { get; } = new() { "Active", "OnLeave", "Terminated" };

        public EmployeesViewModel(EmployeeService employeeService, LocationService locationService)
        {
            _employeeService = employeeService;
            _locationService = locationService;
            LoadEmployeesCommand.Execute(null);
            _ = LoadLocationsAsync();
        }

        [RelayCommand]
        private async Task LoadEmployees()
        {
            var list = await _employeeService.GetAllEmployeesAsync(includeInactive: ShowInactive);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                list = list.Where(e =>
                    e.Name.ToLower().Contains(query) ||
                    e.Position.ToLower().Contains(query) ||
                    e.Department.ToLower().Contains(query) ||
                    e.Phone.ToLower().Contains(query) ||
                    e.Email.ToLower().Contains(query) ||
                    e.NationalId.ToLower().Contains(query)
                ).ToList();
            }

            Employees = new ObservableCollection<Employee>(list);
        }

        private async Task LoadLocationsAsync()
        {
            var list = await _locationService.GetAllLocationsAsync();
            Locations = new ObservableCollection<Location>(list);
        }

        [RelayCommand]
        private void ShowAddEmployeeForm()
        {
            CurrentEmployee = new Employee();
            SelectedEmployeeLocation = null;
            IsFormVisible = true;
        }

        [RelayCommand]
        private void EditEmployee(Employee? employee)
        {
            if (employee == null)
            {
                CurrentEmployee = new Employee();
                SelectedEmployeeLocation = null;
                return;
            }

            CurrentEmployee = new Employee
            {
                Id = employee.Id,
                Name = employee.Name,
                NationalId = employee.NationalId,
                Phone = employee.Phone,
                Email = employee.Email,
                Position = employee.Position,
                Department = employee.Department,
                LocationId = employee.LocationId,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                Status = employee.Status,
                EmploymentType = employee.EmploymentType,
                PayFrequency = employee.PayFrequency,
                BaseSalary = employee.BaseSalary,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                EmergencyContactName = employee.EmergencyContactName,
                EmergencyContactPhone = employee.EmergencyContactPhone,
                UserId = employee.UserId,
                IsActive = employee.IsActive,
                CreatedAt = employee.CreatedAt
            };
            SelectedEmployeeLocation = Locations.FirstOrDefault(l => l.Id == employee.LocationId);
            IsFormVisible = true;
        }

        [RelayCommand]
        private void CloseForm()
        {
            IsFormVisible = false;
        }

        [RelayCommand]
        private async Task SaveEmployee()
        {
            if (string.IsNullOrWhiteSpace(CurrentEmployee.Name))
            {
                return;
            }

            CurrentEmployee.LocationId = SelectedEmployeeLocation?.Id ?? 0;
            var username = UserSession.CurrentUser?.Username ?? "System";

            if (CurrentEmployee.Id == 0)
            {
                await _employeeService.AddEmployeeAsync(CurrentEmployee, username);
            }
            else
            {
                await _employeeService.UpdateEmployeeAsync(CurrentEmployee, username);
            }

            CurrentEmployee = new Employee();
            IsFormVisible = false;
            await LoadEmployees();
        }

        [RelayCommand]
        private void ConfirmDeactivateEmployee(Employee? employee)
        {
            if (employee == null) return;
            EmployeeToDeactivate = employee;
            IsDeleteConfirmationOpen = true;
        }

        [RelayCommand]
        private void CancelDeactivate()
        {
            IsDeleteConfirmationOpen = false;
            EmployeeToDeactivate = null;
        }

        [RelayCommand]
        private async Task ExecuteDeactivate()
        {
            if (EmployeeToDeactivate == null) return;

            var username = UserSession.CurrentUser?.Username ?? "System";
            await _employeeService.DeactivateEmployeeAsync(EmployeeToDeactivate.Id, username);

            IsDeleteConfirmationOpen = false;
            EmployeeToDeactivate = null;
            SelectedEmployee = null;
            await LoadEmployees();
        }

        partial void OnSearchTextChanged(string value)
        {
            LoadEmployeesCommand.Execute(null);
        }

        partial void OnShowInactiveChanged(bool value)
        {
            LoadEmployeesCommand.Execute(null);
        }
    }
}
