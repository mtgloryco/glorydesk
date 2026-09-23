using System.IO;
using System.Linq;
using ClosedXML.Excel;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class ReportExportServiceTests
{
    private static ReportExportModel SampleStatement() => new()
    {
        Title = "Balance Sheet",
        CompanyName = "Acme Ltd",
        Subtitle = "As at 2026-08-30",
        Columns = new[]
        {
            new ReportExportColumn("Line", width: 4),
            new ReportExportColumn("Balance", rightAlign: true, width: 1.4),
        },
        Rows = new[]
        {
            new ReportExportRow { Cells = new[] { "ASSETS", "" }, Bold = true },
            new ReportExportRow { Cells = new[] { "Cash", "1,250.00" }, Indent = 1 },
            new ReportExportRow { Cells = new[] { "Total Assets", "1,250.00" }, Bold = true },
        },
    };

    [Fact]
    public void BuildXlsx_WritesTitleHeaderAndNumericCells()
    {
        var bytes = new ReportExportService().BuildXlsx(SampleStatement());

        Assert.NotEmpty(bytes);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var sheet = wb.Worksheet(1);

        Assert.Equal("Balance Sheet", sheet.Cell(1, 1).GetString());

        // Locate the header row, then the "Cash" data row beneath it.
        var headerRow = sheet.RowsUsed().First(r => r.Cell(1).GetString() == "Line").RowNumber();
        var cashRow = sheet.Row(headerRow + 2);
        Assert.Equal("Cash", cashRow.Cell(1).GetString());
        Assert.Equal(1250.00, cashRow.Cell(2).GetDouble(), 2); // parsed as a number, not text
    }

    [Fact]
    public void BuildPdf_ProducesAPdfDocument()
    {
        var bytes = new ReportExportService().BuildPdf(SampleStatement());

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
