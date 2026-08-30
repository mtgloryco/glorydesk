using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using InventoryManagementSystem.UI.ViewModels;

namespace InventoryManagementSystem.UI.Views
{
    public partial class POSView : UserControl
    {
        public POSView()
        {
            InitializeComponent();
            AddHandler(TextInputEvent, OnGlobalTextInput, RoutingStrategies.Tunnel);
            AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);
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
            KeepScannerFocus();
        }

        /// <summary>
        /// Put the caret back in the product search after every scan so the cashier can keep
        /// scanning item after item without clicking. Any leftover text (a failed scan) is
        /// selected so the next scan overwrites it.
        /// </summary>
        private void KeepScannerFocus()
        {
            var box = this.FindControl<TextBox>("SearchBox");
            if (box == null)
            {
                return;
            }

            box.Focus();
            box.SelectAll();
        }

        /// <summary>
        /// A barcode scanner is just a fast keyboard, and the cashier is rarely focused in the
        /// search box when they pull the trigger - they might have just clicked a cart row or
        /// nothing at all. So: whenever no text field is actively focused (meaning nobody is
        /// deliberately typing somewhere else, like the customer or journal search boxes), route
        /// typed/scanned characters straight into the product search instead of dropping them.
        /// </summary>
        private void OnGlobalTextInput(object? sender, TextInputEventArgs e)
        {
            if (!CanCaptureAsBarcode(out var vm) || string.IsNullOrEmpty(e.Text))
            {
                return;
            }

            vm!.SearchText += e.Text;
            e.Handled = true;
        }

        private async void OnGlobalKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !CanCaptureAsBarcode(out var vm) || string.IsNullOrWhiteSpace(vm!.SearchText))
            {
                return;
            }

            await vm.ScanBarcodeCommand.ExecuteAsync(null);
            e.Handled = true;
            KeepScannerFocus();
        }

        private bool CanCaptureAsBarcode(out POSViewModel? vm)
        {
            vm = DataContext as POSViewModel;
            if (vm == null || !vm.IsSalesTabActive || vm.IsPaymentPanelVisible || vm.IsReceiptModalOpen || vm.IsCreateCustomerModalOpen)
            {
                return false;
            }

            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            return focused is not TextBox and not AutoCompleteBox and not NumericUpDown;
        }
    }
}
