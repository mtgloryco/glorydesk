using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels;

public partial class ReturnsViewModel : ViewModelBase
{
    private readonly ReturnsService _returnsService;
    private readonly InventoryService _inventoryService;
    private readonly SalesOrderService? _salesOrderService;
    private readonly PurchaseOrderService? _purchaseOrderService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _returnType = "Customer Return";
    [ObservableProperty] private Supplier? _selectedSupplier;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private string _reason = "";
    [ObservableProperty] private string _condition = "Resaleable";
    [ObservableProperty] private decimal _refundAmount;
    [ObservableProperty] private CreditNoteDisplayRow? _selectedCreditNote;
    [ObservableProperty] private DebitNoteDisplayRow? _selectedDebitNote;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _applyToSalesOrderId;
    [ObservableProperty] private decimal _applyCreditAmount;
    [ObservableProperty] private int _applyToPurchaseOrderId;
    [ObservableProperty] private decimal _applyDebitAmount;

    public ObservableCollection<string> ReturnTypes { get; } = new() { "Customer Return", "Supplier Return" };
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<CustomerReturn> RecentReturns { get; } = new();
    public ObservableCollection<SupplierReturn> RecentSupplierReturns { get; } = new();
    public ObservableCollection<CreditNoteDisplayRow> CreditNotes { get; } = new();
    public ObservableCollection<DebitNoteDisplayRow> DebitNotes { get; } = new();

    public bool IsSupplierReturn => ReturnType == "Supplier Return";
    public bool IsCustomerReturn => ReturnType == "Customer Return";
    public string RefundAmountLabel => IsSupplierReturn ? "Credit Amount" : "Refund Amount";

