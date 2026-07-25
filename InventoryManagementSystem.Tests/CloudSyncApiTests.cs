using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryManagementSystem.Cloud.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InventoryManagementSystem.Tests;

public class CloudSyncApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    static CloudSyncApiTests()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    }

    public CloudSyncApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Register_Login_Push_Pull_Backup_Flow_Works()
    {
        var client = _factory.CreateClient();
        var email = $"sync-{Guid.NewGuid():N}@example.com";
        const string password = "Secret123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Test Org"));
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var pushResponse = await client.PostAsJsonAsync("/api/sync/push", new SyncPushRequest(
            "test-device",
            new List<SyncChangeRecord>
            {
                new(
                    "Product",
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    JsonSerializer.Serialize(new
                    {
                        SyncId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "Server Product",
                        SKU = "SP-1",
                        Unit = "Pcs",
                        Price = 12.5m,
                        Cost = 7m,
                        StockQuantity = 3,
                        Category = "General",
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }),
                    DateTime.UtcNow,
                    false)
            }));

        pushResponse.EnsureSuccessStatusCode();
        var pushResult = await pushResponse.Content.ReadFromJsonAsync<SyncPushResponse>();
        Assert.NotNull(pushResult);
        Assert.Equal(1, pushResult.Accepted);

        var pullResponse = await client.GetAsync("/api/sync/pull?since=1970-01-01T00:00:00Z");
        pullResponse.EnsureSuccessStatusCode();
        var pullResult = await pullResponse.Content.ReadFromJsonAsync<SyncPullResponse>();
        Assert.NotNull(pullResult);
        Assert.NotEmpty(pullResult.Changes);

        await using var backupStream = new MemoryStream(new byte[] { 0x1f, 0x8b, 0x08, 0x00 });
        using var backupContent = new StreamContent(backupStream);
        backupContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
        var backupUpload = await client.PostAsync("/api/backup", backupContent);
        backupUpload.EnsureSuccessStatusCode();

        var backupInfo = await client.GetFromJsonAsync<BackupInfoResponse>("/api/backup/info");
        Assert.NotNull(backupInfo);
        Assert.True(backupInfo.Exists);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task License_Request_And_Static_Portal_Work()
    {
        var client = _factory.CreateClient();

        var home = await client.GetAsync("/");
        home.EnsureSuccessStatusCode();
        var homeHtml = await home.Content.ReadAsStringAsync();
        Assert.Contains("Glory Desk", homeHtml);

        var request = await client.PostAsJsonAsync("/api/license/request", new LicenseRequestDto(
            "shop@example.com",
            "Test Shop Ltd",
            "Pro",
            "HWID-TEST-12345678"));
        request.EnsureSuccessStatusCode();
        var body = await request.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());

        var badTier = await client.PostAsJsonAsync("/api/license/request", new LicenseRequestDto(
            "shop@example.com",
            "Test Shop Ltd",
            "Invalid",
            "HWID-TEST-12345678"));
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, badTier.StatusCode);
    }

    [Fact]
    public async Task Admin_Can_Issue_License_Key()
    {
        var client = _factory.CreateClient();
        const string adminKey = "dev-admin-key-change-in-production";
        var email = $"license-{Guid.NewGuid():N}@example.com";

        var submit = await client.PostAsJsonAsync("/api/license/request", new LicenseRequestDto(
            email,
            "Issue Test Co",
            "Basic",
            "ABCDEF1234567890ABCDEF12"));
        submit.EnsureSuccessStatusCode();

        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/admin/license-requests?status=pending");
        adminRequest.Headers.Add("X-Admin-Key", adminKey);
        var listResponse = await client.SendAsync(adminRequest);
        listResponse.EnsureSuccessStatusCode();
        var requests = await listResponse.Content.ReadFromJsonAsync<List<LicenseRequestRecord>>();
        Assert.NotNull(requests);
        var pending = requests.First(r => r.Email == email);

        using var issueRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/license-requests/{pending.Id}/issue");
        issueRequest.Headers.Add("X-Admin-Key", adminKey);
        issueRequest.Content = JsonContent.Create(new AdminIssueRequest(1));
        var issueResponse = await client.SendAsync(issueRequest);
        issueResponse.EnsureSuccessStatusCode();
        var issue = await issueResponse.Content.ReadFromJsonAsync<AdminIssueResponse>();
        Assert.NotNull(issue);
        Assert.True(issue.Success);
        Assert.False(string.IsNullOrWhiteSpace(issue.LicenseKey));
        Assert.Contains('.', issue.LicenseKey);
    }
}
