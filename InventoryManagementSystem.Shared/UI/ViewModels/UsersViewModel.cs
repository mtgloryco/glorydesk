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
    public partial class UserModuleAccessItem : ObservableObject
    {
        public string PermissionKey { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;

        [ObservableProperty] private bool _isGranted;
    }

    public partial class UsersViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private int? _editingUserId;

        [ObservableProperty] private ObservableCollection<User> _users = new();
        [ObservableProperty] private ObservableCollection<UserModuleAccessItem> _moduleAccess = new();
        [ObservableProperty] private bool _isPaneOpen;
        [ObservableProperty] private string _paneTitle = "New User";
        [ObservableProperty] private string _errorMessage = "";
        [ObservableProperty] private string _successMessage = "";
        [ObservableProperty] private bool _isUsernameReadOnly;

        [ObservableProperty] private string _username = "";
        [ObservableProperty] private string _password = "";
        [ObservableProperty] private string _selectedRole = "Staff";

        public ObservableCollection<string> Roles { get; } = new(RolePermissions.GetAssignableRoles());

        public UsersViewModel(UserService userService)
        {
            _userService = userService;
            InitializeModuleAccessFromRole("Staff");
            LoadUsersCommand.Execute(null);
        }

        partial void OnSelectedRoleChanged(string value)
        {
            if (_editingUserId == null)
            {
                ApplyRoleTemplate(value);
            }
        }

        [RelayCommand]
        private async Task LoadUsers()
        {
            var list = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(list.OrderBy(u => u.Username));
        }

        [RelayCommand]
        private void OpenAddUserPane()
        {
            _editingUserId = null;
            PaneTitle = "New User";
            Username = "";
            Password = "";
            SelectedRole = "Staff";
            IsUsernameReadOnly = false;
            ErrorMessage = "";
            SuccessMessage = "";
            ApplyRoleTemplate("Staff");
            IsPaneOpen = true;
        }

        [RelayCommand]
        private void OpenEditUserPane(User user)
        {
            if (user == null) return;

            _editingUserId = user.Id;
            PaneTitle = $"Edit User — {user.Username}";
            Username = user.Username;
            Password = "";
            SelectedRole = user.Role;
            IsUsernameReadOnly = true;
            ErrorMessage = "";
            SuccessMessage = "";

            var granted = UserAccessService.DeserializePermissions(user.PermissionsJson);
            if (granted.Count == 0)
            {
                granted = UserAccessService.GetDefaultPermissionsForRole(user.Role);
            }

            foreach (var item in ModuleAccess)
            {
                item.IsGranted = granted.Contains(item.PermissionKey);
            }

            IsPaneOpen = true;
        }

        [RelayCommand]
        private void ApplyRoleTemplateCommand() => ApplyRoleTemplate(SelectedRole);

        [RelayCommand]
        private async Task SaveUser()
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Username is required.";
                return;
            }

            if (_editingUserId == null && string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Password is required for new users.";
                return;
            }

            var granted = ModuleAccess.Where(m => m.IsGranted).Select(m => m.PermissionKey).ToList();
            if (granted.Count == 0 && !SelectedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Select at least one module, or choose Admin role.";
                return;
            }

            if (_editingUserId == null)
            {
                var newUser = new User
                {
                    Username = Username.Trim(),
                    Role = SelectedRole,
                    PermissionsJson = UserAccessService.SerializePermissions(granted)
                };

                await _userService.AddUserAsync(newUser, Password);
                SuccessMessage = $"User created. Login: {newUser.Username} / {Password}";
            }
            else
            {
                var existing = await _userService.GetUserByIdAsync(_editingUserId.Value);
                if (existing == null)
                {
                    ErrorMessage = "User not found.";
                    return;
                }

                existing.Role = SelectedRole;
                existing.PermissionsJson = UserAccessService.SerializePermissions(granted);
                await _userService.UpdateUserAsync(existing);

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    await _userService.UpdatePasswordAsync(existing, Password);
                    SuccessMessage = $"User updated. New password set for {existing.Username}.";
                }
                else
                {
                    SuccessMessage = $"User {existing.Username} updated.";
                }
            }

            await LoadUsers();
        }

        [RelayCommand]
        private void ClosePane()
        {
            IsPaneOpen = false;
            _editingUserId = null;
        }

        [RelayCommand]
        private async Task DeleteUser(User user)
        {
            if (user == null) return;
            if (user.Username == "admin" || user.Id == UserSession.CurrentUser?.Id)
            {
                return;
            }

            await _userService.DeleteUserAsync(user);
            await LoadUsers();
        }

        private void InitializeModuleAccessFromRole(string role)
        {
            ModuleAccess.Clear();
            foreach (var (key, label) in UserAccessService.AllModules)
            {
                ModuleAccess.Add(new UserModuleAccessItem
                {
                    PermissionKey = key,
                    Label = label,
                    IsGranted = RolePermissions.HasPermission(role, key)
                });
            }
        }

        private void ApplyRoleTemplate(string role)
        {
            var defaults = UserAccessService.GetDefaultPermissionsForRole(role);
            foreach (var item in ModuleAccess)
            {
                item.IsGranted = defaults.Contains(item.PermissionKey);
            }
        }
    }
}
