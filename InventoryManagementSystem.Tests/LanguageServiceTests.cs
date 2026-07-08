using System.Collections.Generic;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class LanguageServiceTests
{
    [Fact]
    public void WithoutOverrides_GetStringIndexerAndResources_ReturnEnglishDictionaryValue()
    {
        var language = new LanguageService();

        Assert.Equal("Dashboard", language.GetString("Dashboard"));
        Assert.Equal("Dashboard", language["Dashboard"]);
        Assert.Equal("Dashboard", language.Resources["Dashboard"]);
    }

    [Fact]
    public void SetTerminologyOverrides_OverriddenKeyReturnsOverride_UnoverriddenKeyFallsBackNormally()
    {
        var language = new LanguageService();

        language.SetTerminologyOverrides(new Dictionary<string, string> { ["Dashboard"] = "Control Panel" });

        Assert.Equal("Control Panel", language.GetString("Dashboard"));
        Assert.Equal("Control Panel", language.Resources["Dashboard"]);

        // A key with no override still falls back to the normal English dictionary value.
        Assert.Equal("Reports", language.GetString("Reports"));
    }

    [Fact]
    public void SetLanguage_French_TerminologyOverridesStillApply_AcrossAllLanguages()
    {
        var language = new LanguageService();
        language.SetTerminologyOverrides(new Dictionary<string, string> { ["Dashboard"] = "Control Panel" });

        language.SetLanguage("fr");

        // LanguageService.Resources layers _terminologyOverrides on top of whichever language dictionary
        // is currently active, so overrides are intentionally not English-only - they persist across
        // language switches.
        Assert.Equal("Control Panel", language.GetString("Dashboard"));
        Assert.Equal("Control Panel", language.Resources["Dashboard"]);

        // A key without an override falls back to the newly selected language's own dictionary value.
        Assert.Equal("Caisse / PV", language.GetString("POS"));
    }
}
