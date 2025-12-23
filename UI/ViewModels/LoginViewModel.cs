using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private readonly Action _onLoginSuccess;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoginViewModel(UserService userService, Action onLoginSuccess)
        {
            _userService = userService;
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

            var user = await _userService.AuthenticateAsync(Username, Password);
            if (user != null)
            {
                UserSession.Login(user);
                _onLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
            }
        }
    }
}
