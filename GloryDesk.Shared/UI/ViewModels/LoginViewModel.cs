using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private readonly AuditService _auditService;
        private readonly CloudSyncService? _cloudSyncService;
        private readonly ISessionStore? _sessionStore;
        private readonly LicenseService? _licenseService;
        private readonly SettingsService? _settingsService;
        private readonly System.Action _onLoginSuccess;

        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public string LoginButtonText => IsBusy ? "Signing in..." : "Sign In";

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(LoginButtonText));
        }

        public LoginViewModel(
            UserService userService,
            AuditService auditService,
            System.Action onLoginSuccess,
            CloudSyncService? cloudSyncService = null,
            ISessionStore? sessionStore = null,
            LicenseService? licenseService = null,
            SettingsService? settingsService = null)
        {
            _userService = userService;
            _auditService = auditService;
            _onLoginSuccess = onLoginSuccess;
            _cloudSyncService = cloudSyncService;
            _sessionStore = sessionStore;
            _licenseService = licenseService;
            _settingsService = settingsService;
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

            IsBusy = true;
            try
            {
                var cleanUsername = Username.Trim();

                // 1. Try local authentication first (for offline POS clerks, admin, etc.)
                var user = await _userService.AuthenticateAsync(cleanUsername, Password);

                // 2. If not found locally, attempt Cloud authentication (for users created on glorydesk-web)
                if (user == null && _cloudSyncService != null)
                {
                    var cloudResult = await _cloudSyncService.ConfigureCloudLoginAsync(cleanUsername, Password);
                    if (cloudResult.Success)
                    {
                        var allUsers = await _userService.GetAllUsersAsync();
                        user = allUsers.FirstOrDefault(u => string.Equals(u.Username, cleanUsername, StringComparison.OrdinalIgnoreCase));
                        var resolvedRole = !string.IsNullOrWhiteSpace(cloudResult.Role)
                            ? (char.ToUpper(cloudResult.Role[0]) + cloudResult.Role[1..].ToLower())
                            : "Admin";

                        if (user == null)
                        {
                            user = new User
                            {
                                Username = cleanUsername,
                                Role = resolvedRole
                            };
                            await _userService.AddUserAsync(user, Password);
                        }
                        else if (!string.IsNullOrWhiteSpace(cloudResult.Role) && !string.Equals(user.Role, resolvedRole, StringComparison.OrdinalIgnoreCase))
                        {
                            user.Role = resolvedRole;
                            await _userService.UpdateUserAsync(user);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(cloudResult.Message))
                    {
                        ErrorMessage = cloudResult.Message;
                        return;
                    }
                }

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

                if (_sessionStore != null)
                {
                    try
                    {
                        var syncStatus = _cloudSyncService != null ? await _cloudSyncService.GetStatusAsync() : null;
                        var orgName = syncStatus?.OrganizationName;
                        var sessionData = new UserSessionData
                        {
                            Username = user.Username,
                            Email = cleanUsername,
                            Role = user.Role,
                            AuthToken = _cloudSyncService?.AuthToken,
                            OrganizationName = orgName,
                            LicenseKey = _licenseService?.CurrentLicense?.LicenseToken
                        };
                        _sessionStore.SaveSession(sessionData);

                        if (_settingsService != null && !string.IsNullOrWhiteSpace(orgName))
                        {
                            _settingsService.CurrentSettings.StoreName = orgName;
                            _settingsService.CurrentSettings.SetupCompleted = true;
                            _settingsService.SaveSettings();
                        }
                    }
                    catch { }
                }

                _onLoginSuccess?.Invoke();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
