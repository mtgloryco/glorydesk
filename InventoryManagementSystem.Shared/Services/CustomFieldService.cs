using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class CustomFieldService
    {
        private readonly DatabaseService _databaseService;

        public CustomFieldService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<CustomFieldDefinition>> GetDefinitionsAsync(string entityType, bool activeOnly = true)
        {
            var query = _databaseService.Connection.Table<CustomFieldDefinition>()
                .Where(d => d.EntityType == entityType);

            var definitions = await query.ToListAsync();
            if (activeOnly)
            {
                definitions = definitions.Where(d => d.IsActive).ToList();
            }

            return definitions.OrderBy(d => d.DisplayOrder).ToList();
        }

        public async Task<CustomFieldDefinition> AddDefinitionAsync(CustomFieldDefinition def)
        {
            if (def.DisplayOrder == 0)
            {
                var existing = await _databaseService.Connection.Table<CustomFieldDefinition>()
                    .Where(d => d.EntityType == def.EntityType)
                    .ToListAsync();
                def.DisplayOrder = existing.Count == 0 ? 1 : existing.Max(d => d.DisplayOrder) + 1;
            }

            await _databaseService.Connection.InsertAsync(def);
            return def;
        }

        public async Task UpdateDefinitionAsync(CustomFieldDefinition def)
        {
            await _databaseService.Connection.UpdateAsync(def);
        }

        public async Task DeactivateDefinitionAsync(int id)
        {
            var def = await _databaseService.Connection.FindAsync<CustomFieldDefinition>(id);
            if (def == null) return;
            def.IsActive = false;
            await _databaseService.Connection.UpdateAsync(def);
        }

        public async Task<Dictionary<int, string>> GetValuesAsync(int entityId, IEnumerable<int> definitionIds)
        {
            var idList = definitionIds.ToList();
            var values = await _databaseService.Connection.Table<CustomFieldValue>()
                .Where(v => v.EntityId == entityId)
                .ToListAsync();

            return values.Where(v => idList.Contains(v.DefinitionId))
                .ToDictionary(v => v.DefinitionId, v => v.ValueText);
        }

        public async Task SetValueAsync(int definitionId, int entityId, string value)
        {
            var existing = await _databaseService.Connection.Table<CustomFieldValue>()
                .Where(v => v.DefinitionId == definitionId && v.EntityId == entityId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.ValueText = value;
                await _databaseService.Connection.UpdateAsync(existing);
            }
            else
            {
                await _databaseService.Connection.InsertAsync(new CustomFieldValue
                {
                    DefinitionId = definitionId,
                    EntityId = entityId,
                    ValueText = value
                });
            }
        }

        public async Task<Dictionary<int, Dictionary<int, string>>> GetValuesForEntitiesAsync(string entityType, IEnumerable<int> entityIds)
        {
            var idList = entityIds.ToList();
            var definitionIds = (await GetDefinitionsAsync(entityType, activeOnly: false))
                .Select(d => d.Id)
                .ToList();

            var result = new Dictionary<int, Dictionary<int, string>>();
            if (idList.Count == 0 || definitionIds.Count == 0)
            {
                return result;
            }

            var allValues = await _databaseService.Connection.Table<CustomFieldValue>().ToListAsync();
            var relevant = allValues.Where(v => idList.Contains(v.EntityId) && definitionIds.Contains(v.DefinitionId));

            foreach (var value in relevant)
            {
                if (!result.TryGetValue(value.EntityId, out var byDefinition))
                {
                    byDefinition = new Dictionary<int, string>();
                    result[value.EntityId] = byDefinition;
                }
                byDefinition[value.DefinitionId] = value.ValueText;
            }

            return result;
        }

        public async Task<CustomFieldDefinition?> EnsureDefinitionAsync(string entityType, string fieldKey, string fieldLabel, string fieldType, string? choiceOptions)
        {
            var existing = await _databaseService.Connection.Table<CustomFieldDefinition>()
                .Where(d => d.EntityType == entityType && d.FieldKey == fieldKey)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return existing;
            }

            var definitions = await _databaseService.Connection.Table<CustomFieldDefinition>()
                .Where(d => d.EntityType == entityType)
                .ToListAsync();

            var def = new CustomFieldDefinition
            {
                EntityType = entityType,
                FieldKey = fieldKey,
                FieldLabel = fieldLabel,
                FieldType = fieldType,
                ChoiceOptions = choiceOptions ?? string.Empty,
                DisplayOrder = definitions.Count == 0 ? 1 : definitions.Max(d => d.DisplayOrder) + 1,
                IsActive = true
            };

            await _databaseService.Connection.InsertAsync(def);
            return def;
        }
    }
}
