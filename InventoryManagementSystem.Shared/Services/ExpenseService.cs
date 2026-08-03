using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using SQLite;

namespace InventoryManagementSystem.Services
{
    public class ExpenseService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditService? _auditService;

        public static readonly string[] Categories =
        {
            "Rent", "Utilities", "Salaries", "Marketing", "Maintenance", "Supplies", "Transport", "General", "Other"
        };

        public ExpenseService(DatabaseService databaseService, AuditService? auditService = null)
        {
            _databaseService = databaseService;
            _auditService = auditService;
        }

        public async Task<List<Expense>> GetAllExpensesAsync()
        {
            return await _databaseService.Connection.Table<Expense>()
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<Expense> RecordExpenseAsync(Expense expense, string username)
        {
            if (expense.Amount <= 0)
            {
                throw new InvalidOperationException("Expense amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(expense.Description))
            {
                throw new InvalidOperationException("A description is required.");
            }

            expense.ExpenseNumber = await GenerateExpenseNumberAsync();
            expense.CreatedByUsername = username;
            if (expense.Date == default)
            {
                expense.Date = DateTime.Now;
            }

            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                conn.Insert(expense);
                PostExpenseJournal(conn, expense);
            });

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "ExpenseRecorded", "Expense", expense.Id, expense);
            }

            return expense;
        }

        // Debits the category's expense account and credits the Cash/Bank account paid from -
        // the same "Dr expense / Cr liquidity" shape as PaymentService.PostPaymentJournalEntry,
        // since an expense here is recorded as already paid (there is no separate AP step for it).
        private static void PostExpenseJournal(SQLiteConnection conn, Expense expense)
        {
            var journalType = expense.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : "Bank";
            var journal = conn.Table<Journal>().FirstOrDefault(j => j.Type == journalType)
                ?? conn.Table<Journal>().FirstOrDefault(j => j.Type == "Bank");
            if (journal == null) return;

            var entryCount = conn.Table<JournalEntry>().Count(e => e.JournalId == journal.Id);
            var entry = new JournalEntry
            {
                EntryNumber = $"{journal.SequencePrefix}/{DateTime.Now.Year}/{(entryCount + 1):D5}",
                JournalId = journal.Id,
                Date = expense.Date,
                Reference = $"Expense {expense.ExpenseNumber} - {expense.Description}",
                State = "Posted"
            };
            conn.Insert(entry);

            var expenseAccountCode = ExpenseAccountCode(expense.Category);
            var expenseAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == expenseAccountCode);
            int expenseAccountId = expenseAccount?.Id ?? 17;

            var cashAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == "101000");
            var bankAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == "102000");
            int liquidityAccountId = expense.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                ? (cashAccount?.Id ?? journal.DefaultAccountId ?? 1)
                : (bankAccount?.Id ?? journal.DefaultAccountId ?? 2);

            var label = $"{expense.Category} - {expense.Description}";

            conn.Insert(new JournalLine
            {
                JournalEntryId = entry.Id,
                AccountId = expenseAccountId,
                Label = label,
                Debit = expense.Amount,
                Credit = 0
            });
            conn.Insert(new JournalLine
            {
                JournalEntryId = entry.Id,
                AccountId = liquidityAccountId,
                Label = label,
                Debit = 0,
                Credit = expense.Amount
            });
        }

        private static string ExpenseAccountCode(string category) => category switch
        {
            "Rent" => "512000",
            "Utilities" => "513000",
            "Salaries" => "514000",
            "Marketing" => "515000",
            "Maintenance" => "516000",
            "Supplies" => "517000",
            "Transport" => "518000",
            "Other" => "590000",
            _ => "511000" // General
        };

        private async Task<string> GenerateExpenseNumberAsync()
        {
            var year = DateTime.Now.Year;
            var count = await _databaseService.Connection.Table<Expense>().CountAsync();
            return $"EXP-{year}-{(count + 1):D5}";
        }

        public async Task<Dictionary<string, decimal>> GetTotalsByCategoryAsync(DateTime? from = null, DateTime? to = null)
        {
            var all = await GetAllExpensesAsync();
            if (from.HasValue) all = all.Where(e => e.Date >= from.Value).ToList();
            if (to.HasValue) all = all.Where(e => e.Date <= to.Value).ToList();

            return all.GroupBy(e => e.Category).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
        }
    }
}
