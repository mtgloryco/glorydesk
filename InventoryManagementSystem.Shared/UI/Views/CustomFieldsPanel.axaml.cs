using System.Collections;
using Avalonia;
using Avalonia.Controls;

namespace InventoryManagementSystem.UI.Views;

public partial class CustomFieldsPanel : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<CustomFieldsPanel, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public CustomFieldsPanel()
    {
        InitializeComponent();
    }
}
