using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class DamageWriteOffService
    {
        private readonly DatabaseService _databaseService;
        private readonly InventoryService _inventoryService;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly ManufacturingService _manufacturingService;

        public static readonly string[] ReasonCategories =
        {
            "Damaged", "Expired", "Theft", "Breakage", "Obsolete", "QualityDefect", "Other"
        };

        public DamageWriteOffService(
            DatabaseService databaseService,
            InventoryService inventoryService,
            PurchaseOrderService purchaseOrderService,
            ManufacturingService manufacturingService)
        {
            _databaseService = databaseService;
            _inventoryService = inventoryService;
            _purchaseOrderService = purchaseOrderService;
            _manufacturingService = manufacturingService;
        }

        public async Task<List<DamageWriteOff>> GetAllAsync()
        {
            return await _databaseService.Connection.Table<DamageWriteOff>()
                .Where(w => !w.IsDeleted)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }

        public async Task<DamageWriteOff> RecordWriteOffAsync(
            int productId,
            int quantity,
            string reasonCategory,
            string notes,
            string username)
        {
            if (quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            var product = await _databaseService.Connection.FindAsync<Product>(productId)
                ?? throw new InvalidOperationException("Product not found.");

            if (product.StockQuantity < quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {quantity}");
            }

            var count = await _databaseService.Connection.Table<DamageWriteOff>().CountAsync();
            var writeOff = new DamageWriteOff
            {
                WriteOffNumber = $"DWO-{DateTime.Now:yyyyMMdd}-{(count + 1):D4}",
                ProductId = productId,
                Quantity = quantity,
                ReasonCategory = reasonCategory,
                Notes = notes ?? string.Empty,
                UnitCost = product.Cost,
                TotalCost = product.Cost * quantity,
                Date = DateTime.Now,
                RecordedByUsername = username
            };

            // Reuses the existing, already-audited stock adjustment mechanism instead of duplicating
            // FIFO batch deduction / journal posting: this FIFO-deducts PurchaseBatch rows and posts
            // a Debit "Inventory Adjustment Expense" (520000) / Credit "Inventory Asset" (120000)
            // journal entry (see InventoryService.AddStockMovementAsync, type "ADJUST"). This record
            // only adds a categorized, reportable reason on top of that generic adjustment.
            await _inventoryService.AddStockMovementAsync(
                productId,
                -quantity,
                "ADJUST",
                reason: $"Write-off ({reasonCategory}): {notes}",
                user: username);

            await _databaseService.Connection.InsertAsync(writeOff);

            return writeOff;
        }

        /// <summary>
        /// Creates a draft replenishment document for stock lost to a write-off: a Manufacturing Order
        /// if the product has a Bill of Materials (it's made in-house), otherwise a Purchase Order
        /// against its preferred supplier (from Reorder Rules). Both are left unconfirmed/Draft so a
        /// human reviews and confirms them - this only saves the data entry.
        /// </summary>
        public async Task<(string DocumentType, string DocumentNumber)> ReplenishAsync(int productId, int quantity, string username)
        {
            if (quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            var product = await _databaseService.Connection.FindAsync<Product>(productId)
                ?? throw new InvalidOperationException("Product not found.");

            var bom = await _databaseService.Connection.Table<BillOfMaterial>()
                .Where(b => !b.IsDeleted && b.ProductId == productId)
                .FirstOrDefaultAsync();

            if (bom != null)
            {
                var mo = new ManufacturingOrder
                {
                    MONumber = $"MO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}",
                    BomId = bom.Id,
                    ProductId = productId,
                    TargetQuantity = quantity,
                    OrderDate = DateTime.Now
                };

                var lines = await _manufacturingService.BuildExpectedLinesFromBomAsync(bom.Id, quantity);
                await _manufacturingService.SaveManufacturingOrderAsync(mo, lines);

                return ("Manufacturing Order", mo.MONumber);
            }

            var rule = await _databaseService.Connection.Table<ReorderRule>()
                .Where(r => r.ProductId == productId)
                .FirstOrDefaultAsync();

            if (rule == null || rule.PreferredSupplierId <= 0)
            {
                throw new InvalidOperationException(
                    $"No preferred supplier is set up for {product.Name}. Add a Reorder Rule for it first, or create the Purchase Order manually.");
            }

            var po = new PurchaseOrder
            {
                SupplierId = rule.PreferredSupplierId,
                Status = "Draft",
                Notes = $"Auto-created to replenish stock lost to a write-off ({quantity} {product.Unit}).",
                CreatedByUsername = username,
                ExpectedDeliveryDate = DateTime.Now.AddDays(rule.LeadTimeDays)
            };

            var items = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem
                {
                    ProductId = productId,
                    QuantityOrdered = quantity,
                    QuantityReceived = 0,
                    UnitCost = product.Cost
                }
            };

            await _purchaseOrderService.CreatePurchaseOrderAsync(po, items);

            return ("Purchase Order", po.PONumber);
        }

        public async Task<Dictionary<string, decimal>> GetTotalsByCategoryAsync(DateTime? from = null, DateTime? to = null)
        {
            var all = await GetAllAsync();
            if (from.HasValue) all = all.Where(w => w.Date >= from.Value).ToList();
            if (to.HasValue) all = all.Where(w => w.Date <= to.Value).ToList();

            return all.GroupBy(w => w.ReasonCategory).ToDictionary(g => g.Key, g => g.Sum(w => w.TotalCost));
        }
    }
}
