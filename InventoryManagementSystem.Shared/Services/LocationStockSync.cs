using System;
using System.Linq;
using InventoryManagementSystem.Domain;
using SQLite;

namespace InventoryManagementSystem.Services
{
    /// <summary>
    /// Keeps the default warehouse LocationStock row in sync with product-level stock changes.
    /// When at least one active location exists, stock deltas are mirrored there and
    /// Product.StockQuantity is reconciled to the sum of all location rows for that product.
    /// </summary>
    internal static class LocationStockSync
    {
        public static void ApplyDelta(SQLiteConnection conn, int productId, int quantityDelta, int? specificLocationId = null)
        {
            if (quantityDelta == 0) return;

            Location? location = null;
            if (specificLocationId.HasValue && specificLocationId.Value > 0)
            {
                location = conn.Table<Location>()
                    .FirstOrDefault(l => l.Id == specificLocationId.Value && l.IsActive);
            }

            location ??= conn.Table<Location>()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Id)
                .FirstOrDefault();
            if (location == null) return;

            var locStock = conn.Table<LocationStock>()
                .Where(ls => ls.LocationId == location.Id && ls.ProductId == productId)
                .FirstOrDefault();

            if (locStock == null)
            {
                var existingLocCount = conn.Table<LocationStock>().Count(ls => ls.ProductId == productId);
                int initialQty = 0;
                if (existingLocCount == 0)
                {
                    var product = conn.Find<Product>(productId);
                    initialQty = product?.StockQuantity ?? 0;
                }
                else
                {
                    initialQty = Math.Max(0, quantityDelta);
                }

                if (initialQty < 0)
                {
                    throw new InvalidOperationException($"Insufficient stock at location '{location.Name}'.");
                }

                conn.Insert(new LocationStock
                {
                    LocationId = location.Id,
                    ProductId = productId,
                    Quantity = initialQty
                });

                ReconcileProductStockFromLocations(conn, productId);
                return;
            }

            locStock.Quantity += quantityDelta;
            if (locStock.Quantity < 0)
            {
                throw new InvalidOperationException($"Insufficient stock at location '{location.Name}'.");
            }

            conn.Update(locStock);
            ReconcileProductStockFromLocations(conn, productId);
        }

        public static void ReconcileProductStockFromLocations(SQLiteConnection conn, int productId)
        {
            if (!conn.Table<Location>().Any(l => l.IsActive)) return;

            var total = conn.Table<LocationStock>()
                .Where(ls => ls.ProductId == productId)
                .Sum(ls => ls.Quantity);

            var product = conn.Find<Product>(productId);
            if (product == null) return;

            product.StockQuantity = total;
            SyncMetadataHelper.Touch(product);
            conn.Update(product);
        }
    }
}
