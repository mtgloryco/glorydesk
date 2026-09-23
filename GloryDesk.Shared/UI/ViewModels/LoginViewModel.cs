using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private readonly AuditService _auditService;
        private readonly System.Action _onLoginSuccess;

        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _errorMessage = string.Empty;

        public LoginViewModel(UserService userService, AuditService auditService, System.Action onLoginSuccess)
        {
            _userService = userService;
            _auditService = auditService;
            _onLoginSuccess = onLoginSuccess;
        }

        [RelayCommand]
        private async Task Login()
        {
            ErrorMessage = "";
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password.";
                return;
            }

            var user = await _userService.AuthenticateAsync(Username.Trim(), Password);
            if (user == null)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage = "This account is disabled.";
                return;
            }

            await _userService.RecordLoginAsync(user);
            await _auditService.LogActionAsync(user.Username, "Login", "User", user.Id, new { user.Username, user.Role });

            UserSession.Login(user);
            _onLoginSuccess?.Invoke();
        }
    }
}
