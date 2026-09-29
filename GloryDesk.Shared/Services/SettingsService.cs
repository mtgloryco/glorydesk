using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InventoryManagementSystem.Services
{
    public class AppSettings
    {
        public string StoreName { get; set; } = "My Store";
        public string StoreAddress { get; set; } = "Kigali, Rwanda";
        public string CurrencySymbol { get; set; } = "RWF";
        public decimal DefaultTaxRate { get; set; } = 0.18m;
        public string PrinterName { get; set; } = "";
        public bool IsDarkTheme { get; set; } = true;
        public bool IsSudoModeEnabled { get; set; } = false;
        public string BusinessType { get; set; } = "";
        public bool SetupCompleted { get; set; } = false;
        public Dictionary<string, bool> EnabledModules { get; set; } = new();
        public Dictionary<string, string> TerminologyOverrides { get; set; } = new();

        /// <summary>FIFO (default) or WeightedAverage.</summary>
        public string CostingMethod { get; set; } = "FIFO";

        public bool UseSmtp { get; set; } = false;
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string SmtpFromAddress { get; set; } = "";
        public string SmtpFromName { get; set; } = "";
        public bool SmtpEnableSsl { get; set; } = true;
    }

    public class SettingsService
    {
        public static Func<string?>? CustomLoadSettingsHandler { get; set; }
        public static Action<string>? CustomSaveSettingsHandler { get; set; }

        private readonly string _settingsFilePath;
        public AppSettings CurrentSettings { get; private set; } = new AppSettings();

        public SettingsService()
        {
            var folder = AppPaths.GetRoamingAppDataFolder();
            _settingsFilePath = Path.Combine(folder, "settings.json");
            LoadSettings();
        }

        /// <summary>
        /// Testability seam: points settings persistence at an explicit file (e.g. a temp file in unit tests)
        /// instead of the fixed per-user ApplicationData location used by the parameterless constructor.
        /// </summary>
        public SettingsService(string customSettingsFilePath)
        {
            var folder = Path.GetDirectoryName(customSettingsFilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _settingsFilePath = customSettingsFilePath;
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (CustomLoadSettingsHandler != null)
            {
                try
                {
                    var customJson = CustomLoadSettingsHandler();
                    if (!string.IsNullOrWhiteSpace(customJson))
                    {
                        CurrentSettings = JsonSerializer.Deserialize<AppSettings>(customJson) ?? new AppSettings();
                        return;
                    }
                }
                catch
                {
                    // Fall back to file
                }
            }

            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    CurrentSettings = new AppSettings();
                }
            }
            else
            {
                CurrentSettings = new AppSettings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(CurrentSettings, options);

                try
                {
                    CustomSaveSettingsHandler?.Invoke(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running custom settings save handler: {ex.Message}");
                }

                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void ApplyCloudOrganizationSettings(OrganizationSettingsDto cloudSettings)
        {
            if (cloudSettings == null) return;
            if (!string.IsNullOrWhiteSpace(cloudSettings.StoreName)) CurrentSettings.StoreName = cloudSettings.StoreName;
            if (!string.IsNullOrWhiteSpace(cloudSettings.StoreAddress)) CurrentSettings.StoreAddress = cloudSettings.StoreAddress;
            if (!string.IsNullOrWhiteSpace(cloudSettings.CurrencySymbol)) CurrentSettings.CurrencySymbol = cloudSettings.CurrencySymbol;
            if (cloudSettings.DefaultTaxRate > 0) CurrentSettings.DefaultTaxRate = cloudSettings.DefaultTaxRate;
            if (!string.IsNullOrWhiteSpace(cloudSettings.BusinessType)) CurrentSettings.BusinessType = cloudSettings.BusinessType;
            if (!string.IsNullOrWhiteSpace(cloudSettings.CostingMethod)) CurrentSettings.CostingMethod = cloudSettings.CostingMethod;
            if (cloudSettings.EnabledModules != null && cloudSettings.EnabledModules.Count > 0)
            {
                foreach (var kvp in cloudSettings.EnabledModules) CurrentSettings.EnabledModules[kvp.Key] = kvp.Value;
            }
            if (cloudSettings.TerminologyOverrides != null && cloudSettings.TerminologyOverrides.Count > 0)
            {
                foreach (var kvp in cloudSettings.TerminologyOverrides) CurrentSettings.TerminologyOverrides[kvp.Key] = kvp.Value;
            }
            CurrentSettings.SetupCompleted = true;
            SaveSettings();
        }

        public OrganizationSettingsDto ExportToCloudSettings()
        {
            return new OrganizationSettingsDto
            {
                StoreName = CurrentSettings.StoreName,
                StoreAddress = CurrentSettings.StoreAddress,
                CurrencySymbol = CurrentSettings.CurrencySymbol,
                DefaultTaxRate = CurrentSettings.DefaultTaxRate,
                BusinessType = CurrentSettings.BusinessType,
                SetupCompleted = CurrentSettings.SetupCompleted,
                CostingMethod = CurrentSettings.CostingMethod,
                EnabledModules = new Dictionary<string, bool>(CurrentSettings.EnabledModules),
                TerminologyOverrides = new Dictionary<string, string>(CurrentSettings.TerminologyOverrides),
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
