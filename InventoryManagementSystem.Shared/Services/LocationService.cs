using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class LocationService
    {
        private readonly DatabaseService _databaseService;

        public LocationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Location>> GetAllLocationsAsync()
        {
            return await _databaseService.Connection.Table<Location>().Where(l => l.IsActive).ToListAsync();
        }

        public async Task AddLocationAsync(Location location)
        {
            await _databaseService.Connection.InsertAsync(location);
        }

        public async Task<List<LocationStock>> GetStockByLocationAsync(int locationId)
        {
            return await _databaseService.Connection.Table<LocationStock>()
                .Where(ls => ls.LocationId == locationId)
                .ToListAsync();
        }

        public async Task<List<LocationStock>> GetProductLocationsAsync(int productId)
        {
            return await _databaseService.Connection.Table<LocationStock>()
                .Where(ls => ls.ProductId == productId)
                .ToListAsync();
        }

        public async Task<int> GetProductStockAtLocationAsync(int locationId, int productId)
        {
            var ls = await _databaseService.Connection.Table<LocationStock>()
                .FirstOrDefaultAsync(s => s.LocationId == locationId && s.ProductId == productId);
            return ls?.Quantity ?? 0;
        }

        public async Task TransferStockAsync(StockTransfer transfer)
        {
            await TransferStockBatchAsync(new List<StockTransfer> { transfer });
        }

        public async Task TransferStockBatchAsync(List<StockTransfer> transfers)
        {
            if (transfers == null || transfers.Count == 0) return;

            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                var locations = conn.Table<Location>().ToList();
                var products = conn.Table<Product>().ToList();

                // 1. Pre-validation: check aggregated quantities per product against source location stock
                var groupedBySourceAndProduct = transfers
                    .GroupBy(t => (t.FromLocationId, t.ProductId));

                foreach (var group in groupedBySourceAndProduct)
                {
                    var fromLocId = group.Key.FromLocationId;
                    var prodId = group.Key.ProductId;
                    var totalReq = group.Sum(t => t.Quantity);

                    var sourceStock = conn.Table<LocationStock>()
                        .FirstOrDefault(ls => ls.LocationId == fromLocId && ls.ProductId == prodId);

                    var available = sourceStock?.Quantity ?? 0;
                    if (available < totalReq)
                    {
                        var prod = products.FirstOrDefault(p => p.Id == prodId);
                        var loc = locations.FirstOrDefault(l => l.Id == fromLocId);
                        var prodName = prod?.Name ?? $"Product #{prodId}";
                        var locName = loc?.Name ?? $"Location #{fromLocId}";
                        throw new InvalidOperationException($"Insufficient stock for '{prodName}' at location '{locName}'. Available: {available}, Total Requested: {totalReq}.");
                    }
                }

                // 2. Perform each transfer
                foreach (var transfer in transfers)
                {
                    var sourceLoc = locations.FirstOrDefault(l => l.Id == transfer.FromLocationId);
                    var destLoc = locations.FirstOrDefault(l => l.Id == transfer.ToLocationId);
                    var product = products.FirstOrDefault(p => p.Id == transfer.ProductId);

                    var sourceStock = conn.Table<LocationStock>()
                        .FirstOrDefault(ls => ls.LocationId == transfer.FromLocationId && ls.ProductId == transfer.ProductId);

                    if (sourceStock == null || sourceStock.Quantity < transfer.Quantity)
                    {
                        var prodName = product?.Name ?? $"Product #{transfer.ProductId}";
                        var locName = sourceLoc?.Name ?? $"Location #{transfer.FromLocationId}";
                        throw new InvalidOperationException($"Insufficient stock for '{prodName}' at location '{locName}'. Available: {sourceStock?.Quantity ?? 0}, Requested: {transfer.Quantity}.");
                    }

                    sourceStock.Quantity -= transfer.Quantity;
                    conn.Update(sourceStock);

                    var destStock = conn.Table<LocationStock>()
                        .FirstOrDefault(ls => ls.LocationId == transfer.ToLocationId && ls.ProductId == transfer.ProductId);

                    if (destStock == null)
                    {
                        destStock = new LocationStock
                        {
                            LocationId = transfer.ToLocationId,
                            ProductId = transfer.ProductId,
                            Quantity = transfer.Quantity
                        };
                        conn.Insert(destStock);
                    }
                    else
                    {
                        destStock.Quantity += transfer.Quantity;
                        conn.Update(destStock);
                    }

                    transfer.Status = "Completed";
                    transfer.CompletedDate = DateTime.Now;
                    SyncMetadataHelper.Touch(transfer);
                    conn.Insert(transfer);

                    // Movements with FromLocation & ToLocation
                    var moveOut = new StockMovement
                    {
                        ProductId = transfer.ProductId,
                        QuantityChanged = -transfer.Quantity,
                        MovementType = "ADJUST",
                        Reason = $"Stock Transfer {transfer.TransferNumber} to {destLoc?.Name ?? transfer.ToLocationId.ToString()}",
                        Date = DateTime.Now,
                        Username = transfer.RequestedByUsername,
                        FromLocation = sourceLoc?.Name ?? string.Empty,
                        ToLocation = destLoc?.Name ?? string.Empty
                    };
                    SyncMetadataHelper.Touch(moveOut);
                    conn.Insert(moveOut);

                    var moveIn = new StockMovement
                    {
                        ProductId = transfer.ProductId,
                        QuantityChanged = transfer.Quantity,
                        MovementType = "ADJUST",
                        Reason = $"Stock Transfer {transfer.TransferNumber} from {sourceLoc?.Name ?? transfer.FromLocationId.ToString()}",
                        Date = DateTime.Now,
                        Username = transfer.RequestedByUsername,
                        FromLocation = sourceLoc?.Name ?? string.Empty,
                        ToLocation = destLoc?.Name ?? string.Empty
                    };
                    SyncMetadataHelper.Touch(moveIn);
                    conn.Insert(moveIn);

                    LocationStockSync.ReconcileProductStockFromLocations(conn, transfer.ProductId);
                }
            });
        }

        public async Task<List<StockTransferListItem>> GetAllStockTransfersAsync()
        {
            var transfers = await _databaseService.Connection.Table<StockTransfer>()
                .OrderByDescending(t => t.RequestedDate)
                .ToListAsync();

            var locations = await GetAllLocationsAsync();
            var products = await _databaseService.Connection.Table<Product>().ToListAsync();

            var locDict = locations.ToDictionary(l => l.Id, l => l.Name);
            var prodDict = products.ToDictionary(p => p.Id);

            var result = new List<StockTransferListItem>();
            foreach (var t in transfers)
            {
                prodDict.TryGetValue(t.ProductId, out var prod);
                locDict.TryGetValue(t.FromLocationId, out var fromName);
                locDict.TryGetValue(t.ToLocationId, out var toName);

                result.Add(new StockTransferListItem
                {
                    Transfer = t,
                    FromLocationName = fromName ?? $"Location #{t.FromLocationId}",
                    ToLocationName = toName ?? $"Location #{t.ToLocationId}",
                    ProductName = prod?.Name ?? $"Product #{t.ProductId}",
                    ProductSku = prod?.SKU ?? string.Empty,
                    ProductUnit = prod?.Unit ?? "Pcs"
                });
            }
            return result;
        }

        public async Task<int> GetTotalStockAcrossLocationsAsync(int productId)
        {
            var stocks = await GetProductLocationsAsync(productId);
            return stocks.Sum(s => s.Quantity);
        }

        public async Task<List<LocationStock>> GetLowStockByLocationAsync(int locationId)
        {
            return await _databaseService.Connection.Table<LocationStock>()
                .Where(ls => ls.LocationId == locationId && ls.Quantity <= ls.ReorderPoint)
                .ToListAsync();
        }
    }
}
