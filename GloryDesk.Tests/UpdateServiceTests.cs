using System.Threading.Tasks;
using InventoryManagementSystem.Services;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.2.0", "1.1.0", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.1.1", "1.1.0", true)]
    [InlineData("v1.2.0", "1.1.0", true)]
    [InlineData("1.2.0", "v1.1.0", true)]
    [InlineData("v1.2.0+build123", "v1.1.0", true)]
    [InlineData("1.1.0", "1.1.0", false)]
    [InlineData("1.0.9", "1.1.0", false)]
    [InlineData("v1.0.0", "1.1.0", false)]
    public void IsNewerVersion_ComparesSemVerCorrectly(string remote, string local, bool expected)
    {
        var result = UpdateService.IsNewerVersion(remote, local);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetAppVersion_ReturnsValidVersionString()
    {
        var version = UpdateService.GetAppVersion();
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain("v", version, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_DoesNotThrowAndHandlesUninstalledState()
    {
        var service = new UpdateService();
        var result = await service.CheckForUpdatesAsync();

        // Should return an UpdateResult without throwing
        Assert.NotNull(result);
        // Even when not installed by Velopack, it should never report Dev Mode if fallback handles it
        Assert.False(result.IsDevMode);
    }
}
