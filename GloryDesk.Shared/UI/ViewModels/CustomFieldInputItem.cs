using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using InventoryManagementSystem.Domain;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class CustomFieldInputItem : ObservableObject
    {
        [ObservableProperty]
        private CustomFieldDefinition _definition = new();

        [ObservableProperty]
        private string _textValue = string.Empty;

        [ObservableProperty]
        private bool _boolValue;

        public bool IsText => Definition.FieldType == "Text";
        public bool IsNumber => Definition.FieldType == "Number";
        public bool IsDate => Definition.FieldType == "Date";
        public bool IsBoolean => Definition.FieldType == "Boolean";
        public bool IsChoice => Definition.FieldType == "Choice";

        public List<string> ChoiceOptionsList =>
            (Definition.ChoiceOptions ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        public decimal? NumberValue
        {
            get => decimal.TryParse(TextValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null;
            set => TextValue = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        public DateTimeOffset? DateValue
        {
            get => DateTimeOffset.TryParse(TextValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTimeOffset?)null;
            set => TextValue = value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
        }

        public CustomFieldInputItem()
        {
        }

        public CustomFieldInputItem(CustomFieldDefinition definition, string? initialValue)
        {
            Definition = definition;
            if (definition.FieldType == "Boolean")
            {
                BoolValue = string.Equals(initialValue, "true", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                TextValue = initialValue ?? string.Empty;
            }
        }

        partial void OnDefinitionChanged(CustomFieldDefinition value)
        {
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(IsNumber));
            OnPropertyChanged(nameof(IsDate));
            OnPropertyChanged(nameof(IsBoolean));
            OnPropertyChanged(nameof(IsChoice));
            OnPropertyChanged(nameof(ChoiceOptionsList));
        }

        partial void OnTextValueChanged(string value)
        {
            OnPropertyChanged(nameof(NumberValue));
            OnPropertyChanged(nameof(DateValue));
        }
    }
}
