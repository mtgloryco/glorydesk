using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace InventoryManagementSystem.UI.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public static readonly StringToBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                if (Color.TryParse(hex, out var color))
                {
                    return new SolidColorBrush(color);
                }
            }

            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
