using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class IndustryTemplateServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private readonly string _settingsPath = TempFile.CreateSettingsPath();
    private DatabaseService _db = null!;
    private SettingsService _settings = null!;
    private CustomFieldService _customFields = null!;
    private IndustryTemplateService _templates = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _settings = new SettingsService(_settingsPath);
        _customFields = new CustomFieldService(_db);
        _templates = new IndustryTemplateService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
        TempFile.DeleteFile(_settingsPath);
    }

    [Fact]
    public void GetAvailableTemplates_ReturnsAtLeastSixUniqueTemplatesWithMetadata()
    {
        var templates = IndustryTemplateService.GetAvailableTemplates();

        Assert.True(templates.Count >= 6, $"Expected at least 6 templates, got {templates.Count}");

        foreach (var template in templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(template.Key), "Template Key must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(template.DisplayName), "Template DisplayName must not be empty");
        }

        var uniqueKeyCount = templates.Select(t => t.Key).Distinct().Count();
        Assert.Equal(templates.Count, uniqueKeyCount);
    }

    [Fact]
    public async Task ApplyTemplateAsync_CoffeeTemplate_CreatesExpectedCategoriesAndCustomFields()
    {
        await _templates.ApplyTemplateAsync("coffee_agro_processing", _settings, _customFields);

        var categories = await _db.Connection.Table<Category>().ToListAsync();
        foreach (var expectedCategory in new[] { "Green Beans", "Roasted Beans", "Parchment" })
        {
            Assert.Contains(categories, c => c.Name == expectedCategory);
        }

        var productFields = await _customFields.GetDefinitionsAsync("Product");
        foreach (var expectedKey in new[] { "Grade", "MoisturePercent", "Origin" })
        {
            Assert.Contains(productFields, f => f.FieldKey == expectedKey);
        }
    }

    [Fact]
    public async Task ApplyTemplateAsync_SetsBusinessTypeSetupCompletedAndModules()
    {
        await _templates.ApplyTemplateAsync("coffee_agro_processing", _settings, _customFields);

        Assert.Equal("coffee_agro_processing", _settings.CurrentSettings.BusinessType);
        Assert.True(_settings.CurrentSettings.SetupCompleted);
        Assert.True(_settings.CurrentSettings.EnabledModules["Manufacturing"]);
        Assert.True(_settings.CurrentSettings.EnabledModules["BOM"]);
    }

    [Fact]
    public async Task ApplyTemplateAsync_DoesNotClobberExistingUserTerminologyOverride()
    {
        // The user has already customized "Product" before ever applying a template.
        _settings.CurrentSettings.TerminologyOverrides["Product"] = "MyCustomLabel";

        // The coffee template also wants to set "Product" -> "Lot".
        await _templates.ApplyTemplateAsync("coffee_agro_processing", _settings, _customFields);

        Assert.Equal("MyCustomLabel", _settings.CurrentSettings.TerminologyOverrides["Product"]);
    }

    [Fact]
    public async Task ApplyTemplateAsync_CalledTwice_IsIdempotent()
    {
        await _templates.ApplyTemplateAsync("coffee_agro_processing", _settings, _customFields);
        await _templates.ApplyTemplateAsync("coffee_agro_processing", _settings, _customFields);

        var categories = await _db.Connection.Table<Category>().ToListAsync();
        Assert.Single(categories.Where(c => c.Name == "Green Beans"));
        Assert.Single(categories.Where(c => c.Name == "Roasted Beans"));
        Assert.Single(categories.Where(c => c.Name == "Parchment"));

        var productFields = await _customFields.GetDefinitionsAsync("Product", activeOnly: false);
        Assert.Single(productFields.Where(f => f.FieldKey == "Grade"));
        Assert.Single(productFields.Where(f => f.FieldKey == "MoisturePercent"));
        Assert.Single(productFields.Where(f => f.FieldKey == "Origin"));
    }
}
