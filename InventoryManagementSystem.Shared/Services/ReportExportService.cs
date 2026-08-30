using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryManagementSystem.Services
{
    /// <summary>One column of an exported report.</summary>
    public class ReportExportColumn
    {
        public string Header { get; init; } = string.Empty;
        public bool RightAlign { get; init; }
        /// <summary>Relative width used by the PDF table layout.</summary>
        public double Width { get; init; } = 1;

        public ReportExportColumn() { }

        public ReportExportColumn(string header, bool rightAlign = false, double width = 1)
        {
            Header = header;
            RightAlign = rightAlign;
            Width = width;
        }
    }

    /// <summary>One data row of an exported report. Cells are already-formatted strings.</summary>
    public class ReportExportRow
    {
        public IReadOnlyList<string> Cells { get; init; } = Array.Empty<string>();
        /// <summary>Indent steps for the first cell (hierarchical statements).</summary>
        public int Indent { get; init; }
        public bool Bold { get; init; }
    }

    /// <summary>A whole report ready to be written to a file.</summary>
    public class ReportExportModel
    {
        public string Title { get; init; } = "Report";
        public string Subtitle { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public IReadOnlyList<ReportExportColumn> Columns { get; init; } = Array.Empty<ReportExportColumn>();
        public IReadOnlyList<ReportExportRow> Rows { get; init; } = Array.Empty<ReportExportRow>();
    }

    /// <summary>
    /// Turns an in-memory <see cref="ReportExportModel"/> into a shareable XLSX or PDF document.
    /// Used by the Reports screen so Balance Sheet, P&amp;L, aging, ledger, VAT, etc. can be
    /// exported as files people can send on.
    /// </summary>
    public class ReportExportService
    {
        public ReportExportService()
        {
            if (!OperatingSystem.IsBrowser())
            {
                QuestPDF.Settings.License = LicenseType.Community;
            }
        }

        public byte[] BuildXlsx(ReportExportModel model)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SanitizeSheetName(model.Title));

            var colCount = Math.Max(1, model.Columns.Count);
            var row = 1;

            void MergedLine(string text, Action<IXLCell>? style = null)
            {
                var cell = sheet.Cell(row, 1);
                cell.Value = text;
                style?.Invoke(cell);
                sheet.Range(row, 1, row, colCount).Merge();
                row++;
            }

            MergedLine(model.Title, c => { c.Style.Font.Bold = true; c.Style.Font.FontSize = 14; });
            if (!string.IsNullOrWhiteSpace(model.CompanyName)) MergedLine(model.CompanyName);
            if (!string.IsNullOrWhiteSpace(model.Subtitle)) MergedLine(model.Subtitle, c => c.Style.Font.Italic = true);
            MergedLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}", c => c.Style.Font.FontColor = XLColor.Gray);
            row++;

            var headerRow = row;
            for (var c = 0; c < model.Columns.Count; c++)
            {
                var cell = sheet.Cell(row, c + 1);
                cell.Value = model.Columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            row++;

            foreach (var r in model.Rows)
            {
                for (var c = 0; c < model.Columns.Count; c++)
                {
                    var text = c < r.Cells.Count ? r.Cells[c] : string.Empty;
                    var cell = sheet.Cell(row, c + 1);

                    if (c > 0 && decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var num))
                    {
                        cell.Value = num;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        cell.Value = text;
                    }

                    if (r.Bold) cell.Style.Font.Bold = true;
                    if (model.Columns[c].RightAlign) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    if (c == 0 && r.Indent > 0) cell.Style.Alignment.Indent = r.Indent;
                }
                row++;
            }

            sheet.Columns().AdjustToContents();
            sheet.SheetView.FreezeRows(headerRow);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] BuildPdf(ReportExportModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    page.Header().Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(model.CompanyName))
                            col.Item().Text(model.CompanyName).FontSize(11).SemiBold();
                        col.Item().Text(model.Title).FontSize(16).Bold();
                        if (!string.IsNullOrWhiteSpace(model.Subtitle))
                            col.Item().Text(model.Subtitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var c in model.Columns)
                                cols.RelativeColumn((float)Math.Max(0.3, c.Width));
                        });

                        table.Header(header =>
                        {
                            foreach (var c in model.Columns)
                            {
                                IContainer cell = header.Cell().Background(Colors.Grey.Lighten3).Padding(4);
                                if (c.RightAlign) cell = cell.AlignRight();
                                cell.Text(c.Header).SemiBold();
                            }
                        });

                        foreach (var r in model.Rows)
                        {
                            for (var i = 0; i < model.Columns.Count; i++)
                            {
                                var text = i < r.Cells.Count ? r.Cells[i] : string.Empty;
                                IContainer cell = table.Cell()
                                    .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Background(r.Bold ? Colors.Grey.Lighten4 : Colors.White)
                                    .PaddingVertical(3).PaddingHorizontal(4);

                                if (i == 0 && r.Indent > 0) cell = cell.PaddingLeft(4 + r.Indent * 12f);
                                if (model.Columns[i].RightAlign) cell = cell.AlignRight();

                                var span = cell.Text(text);
                                if (r.Bold) span.SemiBold();
                            }
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string SanitizeSheetName(string name)
        {
            var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var clean = new string(name.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
            if (clean.Length > 31) clean = clean[..31];
            return string.IsNullOrWhiteSpace(clean) ? "Report" : clean;
        }
    }
}
