using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    /// <summary>
    /// Looks up products by SKU/barcode. Works with USB barcode scanners (keyboard wedge + Enter).
    /// </summary>
    public class BarcodeService
    {
        private readonly DatabaseService _databaseService;

        public BarcodeService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<Product?> FindProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            var normalized = barcode.Trim();
            var products = await _databaseService.Connection.Table<Product>()
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Barcode (the printed UPC/EAN) takes priority over SKU (the business's own internal
            // code) - they're often different values. Falls back to an EXACT SKU match for products
            // that only ever had a SKU set, before the dedicated Barcode field existed.
            //
            // Deliberately no partial / "contains" match: a scan must identify exactly one product,
            // and a loose match would silently ring up the wrong item. Partial text search is still
            // available through the product grid filter (POSViewModel.LoadProducts).
            return products.FirstOrDefault(p =>
                       p.Barcode != null && p.Barcode.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                   ?? products.FirstOrDefault(p =>
                       p.SKU != null && p.SKU.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }
    }
}
