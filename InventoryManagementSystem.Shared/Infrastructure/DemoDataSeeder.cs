using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using SQLite;

namespace InventoryManagementSystem.Infrastructure
{
    /// <summary>
    /// Seeds realistic demo inventory data on first run (empty product catalog).
    /// </summary>
    public static class DemoDataSeeder
    {
        private static readonly string[] CategoryNames =
        {
            "Electronics", "Office Supplies", "Food & Beverage", "Clothing",
            "Hardware", "Health & Beauty", "Home & Garden", "Sports"
        };

        private static readonly string[] PieColors =
        {
            "#4F46E5", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4", "#EC4899", "#64748B"
        };

        public static async Task SeedIfEmptyAsync(SQLiteAsyncConnection connection, string currency = "RWF")
        {
            var productCount = await connection.Table<Product>().CountAsync();
            if (productCount > 0)
            {
                return;
            }

            var today = DateTime.Today;
            var rng = new Random(42);

            await connection.RunInTransactionAsync(conn =>
            {
                SeedReferenceData(conn, currency, today, rng);
                var products = SeedProducts(conn, today);
                SeedSalesHistory(conn, products, today, rng);
                SeedPurchaseOrders(conn, products, today, rng);
                SeedReorderRules(conn, products, rng);
            });
        }

        private static void SeedReferenceData(SQLiteConnection conn, string currency, DateTime today, Random rng)
        {
            foreach (var name in CategoryNames)
            {
                var category = new Category { Name = name, Description = $"{name} products" };
                SyncMetadataHelper.Touch(category);
                conn.Insert(category);
            }

            var suppliers = new[]
            {
                new Supplier { Name = "Global Tech Distributors", ContactPerson = "James Okello", Phone = "+250788100001", Email = "sales@globaltech.rw", DefaultLeadTimeDays = 7, Rating = 4.5m, CreatedAt = today.AddDays(-120) },
                new Supplier { Name = "Fresh Foods Wholesale", ContactPerson = "Marie Uwase", Phone = "+250788100002", Email = "orders@freshfoods.rw", DefaultLeadTimeDays = 3, Rating = 4.2m, CreatedAt = today.AddDays(-90) },
                new Supplier { Name = "Office Depot Rwanda", ContactPerson = "Paul Nshimiyimana", Phone = "+250788100003", Email = "b2b@officedepot.rw", DefaultLeadTimeDays = 5, Rating = 4.0m, CreatedAt = today.AddDays(-60) },
                new Supplier { Name = "BuildRight Hardware", ContactPerson = "Grace Mukamana", Phone = "+250788100004", Email = "supply@buildright.rw", DefaultLeadTimeDays = 10, Rating = 3.8m, CreatedAt = today.AddDays(-45) },
                new Supplier { Name = "StyleHub Fashion", ContactPerson = "David Habimana", Phone = "+250788100005", Email = "wholesale@stylehub.rw", DefaultLeadTimeDays = 14, Rating = 4.1m, CreatedAt = today.AddDays(-30) }
            };

            foreach (var supplier in suppliers)
            {
                SyncMetadataHelper.Touch(supplier);
                conn.Insert(supplier);
            }

            var customers = new[]
            {
                new Customer { Name = "Kigali Retail Mart", ContactPerson = "Alice Uwera", Phone = "+250788200001", Email = "buyer@kigalimart.rw", CreatedAt = today.AddDays(-80) },
                new Customer { Name = "Sunrise Cafe", ContactPerson = "Bob Mugisha", Phone = "+250788200002", Email = "procurement@sunrisecafe.rw", CreatedAt = today.AddDays(-70) },
                new Customer { Name = "TechStart Ltd", ContactPerson = "Carol Ingabire", Phone = "+250788200003", Email = "ops@techstart.rw", CreatedAt = today.AddDays(-55) },
                new Customer { Name = "Green Valley Hotel", ContactPerson = "Daniel Niyonsaba", Phone = "+250788200004", Email = "supply@greenvalley.rw", CreatedAt = today.AddDays(-40) },
                new Customer { Name = "City Sports Club", ContactPerson = "Eva Mutoni", Phone = "+250788200005", Email = "inventory@citysports.rw", CreatedAt = today.AddDays(-25) },
                new Customer { Name = "Walk-in Customer", ContactPerson = "", Phone = "", Email = "", CreatedAt = today.AddDays(-10) }
            };

            foreach (var customer in customers)
            {
                SyncMetadataHelper.Touch(customer);
                conn.Insert(customer);
            }

            var locations = new[]
            {
                new Location { Name = "Main Warehouse", Type = "Warehouse", Address = "Kigali Industrial Zone" },
                new Location { Name = "Store Front", Type = "Store", Address = "KN 4 Ave, Kigali" }
            };

            foreach (var location in locations)
            {
                SyncMetadataHelper.Touch(location);
                conn.Insert(location);
            }
        }

