using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class UsersViewModel : ViewModelBase
    {
        private readonly UserService _userService;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private bool _isPaneOpen;

        [ObservableProperty]
        private string _paneTitle = "New User";

        [ObservableProperty]
        private string _errorMessage = "";

        // Form Fields
        [ObservableProperty] private string _username = "";
        [ObservableProperty] private string _password = "";
        [ObservableProperty] private string _selectedRole = "Staff";

        public ObservableCollection<string> Roles { get; } = new() { "Admin", "Staff" };

        public UsersViewModel(UserService userService)
        {
            _userService = userService;
            LoadUsersCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadUsers()
        {
            var list = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(list);
        }

        [RelayCommand]
        private void OpenAddUserPane()
        {
            PaneTitle = "New User";
            Username = "";
            Password = "";
            SelectedRole = "Staff";
            ErrorMessage = "";
            IsPaneOpen = true;
        }

        [RelayCommand]
        private async Task SaveUser()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Username and Password are required.";
                return;
            }

            var newUser = new User
            {
                Username = Username,
                Role = SelectedRole
            };

            await _userService.AddUserAsync(newUser, Password);
            IsPaneOpen = false;
            await LoadUsers();
        }

        [RelayCommand]
        private void CancelPane()
        {
            IsPaneOpen = false;
        }

        [RelayCommand]
        private async Task DeleteUser(User user)
        {
            if (user == null) return;
            if (user.Username == "admin" || user.Id == UserSession.CurrentUser?.Id)
            {
                // Prevent deleting self or the root admin
                return;
            }

            await _userService.DeleteUserAsync(user);
            await LoadUsers();
        }
    }
}
