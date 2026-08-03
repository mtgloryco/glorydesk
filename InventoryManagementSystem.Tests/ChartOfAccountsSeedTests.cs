using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class ChartOfAccountsSeedTests
{
    [Fact]
    public async Task FreshInstall_SeedsFullChartOfAccounts()
    {
        var dbPath = TempFile.CreateDbPath();
        var db = new DatabaseService(dbPath);
        await db.InitializeAsync();

        var accounts = await db.Connection.Table<Account>().ToListAsync();
        var codes = accounts.Select(a => a.Code).ToList();

        Assert.Contains("101000", codes); // Cash on Hand
        Assert.Contains("102000", codes); // Bank Account
        Assert.Contains("111000", codes); // Accounts Receivable
        Assert.Contains("120000", codes); // Inventory Asset
        Assert.Contains("201000", codes); // Accounts Payable
        Assert.Contains("401000", codes); // Product Sales Revenue
        Assert.Contains("501000", codes); // COGS
        Assert.True(accounts.Count >= 20, $"Expected the full base chart of accounts, got only {accounts.Count} rows: {string.Join(",", codes)}");
    }
}
