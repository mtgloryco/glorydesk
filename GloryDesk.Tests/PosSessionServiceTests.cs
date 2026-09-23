using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class PosSessionServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private PosSessionService _sessionService = null!;
    private int _cashMethodId;
    private int _momoMethodId;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
        _sessionService = new PosSessionService(_db);

        var journal = await _db.Connection.Table<Journal>().FirstAsync();
        var cash = new PosPaymentMethod { Name = "Cash", JournalId = journal.Id };
        await _db.Connection.InsertAsync(cash);
        _cashMethodId = cash.Id;

        var momo = new PosPaymentMethod { Name = "Mobile Money", JournalId = journal.Id };
        await _db.Connection.InsertAsync(momo);
        _momoMethodId = momo.Id;
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task OpenSessionAsync_CreatesSessionWithOpeningBalancesPerMethod()
    {
        var session = await _sessionService.OpenSessionAsync(
            new Dictionary<int, decimal> { [_cashMethodId] = 50000, [_momoMethodId] = 0 },
            "cashier1");

        Assert.Equal("Open", session.Status);
        Assert.Equal("cashier1", session.OpenedByUsername);

        var balances = await _db.Connection.Table<PosSessionBalance>().Where(b => b.PosSessionId == session.Id).ToListAsync();
        Assert.Equal(2, balances.Count);
        Assert.Equal(50000, balances.First(b => b.PosPaymentMethodId == _cashMethodId).OpeningBalance);
    }

    [Fact]
    public async Task OpenSessionAsync_ThrowsWhenAnotherSessionIsAlreadyOpen()
    {
        await _sessionService.OpenSessionAsync(new Dictionary<int, decimal> { [_cashMethodId] = 0 }, "cashier1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sessionService.OpenSessionAsync(new Dictionary<int, decimal> { [_cashMethodId] = 0 }, "cashier2"));
    }

    [Fact]
    public async Task GetSessionSummaryAsync_ComputesExpectedBalance_FromSalesAndCashMovements()
    {
        var session = await _sessionService.OpenSessionAsync(
            new Dictionary<int, decimal> { [_cashMethodId] = 10000 },
            "cashier1");

        var customer = new Customer { Name = "Walk-in" };
        await _db.Connection.InsertAsync(customer);

        var order = new SalesOrder
        {
            SONumber = "POS-1",
            CustomerId = customer.Id,
            Status = "Delivered",
            IsPosSale = true,
            PosSessionId = session.Id,
            TotalAmount = 5000,
            OrderDate = DateTime.Now
        };
        await _db.Connection.InsertAsync(order);
        await _db.Connection.InsertAsync(new PosSalePayment { SalesOrderId = order.Id, PosPaymentMethodId = _cashMethodId, Amount = 5000 });

        await _sessionService.RecordCashMovementAsync(session.Id, _cashMethodId, "Out", 2000, "Cash drop to safe", "cashier1");
        await _sessionService.RecordCashMovementAsync(session.Id, _cashMethodId, "In", 1000, "Float top-up", "cashier1");

        var summary = await _sessionService.GetSessionSummaryAsync(session.Id);
        var cashRow = summary.Balances.Single(b => b.PosPaymentMethodId == _cashMethodId);

        // Expected = Opening (10000) + Sales (5000) + CashIn (1000) - CashOut (2000) = 14000
        Assert.Equal(14000, cashRow.ExpectedBalance);
        Assert.Single(summary.Orders);
        Assert.Equal(5000, summary.OrdersTotal);
    }

    [Fact]
    public async Task CloseSessionAsync_RecordsCountedBalancesAndComputesDifference()
    {
        var session = await _sessionService.OpenSessionAsync(
            new Dictionary<int, decimal> { [_cashMethodId] = 10000 },
            "cashier1");

        await _sessionService.CloseSessionAsync(session.Id, new Dictionary<int, decimal> { [_cashMethodId] = 9500 }, "cashier1", "Short by 500");

        var closed = await _db.Connection.FindAsync<PosSession>(session.Id);
        Assert.Equal("Closed", closed.Status);
        Assert.Equal("cashier1", closed.ClosedByUsername);
        Assert.NotNull(closed.ClosedAt);

        var summary = await _sessionService.GetSessionSummaryAsync(session.Id);
        var cashRow = summary.Balances.Single(b => b.PosPaymentMethodId == _cashMethodId);

        // Expected = Opening only (10000), Counted = 9500 -> Difference = -500
        Assert.Equal(10000, cashRow.ExpectedBalance);
        Assert.Equal(9500, cashRow.CountedClosingBalance);
        Assert.Equal(-500, cashRow.Difference);
    }

    [Fact]
    public async Task CloseSessionAsync_ThrowsWhenAlreadyClosed()
    {
        var session = await _sessionService.OpenSessionAsync(new Dictionary<int, decimal> { [_cashMethodId] = 0 }, "cashier1");
        await _sessionService.CloseSessionAsync(session.Id, new Dictionary<int, decimal> { [_cashMethodId] = 0 }, "cashier1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sessionService.CloseSessionAsync(session.Id, new Dictionary<int, decimal> { [_cashMethodId] = 0 }, "cashier1"));
    }
}
