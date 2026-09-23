using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using InventoryManagementSystem.Domain;

namespace InventoryManagementSystem.Services
{
    /// <summary>Numbers the POS Reporting pivot can aggregate.</summary>
    public enum PosMeasure
    {
        OrderCount,
        ProductQuantity,
        Total,
    }

    /// <summary>Fields the POS Reporting pivot can group rows / columns by.</summary>
    public enum PosDimension
    {
        Cashier,
        PaymentMethod,
        Product,
        ProductCategory,
        Customer,
        OrderDateYear,
        OrderDateQuarter,
        OrderDateMonth,
        OrderDateDay,
        Session,
    }

    public static class PosDimensions
    {
        public static string Label(PosDimension d) => d switch
        {
            PosDimension.Cashier => "Cashier",
            PosDimension.PaymentMethod => "Payment Method",
            PosDimension.Product => "Product",
            PosDimension.ProductCategory => "Product Category",
            PosDimension.Customer => "Customer",
            PosDimension.OrderDateYear => "Order Date: Year",
            PosDimension.OrderDateQuarter => "Order Date: Quarter",
            PosDimension.OrderDateMonth => "Order Date: Month",
            PosDimension.OrderDateDay => "Order Date: Day",
            PosDimension.Session => "Session",
            _ => d.ToString(),
        };

        /// <summary>
        /// The bucket a row falls into for the given dimension. Date buckets return
        /// lexically-sortable keys ("2026-07", "2026-Q3") so chronological order is
        /// just an ascending string sort.
        /// </summary>
        public static string Key(PosDimension d, PosOrderReportRow r) => d switch
        {
            PosDimension.Cashier => Clean(r.Cashier),
            PosDimension.PaymentMethod => Clean(r.PaymentMethod),
            PosDimension.Product => Clean(r.ProductName),
            PosDimension.ProductCategory => Clean(r.ProductCategory),
            PosDimension.Customer => Clean(r.CustomerName),
            PosDimension.Session => Clean(r.SessionNumber),
            PosDimension.OrderDateYear => r.OrderDate.ToString("yyyy", CultureInfo.InvariantCulture),
            PosDimension.OrderDateQuarter => $"{r.OrderDate:yyyy}-Q{(r.OrderDate.Month - 1) / 3 + 1}",
            PosDimension.OrderDateMonth => r.OrderDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            PosDimension.OrderDateDay => r.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => "None",
        };

        private static string Clean(string? s) => string.IsNullOrWhiteSpace(s) ? "None" : s.Trim();
    }

    /// <summary>Running totals for one (row-node, column-key) intersection.</summary>
    public class PivotCell
    {
        private readonly HashSet<int> _orderIds = new();

        public int Quantity { get; private set; }
        public decimal LineTotal { get; private set; }
        public int OrderCount => _orderIds.Count;

        public void Add(PosOrderReportRow row)
        {
            _orderIds.Add(row.OrderId);
            Quantity += row.Quantity;
            LineTotal += row.LineTotal;
        }

        public decimal Value(PosMeasure measure) => measure switch
        {
            PosMeasure.OrderCount => OrderCount,
            PosMeasure.ProductQuantity => Quantity,
            PosMeasure.Total => LineTotal,
            _ => 0m,
        };
    }

    public partial class PivotNode : ObservableObject
    {
        [ObservableProperty] private bool _isExpanded;

        public string Label { get; }
        public int Level { get; }
        public List<PivotNode> Children { get; } = new();

        /// <summary>Keyed by column key; always contains <see cref="PosReportPivot.GrandColumnKey"/>.</summary>
        public Dictionary<string, PivotCell> Values { get; } = new();

        public bool HasChildren => Children.Count > 0;

        public PivotNode(string label, int level)
        {
            Label = label;
            Level = level;
        }

        public PivotCell CellFor(string columnKey)
        {
            if (!Values.TryGetValue(columnKey, out var cell))
            {
                cell = new PivotCell();
                Values[columnKey] = cell;
            }
            return cell;
        }
    }

    public class PosPivotResult
    {
        public PivotNode Root { get; init; } = new("Total", 0);

        /// <summary>Ordered column keys ending with the grand-total column.</summary>
        public IReadOnlyList<string> ColumnKeys { get; init; } = new[] { PosReportPivot.GrandColumnKey };
    }

    public static class PosReportPivot
    {
        public const string GrandColumnKey = "Total";

        public static PosPivotResult Build(
            IEnumerable<PosOrderReportRow> rows,
            IReadOnlyList<PosDimension> rowDimensions,
            IReadOnlyList<PosDimension> columnDimensions)
        {
            var rowDims = (rowDimensions ?? Array.Empty<PosDimension>())
                .Where(d => true).ToList();
            var colDim = columnDimensions is { Count: > 0 } ? (PosDimension?)columnDimensions[0] : null;

            var root = new PivotNode("Total", 0) { IsExpanded = true };
            var columnKeys = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                var colKey = colDim.HasValue ? PosDimensions.Key(colDim.Value, row) : null;
                if (colKey != null) columnKeys.Add(colKey);

                Accumulate(root, row, colKey);

                var node = root;
                for (var i = 0; i < rowDims.Count; i++)
                {
                    var label = PosDimensions.Key(rowDims[i], row);
                    var child = node.Children.FirstOrDefault(c => c.Label == label);
                    if (child == null)
                    {
                        child = new PivotNode(label, i + 1) { IsExpanded = i == 0 };
                        node.Children.Add(child);
                    }
                    Accumulate(child, row, colKey);
                    node = child;
                }
            }

            SortRecursive(root);

            var orderedColumns = columnKeys.ToList();
            orderedColumns.Add(GrandColumnKey);

            return new PosPivotResult { Root = root, ColumnKeys = orderedColumns };
        }

        private static void Accumulate(PivotNode node, PosOrderReportRow row, string? colKey)
        {
            node.CellFor(GrandColumnKey).Add(row);
            if (colKey != null) node.CellFor(colKey).Add(row);
        }

        private static void SortRecursive(PivotNode node)
        {
            node.Children.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            foreach (var child in node.Children) SortRecursive(child);
        }
    }
}
