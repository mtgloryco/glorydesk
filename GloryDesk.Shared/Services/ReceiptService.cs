using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class ReceiptService
    {
        private readonly SettingsService _settingsService;

        public ReceiptService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            // QuestPDF Community License (Free for individuals and small businesses < $1M revenue)
            if (!OperatingSystem.IsBrowser())
            {
                try
                {
                    QuestPDF.Settings.License = LicenseType.Community;
                }
                catch { }
            }
        }

        public string FormatReceiptText(
            string cashierName,
            IEnumerable<UI.ViewModels.CartItem> cartItems,
            decimal totalAmount,
            decimal amountPaid,
            decimal changeDue,
            string? receiptNumber = null,
            DateTime? transactionDate = null)
        {
            var date = transactionDate ?? DateTime.Now;
            var recId = receiptNumber ?? Guid.NewGuid().ToString("N")[..6].ToUpper();
            var storeName = !string.IsNullOrWhiteSpace(_settingsService?.CurrentSettings?.StoreName)
                ? _settingsService.CurrentSettings.StoreName
                : "MT GLORY CO";
            var storeAddress = _settingsService?.CurrentSettings?.StoreAddress ?? "Kigali, Rwanda";
            var currency = _settingsService?.CurrentSettings?.CurrencySymbol ?? "RWF";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine(CenterText(storeName, 40));
            if (!string.IsNullOrWhiteSpace(storeAddress))
            {
                sb.AppendLine(CenterText(storeAddress, 40));
            }
            sb.AppendLine("========================================");
            sb.AppendLine($"Date:    {date:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cashier: {cashierName}");
            sb.AppendLine($"Receipt: #{recId}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,7} {3,8}", "ITEM", "QTY", "PRICE", "TOTAL"));
            sb.AppendLine("----------------------------------------");

            if (cartItems != null)
            {
                foreach (var item in cartItems)
                {
                    var name = item.Product?.Name ?? "Item";
                    if (name.Length > 18) name = name[..15] + "...";
                    sb.AppendLine(string.Format("{0,-18} {1,4} {2,7:N0} {3,8:N0}", name, item.Quantity, item.UnitPrice, item.Subtotal));
                }
            }

            sb.AppendLine("----------------------------------------");
            sb.AppendLine(string.Format("{0,-20} {1,19}", "TOTAL:", $"{totalAmount:N0} {currency}"));
            sb.AppendLine(string.Format("{0,-20} {1,19}", "Amount Paid:", $"{amountPaid:N0} {currency}"));
            sb.AppendLine(string.Format("{0,-20} {1,19}", "Change Due:", $"{changeDue:N0} {currency}"));
            sb.AppendLine("========================================");
            sb.AppendLine(CenterText("Thank you for your business!", 40));
            sb.AppendLine(CenterText("Murakoze cyane!", 40));
            sb.AppendLine("========================================");

            return sb.ToString();
        }

        private static string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length >= width) return text;
            int padLeft = (width - text.Length) / 2;
            return text.PadLeft(padLeft + text.Length);
        }

        public string GenerateReceiptPdf(string cashierName, IEnumerable<Domain.StockMovement> items, decimal totalAmount)
        {
            var date = DateTime.Now;
            var filename = $"Receipt_{date:yyyyMMdd_HHmmss}.pdf";
            var path = Path.Combine(AppPaths.EnsureDocumentsSubfolder("Receipts"), filename);

            if (!OperatingSystem.IsBrowser())
            {
                try
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A5); // A5 is standard for receipts
                            page.Margin(1, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(10));

                            page.Header()
                                .Column(col =>
                                {
                                    col.Item().Text(_settingsService.CurrentSettings.StoreName).SemiBold().FontSize(16).AlignCenter();
                                    if (!string.IsNullOrEmpty(_settingsService.CurrentSettings.StoreAddress))
                                    {
                                        col.Item().Text(_settingsService.CurrentSettings.StoreAddress).FontSize(10).AlignCenter().FontColor(Colors.Grey.Medium);
                                    }
                                });

                            page.Content()
                                .PaddingVertical(1, Unit.Centimetre)
                                .Column(x =>
                                {
                                    x.Spacing(5);

                                    x.Item().Text($"Date: {date:g}");
                                    x.Item().Text($"Cashier: {cashierName}");
                                    x.Item().Text($"Receipt #: {Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}");
                                    
                                    x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                    x.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3); // Item
                                            columns.RelativeColumn(1); // Qty
                                            columns.RelativeColumn(1); // Price
                                            columns.RelativeColumn(1); // Total
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Text("Item").Bold();
                                            header.Cell().Text("Qty").Bold().AlignRight();
                                            header.Cell().Text("Price").Bold().AlignRight();
                                            header.Cell().Text("Total").Bold().AlignRight();
                                            
                                            header.Cell().ColumnSpan(4)
                                                .PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                                        });
                                    });
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(x =>
                                {
                                    x.Span("Page ");
                                    x.CurrentPageNumber();
                                });
                        });
                    })
                    .GeneratePdf(path);

                    return path;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReceiptService] QuestPDF unavailable: {ex.Message}");
                }
            }

            var txtFilename = $"Receipt_{date:yyyyMMdd_HHmmss}.txt";
            var textFallback = Path.Combine(AppPaths.EnsureDocumentsSubfolder("Receipts"), txtFilename);
            try
            {
                File.WriteAllText(textFallback, $"Receipt Date: {date:g}\nCashier: {cashierName}\nTotal: {totalAmount:N0}");
            }
            catch { }
            return textFallback;
        }

        // Overload to accept Cart Items directly from POS
        public string GenerateReceiptFromCart(string cashierName, IEnumerable<UI.ViewModels.CartItem> cartItems, decimal totalAmount, decimal amountPaid, decimal changeDue)
        {
            var date = DateTime.Now;
            var outputFolder = AppPaths.EnsureDocumentsSubfolder("Receipts");
            var filename = $"Receipt_{date:yyyyMMdd_HHmmss}.txt";
            var pdfFilename = $"Receipt_{date:yyyyMMdd_HHmmss}.pdf";
            var textPath = Path.Combine(outputFolder, filename);
            var pdfPath = Path.Combine(outputFolder, pdfFilename);

            var itemsList = cartItems?.ToList() ?? new List<UI.ViewModels.CartItem>();
            var receiptText = FormatReceiptText(cashierName, itemsList, totalAmount, amountPaid, changeDue, null, date);

            try
            {
                File.WriteAllText(textPath, receiptText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReceiptService] Failed to write receipt text file: {ex.Message}");
            }

            if (!OperatingSystem.IsBrowser())
            {
                try
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A5);
                            page.Margin(1, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                            page.Header()
                                .Column(col =>
                                {
                                    col.Item().Text(_settingsService.CurrentSettings.StoreName).FontSize(18).SemiBold().AlignCenter();
                                    col.Item().Text(_settingsService.CurrentSettings.StoreAddress).FontSize(10).AlignCenter().FontColor(Colors.Grey.Medium);
                                });

                            page.Content()
                                .PaddingVertical(1, Unit.Centimetre)
                                .Column(column =>
                                {
                                    column.Spacing(5);

                                    column.Item().Row(row =>
                                    {
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text($"Date: {date:yyyy-MM-dd HH:mm}");
                                            c.Item().Text($"Cashier: {cashierName}");
                                            c.Item().Text($"Rec ID: {Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}");
                                        });
                                    });

                                    column.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                                    column.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3); // Name
                                            columns.RelativeColumn(1); // Qty
                                            columns.RelativeColumn(1.5f); // Price
                                            columns.RelativeColumn(1.5f); // Total
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(CellStyle).Text("Item").Bold();
                                            header.Cell().Element(CellStyle).AlignRight().Text("Qty").Bold();
                                            header.Cell().Element(CellStyle).AlignRight().Text("Price").Bold();
                                            header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();

                                            static IContainer CellStyle(IContainer container)
                                            {
                                                return container.PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Black);
                                            }
                                        });

                                        foreach (var item in itemsList)
                                        {
                                            table.Cell().Element(CellStyle).Text(item.Product.Name);
                                            table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                                            table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N0}"); // N0 for simpler currency in receipts
                                            table.Cell().Element(CellStyle).AlignRight().Text($"{item.Subtotal:N0}");

                                            static IContainer CellStyle(IContainer container)
                                            {
                                                return container.PaddingVertical(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                                            }
                                        }
                                    });

                                    column.Item().PaddingTop(10).Column(c =>
                                    {
                                        c.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text("TOTAL").FontSize(14).Bold();
                                            r.RelativeItem().AlignRight().Text($"{totalAmount:N0} RWF").FontSize(14).Bold();
                                        });
                                        
                                        c.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text("Cash").FontSize(10);
                                            r.RelativeItem().AlignRight().Text($"{amountPaid:N0}");
                                        });

                                        c.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text("Change").FontSize(10);
                                            r.RelativeItem().AlignRight().Text($"{changeDue:N0}");
                                        });
                                    });
                                });

                            page.Footer()
                                .Column(c =>
                                {
                                    c.Item().AlignCenter().Text("Thank you for your business!").FontSize(12).Italic();
                                    c.Item().AlignCenter().Text("Murakoze cyane!").FontSize(10).FontColor(Colors.Grey.Darken1);
                                });
                        });
                    })
                    .GeneratePdf(pdfPath);

                    return pdfPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReceiptService] QuestPDF unavailable ({ex.Message}). Using text receipt.");
                }
            }

            return textPath;
        }
    }
}