        private static List<Product> SeedProducts(SQLiteConnection conn, DateTime today)
        {
            var defs = new (string Name, string Sku, string Category, decimal Price, decimal Cost, int Stock)[]
            {
                ("Wireless Mouse", "ELC-001", "Electronics", 25000, 12000, 42),
                ("USB-C Hub 7-Port", "ELC-002", "Electronics", 45000, 28000, 18),
                ("Bluetooth Headphones", "ELC-003", "Electronics", 65000, 38000, 8),
                ("Laptop Stand", "ELC-004", "Electronics", 35000, 18000, 3),
                ("A4 Copy Paper (500)", "OFF-001", "Office Supplies", 8000, 4500, 120),
                ("Ballpoint Pens (Box 50)", "OFF-002", "Office Supplies", 5000, 2200, 65),
                ("Stapler Heavy Duty", "OFF-003", "Office Supplies", 12000, 6000, 4),
                ("File Folders (Pack 25)", "OFF-004", "Office Supplies", 15000, 8000, 28),
                ("Premium Coffee Beans 1kg", "FOD-001", "Food & Beverage", 18000, 9500, 35),
                ("Green Tea Box", "FOD-002", "Food & Beverage", 6000, 2800, 52),
                ("Mineral Water 24pk", "FOD-003", "Food & Beverage", 12000, 7000, 2),
                ("Cooking Oil 5L", "FOD-004", "Food & Beverage", 22000, 14000, 15),
                ("Cotton T-Shirt", "CLT-001", "Clothing", 15000, 7000, 48),
                ("Denim Jeans", "CLT-002", "Clothing", 45000, 25000, 22),
                ("Running Shoes", "CLT-003", "Clothing", 85000, 48000, 5),
                ("Hammer 500g", "HRD-001", "Hardware", 9000, 4000, 30),
                ("Screwdriver Set", "HRD-002", "Hardware", 14000, 6500, 1),
                ("Hand Sanitizer 500ml", "HLT-001", "Health & Beauty", 4500, 1800, 88),
                ("Shampoo 400ml", "HLT-002", "Health & Beauty", 7500, 3200, 14),
                ("Ceramic Plant Pot", "HOM-001", "Home & Garden", 11000, 5000, 20),
                ("LED Desk Lamp", "HOM-002", "Home & Garden", 28000, 15000, 9),
                ("Yoga Mat", "SPT-001", "Sports", 22000, 11000, 16),
                ("Football Size 5", "SPT-002", "Sports", 18000, 9000, 7),
                ("Resistance Bands Set", "SPT-003", "Sports", 16000, 7500, 0)
            };

            var products = new List<Product>();
            var purchaseDate = today.AddDays(-75);

            foreach (var def in defs)
            {
                var product = new Product
                {
                    Name = def.Name,
                    SKU = def.Sku,
                    Category = def.Category,
                    Price = def.Price,
                    Cost = def.Cost,
                    StockQuantity = def.Stock,
                    AvailableInPOS = true,
                    CanBeSold = true,
                    CanBePurchased = true
                };
                SyncMetadataHelper.Touch(product);
                conn.Insert(product);
                products.Add(product);

                if (def.Stock <= 0)
                {
                    continue;
                }

                var batch = new PurchaseBatch
                {
                    ProductId = product.Id,
                    QuantityPurchased = def.Stock,
                    QuantityRemaining = def.Stock,
                    CostPerUnit = def.Cost,
                    PurchaseDate = purchaseDate,
                    BatchNumber = $"DEMO-{def.Sku}",
                    QualityStatus = "Good"
                };
                conn.Insert(batch);

                var movement = new StockMovement
                {
                    ProductId = product.Id,
                    QuantityChanged = def.Stock,
                    MovementType = "IN",
                    Reason = "Initial demo stock",
                    Date = purchaseDate,
                    Username = "admin"
                };
                SyncMetadataHelper.Touch(movement);
                conn.Insert(movement);
            }

            return products;
        }

