using System;
using System.IO;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using SQLite;

namespace InventoryManagementSystem.Infrastructure
{
    public class DatabaseService
    {
        private readonly string _databasePath;
        private SQLiteAsyncConnection _connection;

        public DatabaseService()
        {
            // Store database in the application folder (Portable Mode)
            var folder = AppDomain.CurrentDomain.BaseDirectory;
            // var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InventoryApp");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _databasePath = Path.Combine(folder, "inventory_v1.db");
            _connection = new SQLiteAsyncConnection(_databasePath);
        }

        public async Task InitializeAsync()
        {
            await _connection.CreateTableAsync<Product>();
            await _connection.CreateTableAsync<Category>();
            await _connection.CreateTableAsync<StockMovement>();
            await _connection.CreateTableAsync<PurchaseBatch>();
            await _connection.CreateTableAsync<SaleBatchUsage>();
            await _connection.CreateTableAsync<User>();
            await _connection.CreateTableAsync<LocalLicense>();

            await SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            // App starts clean for production. No test categories or products seeded.
            await Task.CompletedTask;
        }

        public SQLiteAsyncConnection Connection => _connection;
    }
}
