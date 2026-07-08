using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class IndustryTemplate
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> DefaultCategories { get; set; } = new();
        public List<(string Name, double Quantity, string ReferenceUnit)> DefaultUnits { get; set; } = new();
        public Dictionary<string, string> TerminologyOverrides { get; set; } = new();
        public Dictionary<string, bool> EnabledModules { get; set; } = new();
        public List<(string EntityType, string FieldKey, string FieldLabel, string FieldType, string? ChoiceOptions)> SuggestedCustomFields { get; set; } = new();
    }

    public class IndustryTemplateService
    {
        private readonly DatabaseService _dbService;

        public IndustryTemplateService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public static List<IndustryTemplate> GetAvailableTemplates()
        {
            return new List<IndustryTemplate>
            {
                new IndustryTemplate
                {
                    Key = "generic_retail",
                    DisplayName = "Generic Retail / General Store",
                    Description = "A general-purpose retail setup with point-of-sale enabled and no industry-specific fields.",
                    DefaultCategories = new List<string> { "General", "Beverages", "Household", "Electronics" },
                    DefaultUnits = new List<(string, double, string)>
                    {
                        ("Pcs", 1.0, ""),
                        ("Box", 12.0, "Pcs")
                    },
                    TerminologyOverrides = new Dictionary<string, string>(),
                    EnabledModules = new Dictionary<string, bool>
                    {
                        { "POS", true },
                        { "Manufacturing", false },
                        { "BOM", false },
                        { "Expiry", false },
                        { "MultiLocation", false }
                    },
                    SuggestedCustomFields = new List<(string, string, string, string, string?)>()
                },
                new IndustryTemplate
                {
                    Key = "farm_supply",
                    DisplayName = "Farm Supply / Agro-Input Store",
                    Description = "For agro-dealers selling seeds, fertilizers, pesticides and farm tools, tracking crop season and active ingredients.",
                    DefaultCategories = new List<string> { "Seeds", "Fertilizers", "Pesticides", "Farm Tools" },
                    DefaultUnits = new List<(string, double, string)>
                    {
                        ("Pcs", 1.0, ""),
                        ("kg", 1.0, ""),
                        ("Bag (50kg)", 50.0, "kg")
                    },
                    TerminologyOverrides = new Dictionary<string, string>
                    {
                        { "Customer", "Farmer" }
                    },
                    EnabledModules = new Dictionary<string, bool>
                    {
                        { "POS", true },
                        { "Manufacturing", false },
                        { "BOM", false },
                        { "Expiry", true },
                        { "MultiLocation", false }
                    },
                    SuggestedCustomFields = new List<(string, string, string, string, string?)>
                    {
                        ("Product", "CropSeason", "Crop Season", "Text", null),
                        ("Product", "ActiveIngredient", "Active Ingredient", "Text", null)
                    }
                },
                new IndustryTemplate
                {
                    Key = "coffee_agro_processing",
                    DisplayName = "Coffee & Agro-Processing",
                    Description = "For coffee washing stations and agro-processors tracking lots, grade, moisture and origin through manufacturing.",
                    DefaultCategories = new List<string> { "Green Beans", "Roasted Beans", "Parchment" },
                    DefaultUnits = new List<(string, double, string)>
                    {
                        ("kg", 1.0, ""),
                        ("Bag (60kg)", 60.0, "kg")
                    },
                    TerminologyOverrides = new Dictionary<string, string>
                    {
                        { "Product", "Lot" }
                    },
                    EnabledModules = new Dictionary<string, bool>
                    {
                        { "POS", false },
                        { "Manufacturing", true },
                        { "BOM", true },
                        { "Expiry", true },
                        { "MultiLocation", false }
                    },
                    SuggestedCustomFields = new List<(string, string, string, string, string?)>
                    {
                        ("Product", "Grade", "Grade", "Choice", "A,AA,AB,PB,C"),
                        ("Product", "MoisturePercent", "Moisture %", "Number", null),
                        ("Product", "Origin", "Origin", "Text", null)
                    }
                },
                new IndustryTemplate
                {
                    Key = "wholesale_distribution",
                    DisplayName = "Wholesale Distribution",
                    Description = "For distributors moving goods in bulk across multiple warehouses/locations.",
                    DefaultCategories = new List<string> { "General Merchandise", "Bulk Goods" },
                    DefaultUnits = new List<(string, double, string)>
                    {
                        ("Pcs", 1.0, ""),
                        ("Carton", 24.0, "Pcs"),
                        ("Pallet", 48.0, "Carton")
                    },
                    TerminologyOverrides = new Dictionary<string, string>(),
                    EnabledModules = new Dictionary<string, bool>
                    {
                        { "POS", false },
                        { "Manufacturing", false },
                        { "BOM", false },
                        { "Expiry", false },
                        { "MultiLocation", true }
                    },
                    SuggestedCustomFields = new List<(string, string, string, string, string?)>()
                },
                new IndustryTemplate
                {
                    Key = "light_manufacturing",
                    DisplayName = "Light Manufacturing",
                    Description = "For small factories/workshops assembling finished goods from raw materials via bills of materials.",
                    DefaultCategories = new List<string> { "Raw Materials", "Work in Progress", "Finished Goods" },
                    DefaultUnits = new List<(string, double, string)>
                    {
                        ("Pcs", 1.0, ""),
                        ("kg", 1.0, ""),
                        ("m", 1.0, "")
                    },
                    TerminologyOverrides = new Dictionary<string, string>(),
                    EnabledModules = new Dictionary<string, bool>
                    {
                        { "POS", false },
                        { "Manufacturing", true },
                        { "BOM", true },
                        { "Expiry", false },
                        { "MultiLocation", false }
                    },
                    SuggestedCustomFields = new List<(string, string, string, string, string?)>()
                },
                new IndustryTemplate
                {
                    Key = "services",
                    DisplayName = "Services Business",
                    Description = "For service-oriented businesses focused on sales orders and invoicing rather than physical stock.",
                    DefaultCategories = new List<string> { "Services" },
                    DefaultUnits = new List<(string, double, string)>
                    {
                        ("Hour", 1.0, ""),
                        ("Session", 1.0, "")
                    },
                    TerminologyOverrides = new Dictionary<string, string>
                    {
                        { "Product", "Service" }
                    },
                    EnabledModules = new Dictionary<string, bool>
                    {
                        { "POS", false },
                        { "Manufacturing", false },
                        { "BOM", false },
                        { "Expiry", false },
                        { "MultiLocation", false }
                    },
                    SuggestedCustomFields = new List<(string, string, string, string, string?)>()
                }
            };
        }

        public async Task ApplyTemplateAsync(string templateKey, SettingsService settingsService, CustomFieldService customFieldService)
        {
            var template = GetAvailableTemplates().FirstOrDefault(t => t.Key == templateKey);
            if (template == null)
            {
                return;
            }

            var existingCategories = await _dbService.Connection.Table<Category>().ToListAsync();
            foreach (var categoryName in template.DefaultCategories)
            {
                if (!existingCategories.Any(c => c.Name.Equals(categoryName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    await _dbService.Connection.InsertAsync(new Category { Name = categoryName });
                }
            }

            var existingUnits = await _dbService.Connection.Table<ProductUnit>().ToListAsync();
            foreach (var unit in template.DefaultUnits)
            {
                if (!existingUnits.Any(u => u.Name.Equals(unit.Name, System.StringComparison.OrdinalIgnoreCase)))
                {
                    await _dbService.Connection.InsertAsync(new ProductUnit
                    {
                        Name = unit.Name,
                        Quantity = unit.Quantity,
                        ReferenceUnit = unit.ReferenceUnit
                    });
                }
            }

            foreach (var field in template.SuggestedCustomFields)
            {
                await customFieldService.EnsureDefinitionAsync(field.EntityType, field.FieldKey, field.FieldLabel, field.FieldType, field.ChoiceOptions);
            }

            foreach (var term in template.TerminologyOverrides)
            {
                if (!settingsService.CurrentSettings.TerminologyOverrides.ContainsKey(term.Key))
                {
                    settingsService.CurrentSettings.TerminologyOverrides[term.Key] = term.Value;
                }
            }

            foreach (var module in template.EnabledModules)
            {
                settingsService.CurrentSettings.EnabledModules[module.Key] = module.Value;
            }

            settingsService.CurrentSettings.BusinessType = templateKey;
            settingsService.CurrentSettings.SetupCompleted = true;
            settingsService.SaveSettings();
        }
    }
}
