using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class AccountLedgerTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private AccountingReportService _accountingReportService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _accountingReportService = new AccountingReportService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    private async Task<JournalEntry> PostEntryAsync(int accountId, decimal debit, decimal credit, DateTime date, string label)
    {
        var journal = await _db.Connection.Table<Journal>().FirstAsync();
        var entry = new JournalEntry
        {
            EntryNumber = $"TEST/{Guid.NewGuid():N}",
            JournalId = journal.Id,
            Date = date,
            Reference = label,
            State = "Posted"
        };
        await _db.Connection.InsertAsync(entry);
        await _db.Connection.InsertAsync(new JournalLine
        {
            JournalEntryId = entry.Id,
            AccountId = accountId,
            Label = label,
            Debit = debit,
            Credit = credit
        });
        return entry;
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ComputesRunningBalance_ForDebitNormalAccount()
    {
        var cash = await _db.Connection.Table<Account>().Where(a => a.Code == "101000").FirstAsync();

        await PostEntryAsync(cash.Id, 500, 0, DateTime.Today.AddDays(-2), "Opening deposit");
        await PostEntryAsync(cash.Id, 0, 200, DateTime.Today.AddDays(-1), "Cash withdrawal");

        var result = await _accountingReportService.GetAccountLedgerAsync(cash.Id);

        Assert.Equal(0, result.OpeningBalance);
        Assert.Equal(300, result.ClosingBalance);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(500, result.Lines[0].RunningBalance);
        Assert.Equal(300, result.Lines[1].RunningBalance);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ComputesRunningBalance_ForCreditNormalAccount()
    {
        var ap = await _db.Connection.Table<Account>().Where(a => a.Code == "201000").FirstAsync();

        await PostEntryAsync(ap.Id, 0, 1000, DateTime.Today.AddDays(-2), "Vendor bill");
        await PostEntryAsync(ap.Id, 400, 0, DateTime.Today.AddDays(-1), "Vendor payment");

        var result = await _accountingReportService.GetAccountLedgerAsync(ap.Id);

        // Liability accounts increase on the credit side, not the debit side.
        Assert.Equal(1000, result.Lines[0].RunningBalance);
        Assert.Equal(600, result.Lines[1].RunningBalance);
        Assert.Equal(600, result.ClosingBalance);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_FoldsLinesBeforeFromDate_IntoOpeningBalance()
    {
        var cash = await _db.Connection.Table<Account>().Where(a => a.Code == "101000").FirstAsync();

        await PostEntryAsync(cash.Id, 500, 0, DateTime.Today.AddDays(-10), "Old deposit");
        await PostEntryAsync(cash.Id, 100, 0, DateTime.Today.AddDays(-1), "Recent deposit");

        var result = await _accountingReportService.GetAccountLedgerAsync(cash.Id, from: DateTime.Today.AddDays(-5));

        Assert.Equal(500, result.OpeningBalance);
        Assert.Single(result.Lines);
        Assert.Equal(600, result.Lines[0].RunningBalance);
        Assert.Equal(600, result.ClosingBalance);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ExcludesLinesAfterToDate()
    {
        var cash = await _db.Connection.Table<Account>().Where(a => a.Code == "101000").FirstAsync();

        await PostEntryAsync(cash.Id, 500, 0, DateTime.Today.AddDays(-2), "In range");
        await PostEntryAsync(cash.Id, 100, 0, DateTime.Today.AddDays(2), "Out of range");

        var result = await _accountingReportService.GetAccountLedgerAsync(cash.Id, to: DateTime.Today);

        Assert.Single(result.Lines);
        Assert.Equal(500, result.ClosingBalance);
    }
}
