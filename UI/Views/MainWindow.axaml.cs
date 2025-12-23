using System;
using Avalonia.Controls;
using Avalonia.Styling;

namespace InventoryManagementSystem.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (OperatingSystem.IsWindows())
        {
           // Apply padding when maximized to prevent content from being cut off on Windows
           var style = new Style(x => x.OfType<Window>().Class(":maximized"));
           style.Setters.Add(new Setter(Window.PaddingProperty, new Avalonia.Thickness(8)));
           Styles.Add(style);
        }
    }
}