using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using InventoryManagementSystem.UI.ViewModels;

namespace InventoryManagementSystem.UI.Views
{
    public partial class POSView : UserControl
    {
        public POSView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not POSViewModel vm)
            {
                return;
            }

            await vm.ScanBarcodeCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }
}
