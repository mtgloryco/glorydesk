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
        public static void ApplyDelta(SQLiteConnection conn, int productId, int quantityDelta)
        {
            if (quantityDelta == 0) return;

            var location = conn.Table<Location>()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Id)
                .FirstOrDefault();
            if (location == null) return;

            var locStock = conn.Table<LocationStock>()
                .Where(ls => ls.LocationId == location.Id && ls.ProductId == productId)
                .FirstOrDefault();

            if (locStock == null)
            {
                // Bootstrap the default location row from the already-updated product stock.
                var product = conn.Find<Product>(productId);
                var currentQty = product?.StockQuantity ?? 0;
                if (currentQty < 0)
                {
                    throw new InvalidOperationException("Insufficient stock at default location.");
                }

                conn.Insert(new LocationStock
                {
                    LocationId = location.Id,
                    ProductId = productId,
                    Quantity = currentQty
                });
                return;
            }

            locStock.Quantity += quantityDelta;
            if (locStock.Quantity < 0)
            {
                throw new InvalidOperationException("Insufficient stock at default location.");
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
