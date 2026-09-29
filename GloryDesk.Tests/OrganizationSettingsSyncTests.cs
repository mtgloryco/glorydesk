using System;
using System.IO;
using System.Threading.Tasks;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace GloryDesk.Tests;

public class OrganizationSettingsSyncTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _tempSettingsPath;

    public OrganizationSettingsSyncTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"org-test-{Guid.NewGuid():N}.db");
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try { if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath); } catch { }
        try { if (File.Exists(_tempSettingsPath)) File.Delete(_tempSettingsPath); } catch { }
        SettingsService.CustomLoadSettingsHandler = null;
        SettingsService.CustomSaveSettingsHandler = null;
    }

    [Fact]
    public void SettingsService_ApplyCloudOrganizationSettings_SetsAllPropertiesAndMarksSetupComplete()
    {
        var settingsService = new SettingsService(_tempSettingsPath);
        Assert.False(settingsService.CurrentSettings.SetupCompleted);

        var cloudSettings = new OrganizationSettingsDto
        {
            StoreName = "MT GLORY Kigali",
            StoreAddress = "KN 4 Ave, Kigali",
            CurrencySymbol = "RWF",
            BusinessType = "retail",
            CostingMethod = "FIFO",
            SetupCompleted = true
        };

        settingsService.ApplyCloudOrganizationSettings(cloudSettings);

        Assert.True(settingsService.CurrentSettings.SetupCompleted);
        Assert.Equal("MT GLORY Kigali", settingsService.CurrentSettings.StoreName);
        Assert.Equal("KN 4 Ave, Kigali", settingsService.CurrentSettings.StoreAddress);
        Assert.Equal("RWF", settingsService.CurrentSettings.CurrencySymbol);
        Assert.Equal("retail", settingsService.CurrentSettings.BusinessType);
    }

    [Fact]
    public void SettingsService_ExportToCloudSettings_CapturesCurrentSettings()
    {
        var settingsService = new SettingsService(_tempSettingsPath);
        settingsService.CurrentSettings.StoreName = "Bienvenu Supermarket";
        settingsService.CurrentSettings.CurrencySymbol = "USD";
        settingsService.CurrentSettings.SetupCompleted = true;

        var exported = settingsService.ExportToCloudSettings();

        Assert.Equal("Bienvenu Supermarket", exported.StoreName);
        Assert.Equal("USD", exported.CurrencySymbol);
        Assert.True(exported.SetupCompleted);
    }

    [Fact]
    public void SettingsService_CustomHandlers_EnableBrowserLocalStorageEmulation()
    {
        string? storedJson = null;

        SettingsService.CustomLoadSettingsHandler = () => storedJson;
        SettingsService.CustomSaveSettingsHandler = (json) => storedJson = json;

        var settingsService = new SettingsService(_tempSettingsPath);
        settingsService.CurrentSettings.StoreName = "Browser Store";
        settingsService.CurrentSettings.SetupCompleted = true;
        settingsService.SaveSettings();

        Assert.NotNull(storedJson);
        Assert.Contains("Browser Store", storedJson);

        // Simulate new app instance loading from localStorage
        var newInstance = new SettingsService(_tempSettingsPath);
        Assert.True(newInstance.CurrentSettings.SetupCompleted);
        Assert.Equal("Browser Store", newInstance.CurrentSettings.StoreName);
    }
}
