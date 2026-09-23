using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    /// <summary>
    /// Small companion helper for <see cref="Views.CustomFieldsPanel"/> that loads
    /// custom field definitions/values for an entity and persists edited values back.
    /// </summary>
    public class CustomFieldsPanelViewModel
    {
        public ObservableCollection<CustomFieldInputItem> Items { get; } = new();

        public async Task LoadAsync(CustomFieldService service, string entityType, int? entityId)
        {
            Items.Clear();

            var definitions = await service.GetDefinitionsAsync(entityType);
            if (definitions.Count == 0)
            {
                return;
            }

            var values = new Dictionary<int, string>();
            if (entityId.HasValue && entityId.Value > 0)
            {
                values = await service.GetValuesAsync(entityId.Value, definitions.Select(d => d.Id));
            }

            foreach (var definition in definitions)
            {
                values.TryGetValue(definition.Id, out var existingValue);
                Items.Add(new CustomFieldInputItem(definition, existingValue));
            }
        }

        public async Task SaveAsync(CustomFieldService service, int entityId)
        {
            foreach (var item in Items)
            {
                var value = item.Definition.FieldType == "Boolean"
                    ? (item.BoolValue ? "true" : "false")
                    : (item.TextValue ?? string.Empty);

                await service.SetValueAsync(item.Definition.Id, entityId, value);
            }
        }
    }
}
