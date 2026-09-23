using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class PosOrderReportTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private InventoryService _inventory = null!;
    private SalesOrderService _sales = null!;
    private int _cashMethodId;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();

        var license = new LicenseService(_db, new HardwareIdService(), new LicenseCryptoService());
        await license.InitializeAsync();
        var audit = new AuditService(_db);
        _inventory = new InventoryService(_db, license, audit);
        _sales = new SalesOrderService(_db, _inventory);

        var journal = await _db.Connection.Table<Journal>().FirstAsync();
        var cash = new PosPaymentMethod { Name = "Cash", JournalId = journal.Id };
        await _db.Connection.InsertAsync(cash);
        _cashMethodId = cash.Id;
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    private async Task<int> SeedProductAsync(string name, string category)
    {
        var p = new Product { Name = name, Category = category, Price = 100, AvailableInPOS = true };
        await _db.Connection.InsertAsync(p);
        return p.Id;
    }

    private async Task<int> SeedOrderAsync(DateTime date, decimal total, string cashier,
        IEnumerable<(int productId, int qty, decimal unitPrice)> lines)
    {
        var order = new SalesOrder
        {
            SONumber = $"POS-{Guid.NewGuid():N}".Substring(0, 12),
            IsPosSale = true,
            Status = "Delivered",
            OrderDate = date,
            TotalAmount = total,
            CreatedByUsername = cashier,
            PosPaymentMethodId = _cashMethodId,
        };
        await _db.Connection.InsertAsync(order);
        foreach (var (productId, qty, unitPrice) in lines)
        {
            await _db.Connection.InsertAsync(new SalesOrderItem
            {
                SalesOrderId = order.Id,
                ProductId = productId,
                QuantityOrdered = qty,
                UnitPrice = unitPrice,
            });
        }
        return order.Id;
    }

    [Fact]
    public async Task OrderCount_DeDuplicatesLinesOfTheSameOrder()
    {
        var bread = await SeedProductAsync("Bread", "Bakery");
        var soda = await SeedProductAsync("Soda", "Beverages");
        await SeedOrderAsync(new DateTime(2026, 7, 10), 500, "amelia",
            new[] { (bread, 2, 100m), (soda, 3, 100m) });

        var rows = await _sales.GetPosOrderReportRowsAsync(null, null);
        var pivot = PosReportPivot.Build(rows, Array.Empty<PosDimension>(), Array.Empty<PosDimension>());
        var total = pivot.Root.Values[PosReportPivot.GrandColumnKey];

        Assert.Equal(1, total.OrderCount);
        Assert.Equal(5, total.Quantity);
        Assert.Equal(500m, total.LineTotal);
    }

    [Fact]
    public async Task GroupByCategory_PutsBlankCategoryUnderNone_AndSplitsMeasures()
    {
        var bread = await SeedProductAsync("Bread", "Bakery");
        var mystery = await SeedProductAsync("Mystery", "");
        await SeedOrderAsync(new DateTime(2026, 7, 10), 300, "amelia",
            new[] { (bread, 1, 100m) });
        await SeedOrderAsync(new DateTime(2026, 7, 11), 400, "amelia",
            new[] { (mystery, 2, 100m) });

        var rows = await _sales.GetPosOrderReportRowsAsync(null, null);
        var pivot = PosReportPivot.Build(rows, new[] { PosDimension.ProductCategory }, Array.Empty<PosDimension>());

        var labels = pivot.Root.Children.Select(c => c.Label).ToList();
        Assert.Contains("Bakery", labels);
        Assert.Contains("None", labels);

        var none = pivot.Root.Children.Single(c => c.Label == "None");
        Assert.Equal(1, none.Values[PosReportPivot.GrandColumnKey].OrderCount);
        Assert.Equal(200m, none.Values[PosReportPivot.GrandColumnKey].LineTotal);
    }

    [Fact]
    public async Task ColumnGroupingByMonth_ProducesSortedMonthColumnsPlusGrandTotal()
    {
        var bread = await SeedProductAsync("Bread", "Bakery");
        await SeedOrderAsync(new DateTime(2026, 6, 5), 100, "amelia", new[] { (bread, 1, 100m) });
        await SeedOrderAsync(new DateTime(2026, 7, 5), 100, "amelia", new[] { (bread, 1, 100m) });

        var rows = await _sales.GetPosOrderReportRowsAsync(null, null);
        var pivot = PosReportPivot.Build(rows,
            new[] { PosDimension.ProductCategory },
            new[] { PosDimension.OrderDateMonth });

        Assert.Equal(new[] { "2026-06", "2026-07", PosReportPivot.GrandColumnKey }, pivot.ColumnKeys.ToArray());

        var bakery = pivot.Root.Children.Single(c => c.Label == "Bakery");
        Assert.Equal(1, bakery.Values["2026-06"].OrderCount);
        Assert.Equal(2, bakery.Values[PosReportPivot.GrandColumnKey].OrderCount);
    }

    [Fact]
    public async Task DateRangeFilter_ExcludesOrdersOutsideWindow()
    {
        var bread = await SeedProductAsync("Bread", "Bakery");
        await SeedOrderAsync(new DateTime(2026, 1, 1), 100, "amelia", new[] { (bread, 1, 100m) });
        await SeedOrderAsync(new DateTime(2026, 7, 1), 100, "amelia", new[] { (bread, 1, 100m) });

        var rows = await _sales.GetPosOrderReportRowsAsync(new DateTime(2026, 6, 1), new DateTime(2026, 7, 31));

        Assert.Single(rows);
        Assert.Equal(new DateTime(2026, 7, 1), rows[0].OrderDate);
    }
}
