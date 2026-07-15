using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InventoryManagementSystem.Domain;

namespace InventoryManagementSystem.Services
{
    public class SalesOrderPdfService
    {
        private readonly SettingsService _settingsService;

        public SalesOrderPdfService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string GenerateSalesOrderPdf(
            SalesOrder so, 
            List<SalesOrderItem> items, 
            List<Product> allProducts, 
            List<Tax> allTaxes, 
            Customer? customer,
            bool asInvoice = false)
        {
            var dateStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var cleanRef = so.SONumber.Replace("-", "_").Replace(" ", "_");
            var docTypePrefix = asInvoice ? "INV" : (so.Status == "Draft" ? "QUOT" : "SO");
            var filename = $"{docTypePrefix}_{cleanRef}_{dateStr}.pdf";
            
            var outputFolder = AppPaths.EnsureDocumentsSubfolder("Sales");
            var path = Path.Combine(outputFolder, filename);

            Directory.CreateDirectory(outputFolder);

            var companyName = !string.IsNullOrWhiteSpace(so.Company) ? so.Company : _settingsService.CurrentSettings.StoreName;
            var companyAddress = _settingsService.CurrentSettings.StoreAddress;
            var currency = so.Currency ?? _settingsService.CurrentSettings.CurrencySymbol;

            var taxTotals = new Dictionary<int, (Tax Tax, decimal Amount)>();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(companyName).FontSize(18).Bold().FontColor(Colors.Green.Darken4);
                                if (!string.IsNullOrEmpty(companyAddress))
                                {
                                    c.Item().Text(companyAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                                }
                                if (!string.IsNullOrEmpty(so.CreatedByUsername))
                                {
                                    c.Item().Text($"Salesperson: {so.CreatedByUsername}").FontSize(9).FontColor(Colors.Grey.Darken2);
                                }
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                var docTitle = so.Status == "Draft" ? "Sales Quotation" : (asInvoice ? "Tax Invoice" : "Sales Order");
                                c.Item().Text(docTitle).FontSize(24).Bold().FontColor(Colors.Grey.Darken4);
                                c.Item().Text($"# {so.SONumber}").FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                                
                                c.Item().PaddingTop(10).Column(details =>
                                {
                                    details.Spacing(2);
                                    details.Item().Text($"Date: {so.OrderDate:yyyy-MM-dd}").FontSize(9);
                                    details.Item().Text($"Terms: {so.PaymentTerms}").FontSize(9);
                                    if (so.ExpirationDate.HasValue)
                                    {
                                        details.Item().Text($"Expiration: {so.ExpirationDate.Value:yyyy-MM-dd}").FontSize(9);
                                    }
                                    if (so.DeliveryDate.HasValue)
                                    {
                                        details.Item().Text($"Delivery Date: {so.DeliveryDate.Value:yyyy-MM-dd}").FontSize(9);
                                    }
                                });
                            });
                        });

                        col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    // Content
                    page.Content().Column(col =>
                    {
                        col.Spacing(15);

                        // Customer Info
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Customer / Bill To:").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                                if (customer != null)
                                {
                                    c.Item().Text(customer.Name).FontSize(12).Bold();
                                    if (!string.IsNullOrEmpty(customer.ContactPerson))
                                    {
                                        c.Item().Text($"Contact: {customer.ContactPerson}").FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(customer.Phone))
                                    {
                                        c.Item().Text($"Phone: {customer.Phone}").FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(customer.Email))
                                    {
                                        c.Item().Text($"Email: {customer.Email}").FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(customer.Address))
                                    {
                                        c.Item().Text($"Address: {customer.Address}").FontSize(9);
                                    }
                                }
                                else
                                {
                                    c.Item().Text("General Customer").FontSize(12);
                                }
                            });
                        });

                        // Items Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);  // #
                                columns.RelativeColumn(4);   // Product Description
                                columns.RelativeColumn(1.2f); // Qty
                                columns.RelativeColumn(1.8f); // Unit Price
                                columns.RelativeColumn(1.8f); // Taxes
                                columns.RelativeColumn(2);   // Total
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product / SKU").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Unit Price").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tax").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Subtotal").Bold();
                            });

                            int index = 1;
                            foreach (var item in items)
                            {
                                var product = allProducts.FirstOrDefault(p => p.Id == item.ProductId);
                                var tax = item.TaxId.HasValue ? allTaxes.FirstOrDefault(t => t.Id == item.TaxId.Value) : null;
                                
                                var productName = product?.Name ?? "Unknown Product";
                                var productSku = product?.SKU ?? "N/A";
                                
                                var qty = asInvoice ? (item.QuantityInvoiced > 0 ? item.QuantityInvoiced : item.QuantityOrdered) : item.QuantityOrdered;
                                var price = item.UnitPrice;
                                var rawSubtotal = qty * price;
                                var lineTaxAmount = 0m;
                                var lineTotal = rawSubtotal;

                                if (tax != null)
                                {
                                    if (so.IsTaxInclusive)
                                    {
                                        decimal basePrice;
                                        if (tax.Computation == "Percentage")
                                        {
                                            basePrice = rawSubtotal / (1 + (tax.Amount / 100));
                                        }
                                        else
                                        {
                                            basePrice = Math.Max(0, rawSubtotal - (qty * tax.Amount));
                                        }
                                        lineTaxAmount = rawSubtotal - basePrice;
                                    }
                                    else
                                    {
                                        if (tax.Computation == "Percentage")
                                        {
                                            lineTaxAmount = rawSubtotal * (tax.Amount / 100);
                                        }
                                        else
                                        {
                                            lineTaxAmount = qty * tax.Amount;
                                        }
                                        lineTotal = rawSubtotal + lineTaxAmount;
                                    }

                                    if (lineTaxAmount > 0)
                                    {
                                        if (taxTotals.ContainsKey(tax.Id))
                                        {
                                            taxTotals[tax.Id] = (tax, taxTotals[tax.Id].Amount + lineTaxAmount);
                                        }
                                        else
                                        {
                                            taxTotals[tax.Id] = (tax, lineTaxAmount);
                                        }
                                    }
                                }

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(index.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{productName} (SKU: {productSku})");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(qty.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{price:N2} {currency}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(tax?.Name ?? "0%");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{lineTotal:N2} {currency}");

                                index++;
                            }
                        });

                        // Totals Summary
                        col.Item().AlignRight().Width(250).Column(totalsCol =>
                        {
                            totalsCol.Spacing(5);

                            decimal subtotal = items.Sum(it => 
                            {
                                var qty = asInvoice ? (it.QuantityInvoiced > 0 ? it.QuantityInvoiced : it.QuantityOrdered) : it.QuantityOrdered;
                                var rawTotal = qty * it.UnitPrice;
                                if (it.TaxId.HasValue && so.IsTaxInclusive)
                                {
                                    var tax = allTaxes.FirstOrDefault(t => t.Id == it.TaxId.Value);
                                    if (tax != null)
                                    {
                                        if (tax.Computation == "Percentage")
                                        {
                                            return rawTotal / (1 + (tax.Amount / 100));
                                        }
                                        else
                                        {
                                            return Math.Max(0, rawTotal - (qty * tax.Amount));
                                        }
                                    }
                                }
                                return rawTotal;
                            });

                            totalsCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").Bold();
                                r.RelativeItem().AlignRight().Text($"{subtotal:N2} {currency}");
                            });

                            foreach (var keyVal in taxTotals.Values)
                            {
                                totalsCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text($"{keyVal.Tax.Name} ({keyVal.Tax.Amount}%):");
                                    r.RelativeItem().AlignRight().Text($"{keyVal.Amount:N2} {currency}");
                                });
                            }

                            var total = subtotal + taxTotals.Values.Sum(v => v.Amount);
                            totalsCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                            totalsCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total Amount:").FontSize(12).Bold().FontColor(Colors.Green.Darken4);
                                r.RelativeItem().AlignRight().Text($"{total:N2} {currency}").FontSize(12).Bold().FontColor(Colors.Green.Darken4);
                            });
                        });

                        if (!string.IsNullOrEmpty(so.Notes))
                        {
                            col.Item().PaddingTop(15).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(notesCol =>
                            {
                                notesCol.Item().Text("Terms / Notes:").Bold().FontSize(9);
                                notesCol.Item().Text(so.Notes).FontSize(9);
                            });
                        }
                    });

                    // Footer
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text(AppBranding.GeneratedByFooter).FontSize(8).FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                                x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });
                });
            }).GeneratePdf(path);

            return path;
        }
    }
}
