using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class ReturnsService
    {
        private readonly DatabaseService _databaseService;

        public ReturnsService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task ProcessCustomerReturnAsync(CustomerReturn ret)
        {
            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                // Log return
                conn.Insert(ret);

                if (ret.Condition == "Resaleable")
                {
                    // Add stock back via StockMovement IN
                    var movement = new StockMovement
                    {
                        ProductId = ret.ProductId,
                        QuantityChanged = ret.Quantity,
                        MovementType = "IN",
                        Reason = "Customer Return - Resaleable",
                        Date = DateTime.Now,
                        Username = ret.ProcessedByUsername
                    };
                    conn.Insert(movement);

                    // Update product total stock
                    var product = conn.Find<Product>(ret.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += ret.Quantity;
                        conn.Update(product);
                    }
                }
                else
                {
                    // Log as waste (no stock added back)
                    // Optionally log separate waste record if there's a waste table
                }
            });
        }

        public async Task ProcessSupplierReturnAsync(SupplierReturn ret)
        {
            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                var product = conn.Find<Product>(ret.ProductId);
                if (product == null) return;

                // 1. Availability validation
                if (ret.Quantity > product.StockQuantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for return. Available: {product.StockQuantity}, Returning: {ret.Quantity}");
                }

                // 2. Log return
                conn.Insert(ret);

                // 3. Log Stock Movement OUT
                var movement = new StockMovement
                {
                    ProductId = ret.ProductId,
                    QuantityChanged = ret.Quantity,
                    MovementType = "OUT",
                    Reason = "Supplier Return - Overstock/Defective",
                    Date = DateTime.Now,
                    Username = ret.ProcessedByUsername ?? "System",
                    UnitPrice = product.Price
                };
                conn.Insert(movement);

                // 4. Batch Consumption (FIFO)
                int remainingToDeduct = ret.Quantity;
                var batches = conn.Table<PurchaseBatch>()
                                  .Where(b => b.ProductId == ret.ProductId && b.QuantityRemaining > 0)
                                  .OrderBy(b => b.PurchaseDate)
                                  .ThenBy(b => b.Id)
                                  .ToList();

                foreach (var batch in batches)
                {
                    if (remainingToDeduct <= 0) break;

                    int deductFromThisBatch = Math.Min(batch.QuantityRemaining, remainingToDeduct);

                    // Link this OUT movement to the purchase batch for proper COGS/Value tracking
                    conn.Insert(new SaleBatchUsage
                    {
                        StockMovementId = movement.Id,
                        PurchaseBatchId = batch.Id,
                        QuantityUsed = deductFromThisBatch,
                        CostPerUnit = batch.CostPerUnit
                    });

                    batch.QuantityRemaining -= deductFromThisBatch;
                    conn.Update(batch);

                    remainingToDeduct -= deductFromThisBatch;
                }

                if (remainingToDeduct > 0)
                {
                    throw new InvalidOperationException("Batches were out of sync with product stock. Return failed.");
                }

                // 5. Update master stock
                product.StockQuantity -= ret.Quantity;
                conn.Update(product);
            });
        }

        public async Task<List<CustomerReturn>> GetCustomerReturnsAsync(DateTime from, DateTime to)
        {
            return await _databaseService.Connection.Table<CustomerReturn>()
                .Where(r => r.ReturnDate >= from && r.ReturnDate <= to)
                .ToListAsync();
        }

        public async Task<List<SupplierReturn>> GetSupplierReturnsAsync(DateTime from, DateTime to)
        {
            return await _databaseService.Connection.Table<SupplierReturn>()
                .Where(r => r.ReturnDate >= from && r.ReturnDate <= to)
                .ToListAsync();
        }
    }
}
