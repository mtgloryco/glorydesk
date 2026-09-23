using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class UserLoginTests : IAsyncLifetime
{
    private readonly string _dbPath = TempFile.CreateDbPath();
    private DatabaseService _db = null!;
    private UserService _userService = null!;

    public async Task InitializeAsync()
    {
        _db = new DatabaseService(_dbPath);
        await _db.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.CloseConnectionAsync();
        TempFile.DeleteDbFiles(_dbPath);
    }

    [Fact]
    public async Task InitializeAsync_RepairsLegacyPlaintextAdminPassword()
    {
        await _db.Connection.InsertAsync(new User
        {
            Username = "admin",
            Role = "Admin",
            PasswordHash = "admin123",
            IsActive = true
        });

        _userService = new UserService(_db);
        await _userService.InitializeAsync();

        var user = await _userService.AuthenticateAsync("admin", "admin123");
        Assert.NotNull(user);
        Assert.Equal("Admin", user!.Role);
    }
}