        private static void SeedSalesHistory(SQLiteConnection conn, List<Product> products, DateTime today, Random rng)
        {
            var customers = conn.Table<Customer>().ToList();
            var sellable = products.Where(p => p.StockQuantity > 0 && p.Price > 0).ToList();
            if (sellable.Count == 0 || customers.Count == 0)
            {
                return;
            }

            var soCounter = 1;
            for (var dayOffset = 59; dayOffset >= 0; dayOffset--)
            {
                var orderDate = today.AddDays(-dayOffset);
                var ordersToday = dayOffset % 7 == 0 ? rng.Next(2, 4) : (rng.Next(100) < 35 ? 1 : 0);

                for (var o = 0; o < ordersToday; o++)
                {
                    var customer = customers[rng.Next(customers.Count)];
                    var lineCount = rng.Next(1, 4);
                    var orderProducts = sellable.OrderBy(_ => rng.Next()).Take(lineCount).ToList();

                    var so = new SalesOrder
                    {
                        SONumber = $"SO-DEMO-{soCounter:D4}",
                        CustomerId = customer.Id,
                        Status = "Confirmed",
                        OrderDate = orderDate.AddHours(rng.Next(8, 18)),
                        QuotationDate = orderDate,
                        DeliveryDate = orderDate.AddDays(rng.Next(1, 4)),
                        PaymentTerms = "Immediate Payment",
                        CreatedByUsername = "admin",
                        BillingStatus = "Invoiced",
                        DeliveryStatus = "Delivered",
                        Currency = "RWF",
                        Company = "My Company"
                    };
                    SyncMetadataHelper.Touch(so);

                    decimal total = 0;
                    var items = new List<SalesOrderItem>();

                    foreach (var product in orderProducts)
                    {
                        var maxQty = Math.Min(product.StockQuantity, rng.Next(1, 6));
                        if (maxQty <= 0)
                        {
                            continue;
                        }

                        var qty = maxQty;
                        var item = new SalesOrderItem
                        {
                            ProductId = product.Id,
                            QuantityOrdered = qty,
                            QuantityDelivered = qty,
                            QuantityInvoiced = qty,
                            UnitPrice = product.Price
                        };
                        SyncMetadataHelper.Touch(item);
                        items.Add(item);
                        total += qty * product.Price;

                        RecordSale(conn, product, qty, orderDate.AddHours(rng.Next(8, 18)));
                    }

                    if (items.Count == 0)
                    {
                        continue;
                    }

                    so.TotalAmount = total;
                    conn.Insert(so);
                    foreach (var item in items)
                    {
                        item.SalesOrderId = so.Id;
                        conn.Insert(item);
                    }

                    soCounter++;
                }
            }
        }

