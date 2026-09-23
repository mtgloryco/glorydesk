using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class CustomFieldServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private CustomFieldService _service = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _service = new CustomFieldService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task AddDefinitionAsync_ThenGetDefinitionsAsync_RoundTrips()
    {
        var added = await _service.AddDefinitionAsync(new CustomFieldDefinition
        {
            EntityType = "Product",
            FieldKey = "Grade",
            FieldLabel = "Grade",
            FieldType = "Choice",
            ChoiceOptions = "A,B,C"
        });

        var definitions = await _service.GetDefinitionsAsync("Product");

        Assert.Contains(definitions, d => d.Id == added.Id && d.FieldKey == "Grade" && d.FieldLabel == "Grade" && d.ChoiceOptions == "A,B,C");
    }

    [Fact]
    public async Task SetValueAsync_ThenGetValuesAsync_RoundTrips()
    {
        var definition = await _service.AddDefinitionAsync(new CustomFieldDefinition
        {
            EntityType = "Product",
            FieldKey = "Origin",
            FieldLabel = "Origin",
            FieldType = "Text"
        });

        const int entityId = 42;
        await _service.SetValueAsync(definition.Id, entityId, "Ethiopia");

        var values = await _service.GetValuesAsync(entityId, new[] { definition.Id });
        Assert.Equal("Ethiopia", values[definition.Id]);

        // SetValueAsync must update in place rather than duplicate rows on a second call for the same entity.
        await _service.SetValueAsync(definition.Id, entityId, "Kenya");
        var updatedValues = await _service.GetValuesAsync(entityId, new[] { definition.Id });
        Assert.Equal("Kenya", updatedValues[definition.Id]);
    }

    [Fact]
    public async Task DeactivateDefinitionAsync_ExcludedWhenActiveOnly_ButIncludedWhenNot()
    {
        var definition = await _service.AddDefinitionAsync(new CustomFieldDefinition
        {
            EntityType = "Product",
            FieldKey = "Deprecated",
            FieldLabel = "Deprecated Field",
            FieldType = "Text"
        });

        await _service.DeactivateDefinitionAsync(definition.Id);

        var activeOnly = await _service.GetDefinitionsAsync("Product", activeOnly: true);
        var all = await _service.GetDefinitionsAsync("Product", activeOnly: false);

        Assert.DoesNotContain(activeOnly, d => d.Id == definition.Id);
        Assert.Contains(all, d => d.Id == definition.Id);
    }

    [Fact]
    public async Task EnsureDefinitionAsync_CalledTwice_ReturnsSameDefinition_NoDuplicateRows()
    {
        var first = await _service.EnsureDefinitionAsync("Product", "CropSeason", "Crop Season", "Text", null);
        var second = await _service.EnsureDefinitionAsync("Product", "CropSeason", "Crop Season", "Text", null);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);

        var all = await _service.GetDefinitionsAsync("Product", activeOnly: false);
        Assert.Single(all.Where(d => d.FieldKey == "CropSeason"));
    }

    [Fact]
    public async Task EnsureDefinitionAsync_DoesNotOverwriteUserCustomizedLabel()
    {
        var original = await _service.EnsureDefinitionAsync("Product", "Grade", "Grade", "Choice", "A,AA,AB");
        Assert.NotNull(original);

        // Simulate a user renaming the field via the Custom Fields settings tab.
        original!.FieldLabel = "Quality Grade";
        await _service.UpdateDefinitionAsync(original);

        // Re-applying a template (or another EnsureDefinitionAsync call with the original suggested label)
        // must not clobber the user's customization.
        var reEnsured = await _service.EnsureDefinitionAsync("Product", "Grade", "Grade", "Choice", "A,AA,AB");

        Assert.NotNull(reEnsured);
        Assert.Equal(original.Id, reEnsured!.Id);
        Assert.Equal("Quality Grade", reEnsured.FieldLabel);
    }
}
