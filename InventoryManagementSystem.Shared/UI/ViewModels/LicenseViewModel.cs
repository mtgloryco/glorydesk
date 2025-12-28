using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class LicenseViewModel : ViewModelBase
    {
        private readonly LicenseService _licenseService;

        [ObservableProperty]
        private string _licenseType = "Loading...";

        [ObservableProperty]
        private string _licenseStatus = "Loading...";

        [ObservableProperty]
        private string _expirationDate = "";

        [ObservableProperty]
        private string _fingerprint = "";

        [ObservableProperty]
        private string _hardwareId = "";

        [ObservableProperty]
        private string _inputToken = "";

        [ObservableProperty]
        private string _activationMessage = "";

        [ObservableProperty]
        private bool _isActivatedSuccessfully;

        private readonly Action _onActivationSuccess;

        public LicenseViewModel(LicenseService licenseService, HardwareIdService hardwareIdService, Action onActivationSuccess)
        {
            _licenseService = licenseService;
            _onActivationSuccess = onActivationSuccess;
            HardwareId = hardwareIdService.GetCompositeHardwareId();
            RefreshLicenseInfo();
        }

        private void RefreshLicenseInfo()
        {
            var lic = _licenseService.CurrentLicense;
            LicenseType = lic.Type == "None" ? "Activation Required" : lic.Type;
            LicenseStatus = lic.Status;

            if (lic.Status == "Locked" || lic.ExpirationDate == DateTime.MinValue)
            {
                ExpirationDate = "N/A - Product Locked";
            }
            else
            {
                ExpirationDate = lic.ExpirationDate.ToString("yyyy-MM-dd");
            }

            Fingerprint = lic.DeviceFingerprint;
        }

        [RelayCommand]
        private async Task Activate()
        {
            if (string.IsNullOrWhiteSpace(InputToken))
            {
                ActivationMessage = "Please enter your license key.";
                return;
            }

            var token = InputToken.Trim();
            var result = await _licenseService.ActivateLicenseAsync(token);

            switch (result)
            {
                case LicenseValidationResult.Valid:
                    ActivationMessage = "Success! Professional license activated.";
                    IsActivatedSuccessfully = true;
                    RefreshLicenseInfo();
                    break;
                case LicenseValidationResult.Expired:
                    ActivationMessage = "This license key has expired.";
                    break;
                case LicenseValidationResult.HardwareMismatch:
                    ActivationMessage = "Hardware Mismatch: This key is for another computer.";
                    break;
                case LicenseValidationResult.InvalidSignature:
                    ActivationMessage = "Invalid Signature: The key has been tampered with or is fake.";
                    break;
                case LicenseValidationResult.InvalidFormat:
                    ActivationMessage = "Invalid Format: Not a valid license key string.";
                    break;
                default:
                    ActivationMessage = "Activation failed. Please check your key.";
                    break;
            }
        }

        [RelayCommand]
        public void ContinueToLogin() => _onActivationSuccess?.Invoke();
    }
}