        private static void RecordSale(SQLiteConnection conn, Product product, int quantity, DateTime saleDate)
        {
            if (product.StockQuantity < quantity)
            {
                return;
            }

            var movement = new StockMovement
            {
                ProductId = product.Id,
                QuantityChanged = quantity,
                MovementType = "OUT",
                Reason = "Demo sale",
                Date = saleDate,
                Username = "admin",
                UnitPrice = product.Price
            };
            SyncMetadataHelper.Touch(movement);
            conn.Insert(movement);

            product.StockQuantity -= quantity;
            SyncMetadataHelper.Touch(product);
            conn.Update(product);

            var remaining = quantity;
            var batches = conn.Table<PurchaseBatch>()
                .Where(b => b.ProductId == product.Id && b.QuantityRemaining > 0)
                .OrderBy(b => b.PurchaseDate)
                .ToList();

            foreach (var batch in batches)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var used = Math.Min(batch.QuantityRemaining, remaining);
                batch.QuantityRemaining -= used;
                conn.Update(batch);

                conn.Insert(new SaleBatchUsage
                {
                    StockMovementId = movement.Id,
                    PurchaseBatchId = batch.Id,
                    QuantityUsed = used,
                    CostPerUnit = batch.CostPerUnit
                });

                remaining -= used;
            }
        }

        private static void SeedPurchaseOrders(SQLiteConnection conn, List<Product> products, DateTime today, Random rng)
        {
            var suppliers = conn.Table<Supplier>().ToList();
            if (suppliers.Count == 0)
            {
                return;
            }

            var statuses = new[] { "Approved", "Approved", "Draft", "Approved" };
            for (var i = 0; i < 8; i++)
            {
                var supplier = suppliers[rng.Next(suppliers.Count)];
                var orderDate = today.AddDays(-rng.Next(5, 55));
                var poProducts = products.OrderBy(_ => rng.Next()).Take(rng.Next(2, 5)).ToList();
                decimal total = 0;

                var po = new PurchaseOrder
                {
                    PONumber = $"PO-DEMO-{i + 1:D3}",
                    SupplierId = supplier.Id,
                    Status = statuses[i % statuses.Length],
                    OrderDate = orderDate,
                    ExpectedDeliveryDate = orderDate.AddDays(supplier.DefaultLeadTimeDays),
                    ActualDeliveryDate = statuses[i % statuses.Length] == "Approved" ? orderDate.AddDays(supplier.DefaultLeadTimeDays) : null,
                    CreatedByUsername = "admin",
                    Currency = "RWF",
                    ReceiptStatus = i % 3 == 0 ? "Pending" : "Received",
                    BillingStatus = i % 2 == 0 ? "Billed" : "Waiting Bill"
                };
                SyncMetadataHelper.Touch(po);
                conn.Insert(po);

                foreach (var product in poProducts)
                {
                    var qty = rng.Next(5, 30);
                    var unitCost = product.Cost;
                    total += qty * unitCost;

                    var item = new PurchaseOrderItem
                    {
                        PurchaseOrderId = po.Id,
                        ProductId = product.Id,
                        QuantityOrdered = qty,
                        QuantityReceived = po.ReceiptStatus == "Received" ? qty : rng.Next(0, qty),
                        QuantityBilled = po.BillingStatus == "Billed" ? qty : 0,
                        UnitCost = unitCost
                    };
                    SyncMetadataHelper.Touch(item);
                    conn.Insert(item);
                }

                po.TotalAmount = total;
                conn.Update(po);
            }
        }

        private static void SeedReorderRules(SQLiteConnection conn, List<Product> products, Random rng)
        {
            var suppliers = conn.Table<Supplier>().ToList();
            var lowStock = products.Where(p => p.StockQuantity <= 5).ToList();

            foreach (var product in lowStock)
            {
                var supplier = suppliers[rng.Next(suppliers.Count)];
                conn.Insert(new ReorderRule
                {
                    ProductId = product.Id,
                    PreferredSupplierId = supplier.Id,
                    ReorderPoint = 10,
                    ReorderQuantity = 25,
                    LeadTimeDays = supplier.DefaultLeadTimeDays,
                    SafetyStockDays = 3
                });
            }
        }

        public static IReadOnlyList<string> GetDefaultPieColors() => PieColors;
    }
}
