using Avalonia.Controls;
using Avalonia.Input;
using InventoryManagementSystem.UI.ViewModels;

namespace InventoryManagementSystem.UI.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
    }

    private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not InventoryViewModel vm)
        {
            return;
        }

        await vm.ScanBarcodeCommand.ExecuteAsync(null);
        e.Handled = true;
    }
}
