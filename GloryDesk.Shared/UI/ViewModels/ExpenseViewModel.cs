using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels;

public partial class ExpenseViewModel : ViewModelBase
{
    private readonly ExpenseService _expenseService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private DateTimeOffset _date = DateTimeOffset.Now;
    [ObservableProperty] private string _category = "General";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _paymentMethod = "Cash";
    [ObservableProperty] private string _payeeName = "";
    [ObservableProperty] private string _reference = "";
    [ObservableProperty] private string _notes = "";

    public ObservableCollection<Expense> Expenses { get; } = new();
    public string[] Categories => ExpenseService.Categories;
    public string[] PaymentMethods => new[] { "Cash", "Bank" };

    public decimal TotalThisMonth { get; private set; }

    public ExpenseViewModel(ExpenseService expenseService)
    {
        _expenseService = expenseService;
        _ = LoadExpenses();
    }

    [RelayCommand]
    public async Task LoadExpenses()
    {
        IsLoading = true;
        Expenses.Clear();
        var all = await _expenseService.GetAllExpensesAsync();
        foreach (var e in all) Expenses.Add(e);

        var now = DateTime.Now;
        decimal total = 0;
        foreach (var e in all)
        {
            if (e.Date.Year == now.Year && e.Date.Month == now.Month)
            {
                total += e.Amount;
            }
        }
        TotalThisMonth = total;
        OnPropertyChanged(nameof(TotalThisMonth));

        IsLoading = false;
    }

    [RelayCommand]
    public async Task RecordExpense()
    {
        ErrorMessage = null;
        var expense = new Expense
        {
            Date = Date.DateTime,
            Category = Category,
            Description = Description,
            Amount = Amount,
            PaymentMethod = PaymentMethod,
            PayeeName = PayeeName,
            Reference = Reference,
            Notes = Notes
        };

        try
        {
            await _expenseService.RecordExpenseAsync(expense, UserSession.CurrentUser?.Username ?? "System");
            Description = "";
            Amount = 0;
            PayeeName = "";
            Reference = "";
            Notes = "";
            await LoadExpenses();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