    partial void OnReturnTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsSupplierReturn));
        OnPropertyChanged(nameof(IsCustomerReturn));
        OnPropertyChanged(nameof(RefundAmountLabel));
        UpdateSuggestedRefund();
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        UpdateSuggestedRefund();
    }

    partial void OnQuantityChanged(int value)
    {
        UpdateSuggestedRefund();
    }

    private void UpdateSuggestedRefund()
    {
        if (SelectedProduct == null || Quantity <= 0) return;
        RefundAmount = IsSupplierReturn
            ? Quantity * SelectedProduct.Cost
            : Quantity * SelectedProduct.Price;
    }

    public ReturnsViewModel(
        ReturnsService returnsService,
        InventoryService inventoryService,
        SalesOrderService? salesOrderService = null,
        PurchaseOrderService? purchaseOrderService = null)
    {
        _returnsService = returnsService;
        _inventoryService = inventoryService;
        _salesOrderService = salesOrderService;
        _purchaseOrderService = purchaseOrderService;
        _ = LoadInitialData();
    }

    private readonly System.Threading.SemaphoreSlim _initLock = new(1, 1);

    public async Task LoadInitialData()
    {
        await _initLock.WaitAsync();
        try
        {
            IsLoading = true;
            Suppliers.Clear();
            var suppliers = await _returnsService.GetSuppliersAsync();
            foreach (var s in suppliers) Suppliers.Add(s);

            Products.Clear();
            var products = await _inventoryService.GetAllProductsAsync();
            foreach (var p in products) Products.Add(p);

            RecentReturns.Clear();
            var returns = await _returnsService.GetCustomerReturnsAsync(DateTime.Now.AddDays(-30), DateTime.Now);
            foreach (var r in returns) RecentReturns.Add(r);

            RecentSupplierReturns.Clear();
            var supplierReturns = await _returnsService.GetSupplierReturnsAsync(DateTime.Now.AddDays(-30), DateTime.Now);
            foreach (var r in supplierReturns) RecentSupplierReturns.Add(r);

            CreditNotes.Clear();
            var creditRows = await _returnsService.GetCreditNoteDisplayRowsAsync();
            foreach (var row in creditRows) CreditNotes.Add(row);

            DebitNotes.Clear();
            var debitRows = await _returnsService.GetDebitNoteDisplayRowsAsync();
            foreach (var row in debitRows) DebitNotes.Add(row);
        }
        finally
        {
            IsLoading = false;
            _initLock.Release();
        }
    }

    [RelayCommand]
    public async Task ProcessReturn()
    {
        if (SelectedProduct == null || Quantity <= 0)
        {
            StatusMessage = "Please select a product and specify a positive quantity.";
            return;
        }

        try
        {
            if (IsSupplierReturn)
            {
                var supplierId = SelectedSupplier?.Id ?? 1;
                var supRet = new SupplierReturn
                {
                    SupplierId = supplierId,
                    ProductId = SelectedProduct.Id,
                    Quantity = Quantity,
                    Reason = string.IsNullOrWhiteSpace(Reason) ? "Supplier Return" : Reason,
                    CreditAmount = RefundAmount > 0 ? RefundAmount : Quantity * SelectedProduct.Cost,
                    ProcessedByUsername = UserSession.CurrentUser?.Username ?? "System",
                    ReturnDate = DateTime.Now,
                    ReturnNumber = $"RET-SUP-{DateTime.Now:yyyyMMddHHmmss}"
                };

                await _returnsService.ProcessSupplierReturnAsync(supRet);
                Quantity = 0;
                Reason = "";
                RefundAmount = 0;
                StatusMessage = $"Supplier Return {supRet.ReturnNumber} processed successfully.";
                await LoadInitialData();
                return;
            }

            var ret = new CustomerReturn
            {
                ProductId = SelectedProduct.Id,
                Quantity = Quantity,
                Reason = Reason,
                Condition = Condition,
                RefundAmount = RefundAmount > 0 ? RefundAmount : Quantity * SelectedProduct.Price,
                ProcessedByUsername = UserSession.CurrentUser?.Username ?? "System",
                ReturnDate = DateTime.Now,
                ReturnNumber = $"RET-{DateTime.Now:yyyyMMddHHmmss}"
            };

            await _returnsService.ProcessCustomerReturnAsync(ret);
            Quantity = 0;
            Reason = "";
            RefundAmount = 0;
            StatusMessage = $"Return {ret.ReturnNumber} processed successfully.";
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error processing return: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ApplyCreditNote()
    {
        if (SelectedCreditNote == null)
        {
            StatusMessage = "Select a credit note first.";
            return;
        }

        try
        {
            var amount = ApplyCreditAmount > 0 ? ApplyCreditAmount : SelectedCreditNote.RemainingAmount;
            var soId = ApplyToSalesOrderId > 0
                ? ApplyToSalesOrderId
                : SelectedCreditNote.Note.SalesOrderId ?? 0;
            if (soId <= 0)
            {
                StatusMessage = "Enter the sales order Id to apply against.";
                return;
            }

            await _returnsService.ApplyCreditNoteAsync(
                SelectedCreditNote.Note.Id,
                soId,
                amount,
                UserSession.CurrentUser?.Username ?? "System");
            StatusMessage = $"Applied {amount:N2} credit to SO #{soId}.";
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ApplyDebitNote()
    {
        if (SelectedDebitNote == null)
        {
            StatusMessage = "Select a debit note first.";
            return;
        }

        try
        {
            var amount = ApplyDebitAmount > 0 ? ApplyDebitAmount : SelectedDebitNote.RemainingAmount;
            var poId = ApplyToPurchaseOrderId > 0
                ? ApplyToPurchaseOrderId
                : SelectedDebitNote.Note.PurchaseOrderId ?? 0;
            if (poId <= 0)
            {
                StatusMessage = "Enter the purchase order Id to apply against.";
                return;
            }

            await _returnsService.ApplyDebitNoteAsync(
                SelectedDebitNote.Note.Id,
                poId,
                amount,
                UserSession.CurrentUser?.Username ?? "System");
            StatusMessage = $"Applied {amount:N2} debit to PO #{poId}.";
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task PrintCreditNote(CreditNoteDisplayRow? row)
    {
        row ??= SelectedCreditNote;
        if (row == null) return;

        var path = await WriteNoteFileAsync(
            row.DocumentNumber,
            $"Credit Note: {row.DocumentNumber}",
            $"Customer: {row.CustomerName}",
            $"Linked: {row.LinkedDocument}",
            $"Return: {row.LinkedReturnNumber}",
            $"Amount: {row.Amount:N2}",
            $"Date: {row.IssueDate:yyyy-MM-dd}",
            $"Reason: {row.Reason}",
            $"Status: {row.Status}",
            $"Created by: {row.CreatedBy}");

        StatusMessage = $"Saved credit note to {path}";
    }

    [RelayCommand]
    private async Task PrintDebitNote(DebitNoteDisplayRow? row)
    {
        row ??= SelectedDebitNote;
        if (row == null) return;

        var path = await WriteNoteFileAsync(
            row.DocumentNumber,
            $"Debit Note: {row.DocumentNumber}",
            $"Supplier: {row.SupplierName}",
            $"Linked: {row.LinkedDocument}",
            $"Return: {row.LinkedReturnNumber}",
            $"Amount: {row.Amount:N2}",
            $"Date: {row.IssueDate:yyyy-MM-dd}",
            $"Reason: {row.Reason}",
            $"Status: {row.Status}",
            $"Created by: {row.CreatedBy}");

        StatusMessage = $"Saved debit note to {path}";
    }

    private static async Task<string> WriteNoteFileAsync(string docNumber, params string[] lines)
    {
        var dir = AppPaths.EnsureDocumentsSubfolder("Notes");
        Directory.CreateDirectory(dir);
        var safeName = docNumber.Replace('/', '-').Replace('\\', '-');
        var path = Path.Combine(dir, $"{safeName}.txt");
        await File.WriteAllTextAsync(path, string.Join(Environment.NewLine, lines));
        return path;
    }
}
