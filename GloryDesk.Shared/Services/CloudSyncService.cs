using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using SQLite;

namespace InventoryManagementSystem.Services
{
    public class CloudSyncService
    {
        private readonly DatabaseService _databaseService;
        private readonly CloudSyncApiClient _apiClient;
        private readonly AuditService? _auditService;
        private readonly LicenseService? _licenseService;
        private readonly SettingsService? _settingsService;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public CloudSyncService(
            DatabaseService databaseService,
            CloudSyncApiClient? apiClient = null,
            AuditService? auditService = null,
            LicenseService? licenseService = null,
            SettingsService? settingsService = null)
        {
            _databaseService = databaseService;
            _apiClient = apiClient ?? new CloudSyncApiClient();
            _auditService = auditService;
            _licenseService = licenseService;
            _settingsService = settingsService;
        }

        private string? _cachedDeviceId;
        public string DeviceId => _cachedDeviceId ?? Environment.MachineName;
        public string? AuthToken => _apiClient.AuthToken;

        public async Task RestoreCloudSessionAsync(string email, string authToken, string? organizationId = null, string? organizationName = null, string? licenseKey = null)
        {
            try
            {
                _apiClient.AuthToken = authToken;
                if (!string.IsNullOrWhiteSpace(organizationId)) _apiClient.OrganizationId = organizationId;
                var state = await GetOrCreateSyncStateAsync();
                state.AuthToken = authToken;
                state.CloudUserEmail = email;
                if (!string.IsNullOrWhiteSpace(organizationId)) state.OrganizationId = organizationId;
                if (!string.IsNullOrWhiteSpace(organizationName)) state.OrganizationName = organizationName;
                state.LastSyncStatus = "Session restored";
                await _databaseService.Connection.InsertOrReplaceAsync(state);

                if (_settingsService != null)
                {
                    if (!string.IsNullOrWhiteSpace(organizationName))
                    {
                        _settingsService.CurrentSettings.StoreName = organizationName;
                        _settingsService.CurrentSettings.SetupCompleted = true;
                    }
                    _ = SyncOrganizationSettingsAsync();
                }

                if (_licenseService != null && !string.IsNullOrWhiteSpace(licenseKey))
                {
                    try
                    {
                        await _licenseService.ActivateLicenseAsync(licenseKey);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CloudSyncService] Error restoring session: {ex.Message}");
            }
        }

        public async Task<CloudSyncStatus> GetStatusAsync()
        {
            var state = await GetOrCreateSyncStateAsync();
            return new CloudSyncStatus
            {
                IsConfigured = !string.IsNullOrWhiteSpace(state.ApiBaseUrl),
                IsAuthenticated = !string.IsNullOrWhiteSpace(state.AuthToken),
                OrganizationName = state.OrganizationName,
                LastSyncDate = state.LastPullAt ?? state.LastPushAt,
                LastBackupDate = state.LastBackupAt,
                StatusText = state.LastSyncStatus,
                PendingOutboundCount = state.PendingOutboundCount
            };
        }

        public async Task<DateTime?> GetLastSyncDateAsync()
        {
            var state = await _databaseService.Connection.Table<SyncState>().FirstOrDefaultAsync();
            return state?.LastPullAt ?? state?.LastPushAt;
        }

        public DateTime? GetLastSyncDate()
        {
            try
            {
                var task = Task.Run(async () => await _databaseService.Connection.Table<SyncState>().FirstOrDefaultAsync());
                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    return task.Result?.LastPullAt ?? task.Result?.LastPushAt;
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> SyncOrganizationSettingsAsync()
        {
            if (_settingsService == null || string.IsNullOrWhiteSpace(_apiClient.AuthToken))
            {
                return false;
            }

            try
            {
                var cloudSettings = await _apiClient.GetOrganizationSettingsAsync();
                if (cloudSettings != null)
                {
                    // If local already had custom business info and cloud was untouched default, push local to cloud
                    if (_settingsService.CurrentSettings.SetupCompleted &&
                        !string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StoreName) &&
                        _settingsService.CurrentSettings.StoreName != "My Store" &&
                        (string.IsNullOrWhiteSpace(cloudSettings.StoreName) || cloudSettings.StoreName == "My Store"))
                    {
                        var toPush = _settingsService.ExportToCloudSettings();
                        await _apiClient.UpdateOrganizationSettingsAsync(toPush);
                    }
                    else
                    {
                        // Apply cloud settings to local
                        _settingsService.ApplyCloudOrganizationSettings(cloudSettings);
                    }
                    return true;
                }
                else if (_settingsService.CurrentSettings.SetupCompleted)
                {
                    // No cloud settings yet, push local settings to initialize cloud
                    var toPush = _settingsService.ExportToCloudSettings();
                    return await _apiClient.UpdateOrganizationSettingsAsync(toPush);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CloudSyncService] Error syncing organization settings: {ex.Message}");
            }

            return false;
        }

        public async Task<bool> PushOrganizationSettingsAsync()
        {
            if (_settingsService == null || string.IsNullOrWhiteSpace(_apiClient.AuthToken)) return false;
            try
            {
                var toPush = _settingsService.ExportToCloudSettings();
                return await _apiClient.UpdateOrganizationSettingsAsync(toPush);
            }
            catch
            {
                return false;
            }
        }

        public async Task<CloudSyncResult> ConfigureCloudLoginAsync(string email, string password, string? organizationName = null, bool register = false)
        {
            try
            {
                var state = await GetOrCreateSyncStateAsync();
                _apiClient.BaseUrl = state.ApiBaseUrl;

                string? hardwareId = null;
                if (!OperatingSystem.IsBrowser() && _licenseService != null)
                {
                    try
                    {
                        hardwareId = _licenseService.GetHardwareId();
                    }
                    catch { }
                }

                CloudAuthResponse auth;
                if (register)
                {
                    auth = await _apiClient.RegisterAsync(email, password, organizationName ?? $"{email.Split('@')[0]} Workspace");
                }
                else
                {
                    auth = await _apiClient.LoginAsync(email, password, hardwareId);
                }

                // Guard: this local database file already belongs to a different organization.
                // Signing into a second org here would silently mix two companies' records in
                // one SQLite file. Refuse instead — a different org needs its own local profile/file.
                if (!string.IsNullOrWhiteSpace(state.OrganizationId) &&
                    !string.Equals(state.OrganizationId, auth.OrganizationId, StringComparison.Ordinal))
                {
                    return CloudSyncResult.Fail(
                        $"This device is already linked to \"{state.OrganizationName}\". Signing in to " +
                        $"\"{auth.OrganizationName}\" here would mix two companies' data in the same local file. " +
                        "Set up a separate company profile for the new organization instead.");
                }

                state.AuthToken = auth.Token;
                state.OrganizationId = auth.OrganizationId;
                state.OrganizationName = auth.OrganizationName;
                state.CloudUserEmail = auth.Email;
                state.LastSyncStatus = register ? "Registered with cloud" : "Connected to cloud";
                await _databaseService.Connection.InsertOrReplaceAsync(state);

                if (_settingsService != null)
                {
                    if (!string.IsNullOrWhiteSpace(auth.OrganizationName))
                    {
                        _settingsService.CurrentSettings.StoreName = auth.OrganizationName;
                        _settingsService.CurrentSettings.SetupCompleted = true;
                    }
                    await SyncOrganizationSettingsAsync();
                }

                if (_licenseService != null && !string.IsNullOrWhiteSpace(auth.LicenseKey))
                {
                    try
                    {
                        await _licenseService.ActivateLicenseAsync(auth.LicenseKey);
                    }
                    catch { }
                }

                return CloudSyncResult.Ok(state.LastSyncStatus, 0, auth.Role);
            }
            catch (Exception ex)
            {
                return CloudSyncResult.Fail(ex.Message);
            }
        }

        public async Task<bool> BackupToCloudAsync(string userId, string authToken)
        {
            try
            {
                var state = await PrepareApiClientAsync(authToken);
                if (state == null) return false;

                await _databaseService.CheckpointWalAsync();

                var dbPath = _databaseService.DatabasePath;
                if (!File.Exists(dbPath)) return false;

                await using var compressed = await CompressFileAsync(dbPath);
                await _apiClient.UploadBackupAsync(compressed);

                state.LastBackupAt = DateTime.UtcNow;
                state.LastSyncStatus = $"Backup completed by {userId}";
                await _databaseService.Connection.UpdateAsync(state);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cloud backup error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreFromCloudAsync(string userId, string authToken)
        {
            try
            {
                var state = await PrepareApiClientAsync(authToken);
                if (state == null) return false;

                await using var remoteStream = await _apiClient.DownloadBackupAsync();
                var tempGz = Path.Combine(Path.GetTempPath(), $"ims-restore-{Guid.NewGuid():N}.db.gz");
                var tempDb = Path.Combine(Path.GetTempPath(), $"ims-restore-{Guid.NewGuid():N}.db");

                await using (var file = File.Create(tempGz))
                {
                    await remoteStream.CopyToAsync(file);
                }

                await DecompressToFileAsync(tempGz, tempDb);

                await _databaseService.CloseConnectionAsync();
                File.Copy(tempDb, _databaseService.DatabasePath, overwrite: true);
                await _databaseService.ReopenConnectionAsync();
                await _databaseService.InitializeAsync();

                try { File.Delete(tempGz); File.Delete(tempDb); } catch { /* ignore cleanup errors */ }

                state.LastSyncStatus = $"Restored from cloud by {userId}";
                state.LastBackupAt = DateTime.UtcNow;
                await _databaseService.Connection.UpdateAsync(state);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cloud restore error: {ex.Message}");
                await _databaseService.ReopenConnectionAsync();
                return false;
            }
        }

        public async Task<int> SyncDeltaAsync()
        {
            var state = await GetOrCreateSyncStateAsync();
            if (string.IsNullOrWhiteSpace(state.AuthToken))
            {
                state.LastSyncStatus = "Not authenticated";
                await _databaseService.Connection.UpdateAsync(state);
                return 0;
            }

            _apiClient.BaseUrl = state.ApiBaseUrl;
            _apiClient.AuthToken = state.AuthToken;
            _apiClient.OrganizationId = state.OrganizationId;

            _ = SyncOrganizationSettingsAsync();

            var since = state.LastPullAt ?? DateTime.MinValue;
            var pushed = await PushLocalChangesAsync(state);
            var pulled = await PullServerChangesAsync(since);

            var total = pushed + pulled;
            var now = DateTime.UtcNow;
            state.LastPushAt = now;
            state.LastPullAt = now;
            state.PendingOutboundCount = 0;
            state.LastSyncStatus = total > 0 ? $"Synced {total} records" : "Already up to date";
            await _databaseService.Connection.UpdateAsync(state);

            return total;
        }

        public async Task<CloudSyncResult> SyncNowAsync(string userId)
        {
            var state = await GetOrCreateSyncStateAsync();
            if (string.IsNullOrWhiteSpace(state.AuthToken))
            {
                return CloudSyncResult.Fail("Connect to cloud first (email/password in Settings or sync panel).");
            }

            try
            {
                var count = await SyncDeltaAsync();
                return CloudSyncResult.Ok(state.LastSyncStatus, count);
            }
            catch (Exception ex)
            {
                // Full SQLite messages (e.g. UNIQUE constraint failed: Account.Code) live here —
                // the sidebar truncates visually; hover the status text for the complete string.
                System.Diagnostics.Debug.WriteLine($"[CloudSyncService] Sync failed: {ex}");
                state.LastSyncStatus = $"Sync failed: {ex.Message}";
                await _databaseService.Connection.UpdateAsync(state);
                return CloudSyncResult.Fail(state.LastSyncStatus);
            }
        }

        private async Task<SyncState?> PrepareApiClientAsync(string authToken)
        {
            var state = await GetOrCreateSyncStateAsync();
            if (string.IsNullOrWhiteSpace(authToken) && string.IsNullOrWhiteSpace(state.AuthToken))
            {
                return null;
            }

            _apiClient.BaseUrl = state.ApiBaseUrl;
            _apiClient.AuthToken = string.IsNullOrWhiteSpace(authToken) ? state.AuthToken : authToken;
            _apiClient.OrganizationId = state.OrganizationId;
            return state;
        }

        private async Task<int> PushLocalChangesAsync(SyncState state)
        {
            var since = state.LastPushAt ?? DateTime.MinValue;
            var changes = new List<SyncChangeDto>();
            var deviceId = state.DeviceId;

            foreach (var descriptor in SyncEntityRegistry.All)
            {
                var localChanges = await GetLocalChangesAsync(descriptor, since);
                changes.AddRange(localChanges);
            }

            if (changes.Count == 0) return 0;

            var response = await _apiClient.PushChangesAsync(new SyncPushRequest
            {
                DeviceId = deviceId,
                Changes = changes
            });

            return response.Accepted;
        }

        private async Task<int> PullServerChangesAsync(DateTime sinceUtc)
        {
            var pull = await _apiClient.PullChangesAsync(sinceUtc);
            var applied = 0;

            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                foreach (var change in pull.Changes.OrderBy(c => c.UpdatedAt))
                {
                    var descriptor = SyncEntityRegistry.All.FirstOrDefault(d => d.EntityType == change.EntityType);
                    if (descriptor == null) continue;

                    if (ApplyServerChange(conn, descriptor, change))
                    {
                        applied++;
                    }
                }
            });

            return applied;
        }

        private async Task<List<SyncChangeDto>> GetLocalChangesAsync(SyncEntityDescriptor descriptor, DateTime sinceUtc)
        {
            var results = new List<SyncChangeDto>();
            var items = await QuerySyncableEntitiesAsync(descriptor, sinceUtc);
            foreach (var item in items)
            {
                results.Add(new SyncChangeDto
                {
                    EntityType = descriptor.EntityType,
                    SyncId = item.SyncId,
                    PayloadJson = JsonSerializer.Serialize(item, descriptor.ClrType, _jsonOptions),
                    UpdatedAt = item.UpdatedAt.ToUniversalTime(),
                    IsDeleted = item.IsDeleted
                });
            }

            return results;
        }

        private async Task<List<ISyncableEntity>> QuerySyncableEntitiesAsync(SyncEntityDescriptor descriptor, DateTime sinceUtc)
        {
            return descriptor.ClrType.Name switch
            {
                nameof(Product) => (await _databaseService.Connection.Table<Product>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(StockMovement) => (await _databaseService.Connection.Table<StockMovement>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Supplier) => (await _databaseService.Connection.Table<Supplier>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(PurchaseOrder) => (await _databaseService.Connection.Table<PurchaseOrder>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(PurchaseOrderItem) => (await _databaseService.Connection.Table<PurchaseOrderItem>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(SalesOrder) => (await _databaseService.Connection.Table<SalesOrder>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(SalesOrderItem) => (await _databaseService.Connection.Table<SalesOrderItem>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Category) => (await _databaseService.Connection.Table<Category>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Tax) => (await _databaseService.Connection.Table<Tax>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(SupplierProduct) => (await _databaseService.Connection.Table<SupplierProduct>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Account) => (await _databaseService.Connection.Table<Account>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Journal) => (await _databaseService.Connection.Table<Journal>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(JournalEntry) => (await _databaseService.Connection.Table<JournalEntry>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(JournalLine) => (await _databaseService.Connection.Table<JournalLine>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(ProductBundle) => (await _databaseService.Connection.Table<ProductBundle>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(BillOfMaterial) => (await _databaseService.Connection.Table<BillOfMaterial>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(BillOfMaterialLine) => (await _databaseService.Connection.Table<BillOfMaterialLine>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(ManufacturingOrder) => (await _databaseService.Connection.Table<ManufacturingOrder>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(ManufacturingOrderLine) => (await _databaseService.Connection.Table<ManufacturingOrderLine>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(CustomerReturn) => (await _databaseService.Connection.Table<CustomerReturn>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(SupplierReturn) => (await _databaseService.Connection.Table<SupplierReturn>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Location) => (await _databaseService.Connection.Table<Location>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(LocationStock) => (await _databaseService.Connection.Table<LocationStock>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(StockTransfer) => (await _databaseService.Connection.Table<StockTransfer>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(Expense) => (await _databaseService.Connection.Table<Expense>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                nameof(DamageWriteOff) => (await _databaseService.Connection.Table<DamageWriteOff>().Where(p => p.UpdatedAt > sinceUtc).ToListAsync()).Cast<ISyncableEntity>().ToList(),
                _ => new List<ISyncableEntity>()
            };
        }

        private bool ApplyServerChange(SQLiteConnection conn, SyncEntityDescriptor descriptor, SyncChangeDto change)
        {
            if (change.IsDeleted)
            {
                return SoftDeleteLocalBySyncId(conn, descriptor, change.SyncId, change.UpdatedAt);
            }

            var entity = JsonSerializer.Deserialize(change.PayloadJson, descriptor.ClrType, _jsonOptions);
            if (entity is not ISyncableEntity syncable) return false;

            syncable.SyncId = change.SyncId;
            syncable.UpdatedAt = change.UpdatedAt.ToUniversalTime();
            syncable.IsDeleted = false;

            // Prefer SyncId match; for seeded chart-of-accounts, local and cloud often
            // share the same Code with different SyncIds — fall back to natural key.
            var existing = FindLocalBySyncId(conn, descriptor, change.SyncId)
                           ?? FindLocalByNaturalKey(conn, descriptor, entity);

            if (existing == null)
            {
                ResetAutoIncrementId(entity);
                try
                {
                    conn.Insert(entity);
                }
                catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
                {
                    // Last-resort: unique conflict on insert (e.g. Account.Code) — rematch and update.
                    existing = FindLocalByNaturalKey(conn, descriptor, entity);
                    if (existing == null) throw;
                    CopyLocalId(existing, syncable);
                    conn.Update(entity);
                }
                return true;
            }

            CopyLocalId(existing, syncable);
            var serverUpdatedAt = change.UpdatedAt.ToUniversalTime();
            var localUpdatedAt = GetUpdatedAt(existing);

            if (localUpdatedAt > serverUpdatedAt)
            {
                var localJson = JsonSerializer.Serialize(existing, descriptor.ClrType, _jsonOptions);
                if (!string.Equals(localJson, change.PayloadJson, StringComparison.Ordinal))
                {
                    LogSyncConflict(conn, change.EntityType, change.SyncId, localJson, change.PayloadJson);
                    return false;
                }
            }

            if (localUpdatedAt <= serverUpdatedAt)
            {
                conn.Update(entity);
                return true;
            }

            return false;
        }

        public async Task<int> ResolvePendingConflictsAsync(string resolution, string username)
        {
            var pending = await _databaseService.Connection.Table<SyncConflictLog>()
                .Where(c => c.Resolution == "Pending")
                .ToListAsync();

            var resolved = 0;
            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                foreach (var conflict in pending)
                {
                    var descriptor = SyncEntityRegistry.All.FirstOrDefault(d => d.EntityType == conflict.EntityType);
                    if (descriptor == null) continue;

                    if (resolution.Equals("ServerWins", StringComparison.OrdinalIgnoreCase))
                    {
                        var entity = JsonSerializer.Deserialize(conflict.ServerPayloadJson, descriptor.ClrType, _jsonOptions);
                        if (entity is ISyncableEntity syncable)
                        {
                            var existing = FindLocalBySyncId(conn, descriptor, conflict.SyncId);
                            if (existing != null)
                            {
                                CopyLocalId(existing, syncable);
                                conn.Update(entity);
                            }
                            else
                            {
                                ResetAutoIncrementId(entity!);
                                conn.Insert(entity);
                            }
                        }
                    }

                    conflict.Resolution = resolution;
                    conflict.ResolvedAt = DateTime.UtcNow;
                    conn.Update(conflict);
                    resolved++;
                }
            });

            if (_auditService != null && resolved > 0)
            {
                await _auditService.LogActionAsync(username, "ResolveSyncConflicts", "SyncConflictLog", 0,
                    new { resolution, resolvedCount = resolved });
            }

            return resolved;
        }

        private void LogSyncConflict(SQLiteConnection conn, string entityType, Guid syncId, string localJson, string serverJson)
        {
            conn.Insert(new SyncConflictLog
            {
                EntityType = entityType,
                SyncId = syncId,
                LocalPayloadJson = localJson,
                ServerPayloadJson = serverJson,
                DetectedAt = DateTime.UtcNow,
                Resolution = "Pending"
            });
        }

        private static ISyncableEntity? FindLocalBySyncId(SQLiteConnection conn, SyncEntityDescriptor descriptor, Guid syncId)
        {
            return descriptor.EntityType switch
            {
                "Product" => conn.Table<Product>().FirstOrDefault(x => x.SyncId == syncId),
                "StockMovement" => conn.Table<StockMovement>().FirstOrDefault(x => x.SyncId == syncId),
                "Supplier" => conn.Table<Supplier>().FirstOrDefault(x => x.SyncId == syncId),
                "PurchaseOrder" => conn.Table<PurchaseOrder>().FirstOrDefault(x => x.SyncId == syncId),
                "PurchaseOrderItem" => conn.Table<PurchaseOrderItem>().FirstOrDefault(x => x.SyncId == syncId),
                "SalesOrder" => conn.Table<SalesOrder>().FirstOrDefault(x => x.SyncId == syncId),
                "SalesOrderItem" => conn.Table<SalesOrderItem>().FirstOrDefault(x => x.SyncId == syncId),
                "Category" => conn.Table<Category>().FirstOrDefault(x => x.SyncId == syncId),
                "Tax" => conn.Table<Tax>().FirstOrDefault(x => x.SyncId == syncId),
                "SupplierProduct" => conn.Table<SupplierProduct>().FirstOrDefault(x => x.SyncId == syncId),
                "Account" => conn.Table<Account>().FirstOrDefault(x => x.SyncId == syncId),
                "Journal" => conn.Table<Journal>().FirstOrDefault(x => x.SyncId == syncId),
                "JournalEntry" => conn.Table<JournalEntry>().FirstOrDefault(x => x.SyncId == syncId),
                "JournalLine" => conn.Table<JournalLine>().FirstOrDefault(x => x.SyncId == syncId),
                "ProductBundle" => conn.Table<ProductBundle>().FirstOrDefault(x => x.SyncId == syncId),
                "BillOfMaterial" => conn.Table<BillOfMaterial>().FirstOrDefault(x => x.SyncId == syncId),
                "BillOfMaterialLine" => conn.Table<BillOfMaterialLine>().FirstOrDefault(x => x.SyncId == syncId),
                "ManufacturingOrder" => conn.Table<ManufacturingOrder>().FirstOrDefault(x => x.SyncId == syncId),
                "ManufacturingOrderLine" => conn.Table<ManufacturingOrderLine>().FirstOrDefault(x => x.SyncId == syncId),
                "CustomerReturn" => conn.Table<CustomerReturn>().FirstOrDefault(x => x.SyncId == syncId),
                "SupplierReturn" => conn.Table<SupplierReturn>().FirstOrDefault(x => x.SyncId == syncId),
                "Location" => conn.Table<Location>().FirstOrDefault(x => x.SyncId == syncId),
                "LocationStock" => conn.Table<LocationStock>().FirstOrDefault(x => x.SyncId == syncId),
                "StockTransfer" => conn.Table<StockTransfer>().FirstOrDefault(x => x.SyncId == syncId),
                "Expense" => conn.Table<Expense>().FirstOrDefault(x => x.SyncId == syncId),
                "DamageWriteOff" => conn.Table<DamageWriteOff>().FirstOrDefault(x => x.SyncId == syncId),
                _ => null
            };
        }

        /// <summary>
        /// Match local rows by business unique key when SyncId differs (common for seeded Accounts).
        /// </summary>
        private static ISyncableEntity? FindLocalByNaturalKey(SQLiteConnection conn, SyncEntityDescriptor descriptor, object incoming)
        {
            return descriptor.EntityType switch
            {
                "Account" when incoming is Account a && !string.IsNullOrWhiteSpace(a.Code)
                    => conn.Table<Account>().FirstOrDefault(x => x.Code == a.Code),
                _ => null
            };
        }

        private static bool SoftDeleteLocalBySyncId(SQLiteConnection conn, SyncEntityDescriptor descriptor, Guid syncId, DateTime updatedAt)
        {
            var existing = FindLocalBySyncId(conn, descriptor, syncId);
            if (existing == null) return false;

            existing.IsDeleted = true;
            existing.UpdatedAt = updatedAt.ToUniversalTime();
            conn.Update(existing, existing.GetType());
            return true;
        }

        private static DateTime GetUpdatedAt(ISyncableEntity entity) => entity.UpdatedAt.ToUniversalTime();

        private static void ResetAutoIncrementId(object entity)
        {
            var idProp = entity.GetType().GetProperty("Id");
            if (idProp != null && idProp.CanWrite && idProp.PropertyType == typeof(int))
            {
                idProp.SetValue(entity, 0);
            }
        }

        private static void CopyLocalId(ISyncableEntity target, ISyncableEntity source)
        {
            var idProp = target.GetType().GetProperty("Id");
            var sourceIdProp = source.GetType().GetProperty("Id");
            if (idProp != null && sourceIdProp != null && idProp.CanWrite)
            {
                idProp.SetValue(source, idProp.GetValue(target));
            }
        }

        private async Task<SyncState> GetOrCreateSyncStateAsync()
        {
            var deviceId = await GetOrCreateDeviceIdAsync();
            var state = await _databaseService.Connection.Table<SyncState>().FirstOrDefaultAsync(s => s.DeviceId == deviceId);
            if (state != null) return state;

            state = new SyncState
            {
                DeviceId = deviceId,
                ApiBaseUrl = CloudSyncDefaults.ResolveApiBaseUrl(),
                LastSyncStatus = "Never synced"
            };
            await _databaseService.Connection.InsertAsync(state);
            return state;
        }

        private async Task<string> GetOrCreateDeviceIdAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cachedDeviceId))
            {
                return _cachedDeviceId;
            }

            var existing = await _databaseService.Connection.Table<SyncState>().FirstOrDefaultAsync();
            if (existing != null && !string.IsNullOrWhiteSpace(existing.DeviceId))
            {
                _cachedDeviceId = existing.DeviceId;
                return existing.DeviceId;
            }

            _cachedDeviceId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
            return _cachedDeviceId;
        }

        private static async Task<MemoryStream> CompressFileAsync(string sourcePath)
        {
            var output = new MemoryStream();
            await using (var input = File.OpenRead(sourcePath))
            await using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                await input.CopyToAsync(gzip);
            }

            output.Position = 0;
            return output;
        }

        private static async Task DecompressToFileAsync(string gzipPath, string outputPath)
        {
            await using var input = File.OpenRead(gzipPath);
            await using var gzip = new GZipStream(input, CompressionMode.Decompress);
            await using var output = File.Create(outputPath);
            await gzip.CopyToAsync(output);
        }
    }
}
