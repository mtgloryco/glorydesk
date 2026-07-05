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

        public async Task ProcessSalesOrderReturnAsync(int salesOrderId, List<(int itemId, int quantityToReturn, string condition, string reason, decimal refundAmount)> returns, string processedByUsername)
        {
            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                var so = conn.Find<SalesOrder>(salesOrderId);
                if (so == null) throw new ArgumentException("Sales Order not found.");

                foreach (var ret in returns)
                {
                    if (ret.quantityToReturn <= 0) continue;

                    var item = conn.Find<SalesOrderItem>(ret.itemId);
                    if (item == null || item.SalesOrderId != salesOrderId) continue;

                    if (ret.quantityToReturn > item.QuantityDelivered)
                    {
                        throw new InvalidOperationException($"Cannot return more than delivered. Delivered: {item.QuantityDelivered}, Returning: {ret.quantityToReturn}");
                    }

                    // Decrement quantity delivered
                    item.QuantityDelivered -= ret.quantityToReturn;
                    conn.Update(item);

                    // Insert CustomerReturn record
                    var customerReturn = new CustomerReturn
                    {
                        ReturnNumber = $"RET-SO-{so.SONumber}-{DateTime.Now:yyyyMMddHHmmss}-{item.ProductId}",
                        ProductId = item.ProductId,
                        Quantity = ret.quantityToReturn,
                        Reason = ret.reason,
                        Condition = ret.condition,
                        RefundAmount = ret.refundAmount,
                        ProcessedByUsername = processedByUsername,
                        ReturnDate = DateTime.Now,
                        OriginalReceiptId = salesOrderId.ToString(),
                        Resolution = "Restocked"
                    };
                    conn.Insert(customerReturn);

                    if (ret.condition == "Resaleable")
                    {
                        // Add stock back via StockMovement IN
                        var movement = new StockMovement
                        {
                            ProductId = item.ProductId,
                            QuantityChanged = ret.quantityToReturn,
                            MovementType = "IN",
                            Reason = $"Customer Return (SO: {so.SONumber})",
                            Date = DateTime.Now,
                            Username = processedByUsername,
                            UnitPrice = item.UnitPrice
                        };
                        conn.Insert(movement);

                        // Update product total stock
                        var product = conn.Find<Product>(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += ret.quantityToReturn;
                            conn.Update(product);
                        }
                    }
                }

                // Re-evaluate SalesOrder Status
                var allItems = conn.Table<SalesOrderItem>().Where(i => i.SalesOrderId == salesOrderId).ToList();
                bool allDelivered = allItems.All(i => i.QuantityDelivered >= i.QuantityOrdered);

                so.Status = allDelivered ? "Delivered" : "Confirmed";
                so.DeliveryStatus = allItems.Any(i => i.QuantityDelivered > 0)
                    ? (allDelivered ? "Delivered" : "Partially Delivered")
                    : "Pending";

                if (so.Status == "Delivered")
                {
                    so.DeliveryDate = DateTime.Now;
                }
                else
                {
                    so.DeliveryDate = null;
                }
                conn.Update(so);
            });
        }

        public async Task ProcessPurchaseOrderReturnAsync(int purchaseOrderId, List<(int itemId, int quantityToReturn, string reason, decimal creditAmount)> returns, string processedByUsername)
        {
            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                var po = conn.Find<PurchaseOrder>(purchaseOrderId);
                if (po == null) throw new ArgumentException("Purchase Order not found.");

                foreach (var ret in returns)
                {
                    if (ret.quantityToReturn <= 0) continue;

                    var item = conn.Find<PurchaseOrderItem>(ret.itemId);
                    if (item == null || item.PurchaseOrderId != purchaseOrderId) continue;

                    if (ret.quantityToReturn > item.QuantityReceived)
                    {
                        throw new InvalidOperationException($"Cannot return more than received. Received: {item.QuantityReceived}, Returning: {ret.quantityToReturn}");
                    }

                    var product = conn.Find<Product>(item.ProductId);
                    if (product == null) continue;

                    if (ret.quantityToReturn > product.StockQuantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for return. Available: {product.StockQuantity}, Returning: {ret.quantityToReturn}");
                    }

                    // Decrement quantity received
                    item.QuantityReceived -= ret.quantityToReturn;
                    conn.Update(item);

                    // Insert SupplierReturn record
                    var supplierReturn = new SupplierReturn
                    {
                        ReturnNumber = $"RET-PO-{po.PONumber}-{DateTime.Now:yyyyMMddHHmmss}-{item.ProductId}",
                        SupplierId = po.SupplierId,
                        ProductId = item.ProductId,
                        Quantity = ret.quantityToReturn,
                        Reason = ret.reason,
                        Status = "Credited",
                        CreditAmount = ret.creditAmount,
                        ProcessedByUsername = processedByUsername,
                        ReturnDate = DateTime.Now,
                        OriginalReceiptId = purchaseOrderId.ToString()
                    };
                    conn.Insert(supplierReturn);

                    // Update product total stock
                    product.StockQuantity -= ret.quantityToReturn;
                    conn.Update(product);

                    // Log Stock Movement OUT
                    var movement = new StockMovement
                    {
                        ProductId = item.ProductId,
                        QuantityChanged = ret.quantityToReturn,
                        MovementType = "OUT",
                        Reason = $"Supplier Return (PO: {po.PONumber})",
                        Date = DateTime.Now,
                        Username = processedByUsername,
                        UnitPrice = product.Price
                    };
                    conn.Insert(movement);

                    // Batch Consumption (FIFO)
                    int remainingToDeduct = ret.quantityToReturn;
                    var batches = conn.Table<PurchaseBatch>()
                                      .Where(b => b.ProductId == item.ProductId && b.QuantityRemaining > 0)
                                      .OrderBy(b => b.PurchaseDate)
                                      .ThenBy(b => b.Id)
                                      .ToList();

                    foreach (var batch in batches)
                    {
                        if (remainingToDeduct <= 0) break;

                        int deductFromThisBatch = Math.Min(batch.QuantityRemaining, remainingToDeduct);

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
                }

                // Re-evaluate PurchaseOrder Status
                var allItems = conn.Table<PurchaseOrderItem>().Where(i => i.PurchaseOrderId == purchaseOrderId).ToList();
                bool allReceived = allItems.All(i => i.QuantityReceived >= i.QuantityOrdered);

                po.Status = allReceived ? "Received" : "Shipped";
                po.ReceiptStatus = allItems.Any(i => i.QuantityReceived > 0)
                    ? (allReceived ? "Received" : "Partially Received")
                    : "Pending";

                if (po.Status == "Received")
                {
                    po.ActualDeliveryDate = DateTime.Now;
                }
                else
                {
                    po.ActualDeliveryDate = null;
                }
                conn.Update(po);
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
