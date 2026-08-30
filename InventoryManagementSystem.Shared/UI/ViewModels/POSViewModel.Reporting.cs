using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace InventoryManagementSystem.UI.ViewModels
{
    /// <summary>A pickable row/column grouping in the reporting toolbar (null = "None").</summary>
    public record PosDimensionOption(PosDimension? Value, string Label);

    /// <summary>Column header cell: one per (column key × active measure).</summary>
    public class PosPivotHeader
    {
        public string ColumnKey { get; init; } = "";
        public PosMeasure Measure { get; init; }
        public string GroupTitle { get; init; } = "";
        public string MeasureTitle { get; init; } = "";
    }

    public class PosPivotValue
    {
        public string Text { get; init; } = "";
        public bool IsNumeric { get; init; } = true;
    }

    /// <summary>A single visible line of the pivot, flattened from the tree for column alignment.</summary>
    public partial class PosPivotRowItem : ObservableObject
    {
        public PivotNode Node { get; init; } = new("", 0);
        public string Label { get; init; } = "";
        public int Level { get; init; }
        public bool HasChildren { get; init; }
        public bool IsTotalRow { get; init; }
        public Avalonia.Thickness IndentThickness => new(Level * 18, 0, 0, 0);
        public string Glyph => !HasChildren ? "" : Node.IsExpanded ? "−" : "+";
        public string LabelFontWeight => IsTotalRow || Level <= 1 ? "SemiBold" : "Normal";
        public ObservableCollection<PosPivotValue> Cells { get; } = new();
    }

    public partial class POSViewModel
    {
        // ----- backing data -----
        private List<PosOrderReportRow> _reportRows = new();
        private PosPivotResult _pivot = new();

        public IReadOnlyList<PosDimensionOption> DimensionOptions { get; } = BuildDimensionOptions();
        public IReadOnlyList<PosMeasure> ChartMeasureOptions { get; } =
            new[] { PosMeasure.OrderCount, PosMeasure.ProductQuantity, PosMeasure.Total };

        [ObservableProperty] private string _reportKind = "Orders";
        [ObservableProperty] private bool _isLoadingReport;
        [ObservableProperty] private string _reportViewMode = "Pivot"; // "Pivot" | "Graph"
        [ObservableProperty] private string _chartType = "Bar";        // "Bar" | "Line" | "Pie"
        [ObservableProperty] private PosMeasure _chartMeasure = PosMeasure.Total;

        public bool IsPivotViewMode => ReportViewMode == "Pivot";
        public bool IsGraphViewMode => ReportViewMode == "Graph";
        public bool IsPieChart => ChartType == "Pie";
        public bool IsCartesianChart => ChartType != "Pie";
        public bool IsBarChartSelected => ChartType == "Bar";
        public bool IsLineChartSelected => ChartType == "Line";
        public bool IsPieChartSelected => ChartType == "Pie";
        public bool IsChartMeasureOrderCount => ChartMeasure == PosMeasure.OrderCount;
        public bool IsChartMeasureQuantity => ChartMeasure == PosMeasure.ProductQuantity;
        public bool IsChartMeasureTotal => ChartMeasure == PosMeasure.Total;

        [ObservableProperty] private DateTimeOffset? _reportFromDate = DateTimeOffset.Now.AddDays(-90);
        [ObservableProperty] private DateTimeOffset? _reportToDate = DateTimeOffset.Now;

        [ObservableProperty] private bool _showOrderCount = true;
        [ObservableProperty] private bool _showProductQuantity = true;
        [ObservableProperty] private bool _showTotal = true;

        [ObservableProperty] private PosDimensionOption? _rowDimension1;
        [ObservableProperty] private PosDimensionOption? _rowDimension2;
        [ObservableProperty] private PosDimensionOption? _columnDimension;

        [ObservableProperty] private ObservableCollection<PosPivotHeader> _pivotHeaders = new();
        [ObservableProperty] private ObservableCollection<PosPivotRowItem> _pivotRowsFlat = new();
        [ObservableProperty] private string _reportEmptyMessage = "";

        /// <summary>One-line description of the active filters, shown on the collapsed filter field.</summary>
        [ObservableProperty] private string _reportFilterSummary = "";

        [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _yAxes = Array.Empty<Axis>();

        // Column geometry shared by header + rows.
        public double LabelColumnWidth => 300;
        public double ValueCellWidth => 130;

        private bool _reportInitialised;

        private static IReadOnlyList<PosDimensionOption> BuildDimensionOptions()
        {
            var list = new List<PosDimensionOption> { new(null, "None") };
            foreach (PosDimension d in Enum.GetValues(typeof(PosDimension)))
            {
                list.Add(new PosDimensionOption(d, PosDimensions.Label(d)));
            }
            return list;
        }

        private IEnumerable<PosMeasure> ActiveMeasures()
        {
            if (ShowOrderCount) yield return PosMeasure.OrderCount;
            if (ShowProductQuantity) yield return PosMeasure.ProductQuantity;
            if (ShowTotal) yield return PosMeasure.Total;
        }

        [RelayCommand]
        private async Task LoadOrderReportAsync()
        {
            if (!_reportInitialised)
            {
                _reportInitialised = true;
                RowDimension1 ??= DimensionOptions.First(o => o.Value == PosDimension.ProductCategory);
                RowDimension2 ??= DimensionOptions.First(o => o.Value == null);
                ColumnDimension ??= DimensionOptions.First(o => o.Value == PosDimension.OrderDateMonth);
            }

            try
            {
                IsLoadingReport = true;
                _reportRows = await _salesOrderService.GetPosOrderReportRowsAsync(
                    ReportFromDate?.DateTime, ReportToDate?.DateTime);
                Recompute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load POS order report: {ex.Message}");
                ReportEmptyMessage = "Could not load report data.";
            }
            finally
            {
                IsLoadingReport = false;
            }
        }

        private void Recompute()
        {
            var rowDims = new List<PosDimension>();
            if (RowDimension1?.Value is PosDimension r1) rowDims.Add(r1);
            if (RowDimension2?.Value is PosDimension r2) rowDims.Add(r2);

            var colDims = new List<PosDimension>();
            if (ColumnDimension?.Value is PosDimension c1) colDims.Add(c1);

            _pivot = PosReportPivot.Build(_reportRows, rowDims, colDims);

            BuildHeaders();
            RebuildFlatRows();
            RebuildChart();
            UpdateReportFilterSummary();

            ReportEmptyMessage = _reportRows.Count == 0
                ? "No POS orders in the selected period."
                : "";
        }

        private void UpdateReportFilterSummary()
        {
            var parts = new List<string>();

            var r1 = RowDimension1?.Value is PosDimension a ? PosDimensions.Label(a) : null;
            var r2 = RowDimension2?.Value is PosDimension b ? PosDimensions.Label(b) : null;
            parts.Add(r1 == null ? "No row grouping"
                    : r2 == null ? r1
                    : $"{r1} → {r2}");

            if (ColumnDimension?.Value is PosDimension c)
            {
                parts.Add($"by {PosDimensions.Label(c)}");
            }

            if (ReportFromDate is { } f && ReportToDate is { } t)
            {
                parts.Add($"{f:MMM d, yyyy} – {t:MMM d, yyyy}");
            }
            else if (ReportFromDate is { } fromOnly)
            {
                parts.Add($"from {fromOnly:MMM d, yyyy}");
            }
            else if (ReportToDate is { } toOnly)
            {
                parts.Add($"until {toOnly:MMM d, yyyy}");
            }
            else
            {
                parts.Add("all dates");
            }

            ReportFilterSummary = string.Join("  ·  ", parts);
        }

        private void BuildHeaders()
        {
            var headers = new ObservableCollection<PosPivotHeader>();
            var measures = ActiveMeasures().ToList();
            if (measures.Count == 0) measures.Add(PosMeasure.Total);

            foreach (var key in _pivot.ColumnKeys)
            {
                foreach (var m in measures)
                {
                    headers.Add(new PosPivotHeader
                    {
                        ColumnKey = key,
                        Measure = m,
                        GroupTitle = key == PosReportPivot.GrandColumnKey ? "Total" : FormatColumnKey(key),
                        MeasureTitle = MeasureTitle(m),
                    });
                }
            }
            PivotHeaders = headers;
        }

        private void RebuildFlatRows()
        {
            var flat = new ObservableCollection<PosPivotRowItem>();
            if (_pivot.Root != null)
            {
                AddRow(flat, _pivot.Root, isTotal: true);
                if (_pivot.Root.IsExpanded)
                {
                    foreach (var child in _pivot.Root.Children)
                    {
                        AddNodeRecursive(flat, child);
                    }
                }
            }
            PivotRowsFlat = flat;
        }

        private void AddNodeRecursive(ObservableCollection<PosPivotRowItem> flat, PivotNode node)
        {
            AddRow(flat, node, isTotal: false);
            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    AddNodeRecursive(flat, child);
                }
            }
        }

        private void AddRow(ObservableCollection<PosPivotRowItem> flat, PivotNode node, bool isTotal)
        {
            var item = new PosPivotRowItem
            {
                Node = node,
                Label = isTotal ? "Total" : FormatRowLabel(node),
                Level = node.Level,
                HasChildren = node.HasChildren,
                IsTotalRow = isTotal,
            };

            var measures = ActiveMeasures().ToList();
            if (measures.Count == 0) measures.Add(PosMeasure.Total);

            foreach (var key in _pivot.ColumnKeys)
            {
                node.Values.TryGetValue(key, out var cell);
                foreach (var m in measures)
                {
                    item.Cells.Add(new PosPivotValue
                    {
                        Text = cell == null ? "" : FormatMeasure(m, cell.Value(m)),
                    });
                }
            }
            flat.Add(item);
        }

        [RelayCommand]
        private void TogglePivotNode(PosPivotRowItem? row)
        {
            if (row?.Node == null || !row.Node.HasChildren) return;
            row.Node.IsExpanded = !row.Node.IsExpanded;
            RebuildFlatRows();
        }

        [RelayCommand]
        private void SetReportViewMode(string mode) => ReportViewMode = mode;

        partial void OnReportViewModeChanged(string value)
        {
            OnPropertyChanged(nameof(IsPivotViewMode));
            OnPropertyChanged(nameof(IsGraphViewMode));
        }

        partial void OnChartTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsPieChart));
            OnPropertyChanged(nameof(IsCartesianChart));
            OnPropertyChanged(nameof(IsBarChartSelected));
            OnPropertyChanged(nameof(IsLineChartSelected));
            OnPropertyChanged(nameof(IsPieChartSelected));
        }

        [RelayCommand]
        private void SetChartType(string type)
        {
            ChartType = type;
            RebuildChart();
        }

        [RelayCommand]
        private void SetChartMeasure(string measure)
        {
            ChartMeasure = measure switch
            {
                "OrderCount" => PosMeasure.OrderCount,
                "ProductQuantity" => PosMeasure.ProductQuantity,
                _ => PosMeasure.Total,
            };
        }

        [RelayCommand]
        private void ToggleMeasure(string measure)
        {
            switch (measure)
            {
                case "OrderCount": ShowOrderCount = !ShowOrderCount; break;
                case "ProductQuantity": ShowProductQuantity = !ShowProductQuantity; break;
                case "Total": ShowTotal = !ShowTotal; break;
            }
            if (!ShowOrderCount && !ShowProductQuantity && !ShowTotal) ShowTotal = true;
            BuildHeaders();
            RebuildFlatRows();
        }

        partial void OnRowDimension1Changed(PosDimensionOption? value) => RecomputeIfReady();
        partial void OnRowDimension2Changed(PosDimensionOption? value) => RecomputeIfReady();
        partial void OnColumnDimensionChanged(PosDimensionOption? value) => RecomputeIfReady();
        partial void OnChartMeasureChanged(PosMeasure value)
        {
            OnPropertyChanged(nameof(IsChartMeasureOrderCount));
            OnPropertyChanged(nameof(IsChartMeasureQuantity));
            OnPropertyChanged(nameof(IsChartMeasureTotal));
            RebuildChart();
        }

        [RelayCommand]
        private void SetReportDatePreset(string preset)
        {
            var today = DateTimeOffset.Now;
            switch (preset)
            {
                case "all":
                    ReportFromDate = null;
                    ReportToDate = null;
                    break;
                case "7d":
                    ReportFromDate = today.AddDays(-7);
                    ReportToDate = today;
                    break;
                case "30d":
                    ReportFromDate = today.AddDays(-30);
                    ReportToDate = today;
                    break;
                case "90d":
                    ReportFromDate = today.AddDays(-90);
                    ReportToDate = today;
                    break;
                case "month":
                    ReportFromDate = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
                    ReportToDate = today;
                    break;
                case "year":
                    ReportFromDate = new DateTimeOffset(new DateTime(today.Year, 1, 1));
                    ReportToDate = today;
                    break;
            }
        }

        partial void OnReportFromDateChanged(DateTimeOffset? value)
        {
            if (_reportInitialised) _ = LoadOrderReportAsync();
        }

        partial void OnReportToDateChanged(DateTimeOffset? value)
        {
            if (_reportInitialised) _ = LoadOrderReportAsync();
        }

        private void RecomputeIfReady()
        {
            if (_reportInitialised) Recompute();
        }

        private void RebuildChart()
        {
            var root = _pivot.Root;
            if (root == null || root.Children.Count == 0)
            {
                Series = Array.Empty<ISeries>();
                XAxes = Array.Empty<Axis>();
                YAxes = Array.Empty<Axis>();
                return;
            }

            var categories = root.Children.Select(FormatRowLabel).ToArray();
            var valueColumns = _pivot.ColumnKeys
                .Where(k => k != PosReportPivot.GrandColumnKey)
                .ToList();

            if (ChartType == "Pie")
            {
                Series = root.Children.Select(child =>
                {
                    child.Values.TryGetValue(PosReportPivot.GrandColumnKey, out var cell);
                    return (ISeries)new PieSeries<double>
                    {
                        Values = new[] { (double)(cell?.Value(ChartMeasure) ?? 0m) },
                        Name = FormatRowLabel(child),
                    };
                }).ToArray();
                XAxes = Array.Empty<Axis>();
                YAxes = Array.Empty<Axis>();
                return;
            }

            var seriesList = new List<ISeries>();
            if (valueColumns.Count > 0)
            {
                foreach (var col in valueColumns)
                {
                    var values = root.Children.Select(child =>
                    {
                        child.Values.TryGetValue(col, out var cell);
                        return (double)(cell?.Value(ChartMeasure) ?? 0m);
                    }).ToArray();
                    seriesList.Add(MakeCartesian(FormatColumnKey(col), values));
                }
            }
            else
            {
                var values = root.Children.Select(child =>
                {
                    child.Values.TryGetValue(PosReportPivot.GrandColumnKey, out var cell);
                    return (double)(cell?.Value(ChartMeasure) ?? 0m);
                }).ToArray();
                seriesList.Add(MakeCartesian(MeasureTitle(ChartMeasure), values));
            }

            Series = seriesList.ToArray();
            XAxes = new[] { new Axis { Labels = categories, LabelsRotation = 15 } };
            YAxes = new[] { new Axis { Name = MeasureTitle(ChartMeasure) } };
        }

        private ISeries MakeCartesian(string name, double[] values) => ChartType == "Line"
            ? new LineSeries<double> { Name = name, Values = values }
            : new ColumnSeries<double> { Name = name, Values = values };

        // ----- export -----
        [RelayCommand]
        private async Task ExportOrderReportAsync()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime
                is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow == null)
            {
                return;
            }

            var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export POS Order Report",
                DefaultExtension = ".xlsx",
                SuggestedFileName = $"pos-orders-{DateTime.Now:yyyyMMdd}",
                FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } } }
            });
            if (file == null) return;

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("POS Orders");

            sheet.Cell(1, 1).Value = "Group";
            for (var i = 0; i < PivotHeaders.Count; i++)
            {
                sheet.Cell(1, i + 2).Value = $"{PivotHeaders[i].GroupTitle} · {PivotHeaders[i].MeasureTitle}";
                sheet.Cell(1, i + 2).Style.Font.Bold = true;
            }
            sheet.Cell(1, 1).Style.Font.Bold = true;

            var r = 2;
            foreach (var row in PivotRowsFlat)
            {
                sheet.Cell(r, 1).Value = new string(' ', row.Level * 2) + row.Label;
                for (var i = 0; i < row.Cells.Count; i++)
                {
                    if (decimal.TryParse(row.Cells[i].Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var num))
                        sheet.Cell(r, i + 2).Value = num;
                    else
                        sheet.Cell(r, i + 2).Value = row.Cells[i].Text;
                }
                r++;
            }
            sheet.Columns().AdjustToContents();

            await using var stream = await file.OpenWriteAsync();
            workbook.SaveAs(stream);
        }

        // ----- formatting helpers -----
        private static string MeasureTitle(PosMeasure m) => m switch
        {
            PosMeasure.OrderCount => "Order",
            PosMeasure.ProductQuantity => "Product Quantity",
            PosMeasure.Total => "Total Price",
            _ => m.ToString(),
        };

        private static string FormatMeasure(PosMeasure m, decimal value) => m switch
        {
            PosMeasure.Total => value.ToString("N2", CultureInfo.CurrentCulture),
            _ => value.ToString("N0", CultureInfo.CurrentCulture),
        };

        private string FormatRowLabel(PivotNode node) => FormatDimensionKey(RowDimensionFor(node.Level), node.Label);

        private PosDimension? RowDimensionFor(int level) => level switch
        {
            1 => RowDimension1?.Value,
            2 => RowDimension2?.Value,
            _ => null,
        };

        private string FormatColumnKey(string key) => FormatDimensionKey(ColumnDimension?.Value, key);

        private static string FormatDimensionKey(PosDimension? dim, string key)
        {
            if (string.IsNullOrEmpty(key)) return "None";
            switch (dim)
            {
                case PosDimension.OrderDateMonth when DateTime.TryParseExact(key, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mo):
                    return mo.ToString("MMM yyyy", CultureInfo.CurrentCulture);
                case PosDimension.OrderDateDay when DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var da):
                    return da.ToString("dd MMM yyyy", CultureInfo.CurrentCulture);
                default:
                    return key;
            }
        }
    }
}
