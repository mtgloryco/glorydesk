using System.Text;
using InventoryManagementSystem.Cloud.Data;
using InventoryManagementSystem.Cloud.Models;
using InventoryManagementSystem.Cloud.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CloudDatabase>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<LicenseRequestService>();
builder.Services.AddSingleton<LicenseKeyGeneratorService>();
builder.Services.AddScoped<LicenseAdminService>();
builder.Services.AddScoped<LicenseSeatService>();
builder.Services.AddSingleton<EmailNotificationService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

var db = app.Services.GetRequiredService<CloudDatabase>();
await db.InitializeAsync();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

static bool IsAdmin(HttpContext ctx, IConfiguration config)
{
    var expected = config["Admin:ApiKey"];
    if (string.IsNullOrWhiteSpace(expected)) return false;
    return string.Equals(ctx.Request.Headers["X-Admin-Key"].FirstOrDefault(), expected, StringComparison.Ordinal);
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Glory Desk Cloud API",
    utc = DateTime.UtcNow
}));

app.MapGet("/api", () => Results.Ok(new
{
    service = "Glory Desk Cloud API",
    product = "Glory Desk",
    publisher = "MT GLORY CO",
    portal = "/",
    activate = "/activate.html",
    download = "/download.html",
    account = "/account/login.html",
    admin = "/admin/",
    health = "/health"
}));

app.MapGet("/api/pricing", (IConfiguration config) => Results.Ok(new
{
    currency = "RWF",
    tiers = new[]
    {
        new { name = "Basic", products = (object)50, locations = (object)1, price = config["Pricing:BasicRwf"], period = "year" },
        new { name = "Medium", products = (object)500, locations = (object)3, price = config["Pricing:MediumRwf"], period = "year" },
        new { name = "Pro", products = (object)"Unlimited", locations = (object)"Unlimited", price = config["Pricing:ProRwf"], period = "year" },
        new { name = "Enterprise", products = (object)"Unlimited", locations = (object)"Unlimited", price = config["Pricing:EnterpriseRwf"], period = "year" }
    },
    note = "Prices exclude VAT. Enterprise includes cloud sync and priority support."
}));

app.MapGet("/api/downloads/info", (IConfiguration config, IWebHostEnvironment env) =>
{
    var fileName = config["Downloads:WindowsInstallerFile"] ?? "GloryDesk_Setup_v1.0.1_Windows.exe";
    var localPath = Path.Combine(env.WebRootPath, "downloads", fileName);
    var exists = File.Exists(localPath);
    return Results.Ok(new
    {
        platform = "Windows",
        version = config["Downloads:WindowsVersion"] ?? "1.0.1",
        fileName,
        sizeMb = config["Downloads:WindowsSizeMb"] ?? "102",
        available = exists,
        downloadUrl = exists ? $"/downloads/{fileName}" : config["Downloads:GitHubReleaseUrl"],
        requirements = new[] { "Windows 10/11 64-bit", "4 GB RAM minimum", "500 MB disk space" }
    });
});

app.MapPost("/api/license/request", async (LicenseRequestDto request, LicenseRequestService licenseRequests) =>
{
    var result = await licenseRequests.SubmitAsync(request);
    return result.Success
        ? Results.Ok(new { success = true, message = result.Message })
        : Results.BadRequest(new { success = false, message = result.Message });
});

app.MapGet("/api/admin/license-requests", async (HttpContext ctx, IConfiguration config, LicenseAdminService admin, string? status) =>
{
    if (!IsAdmin(ctx, config)) return Results.Unauthorized();
    var items = await admin.ListRequestsAsync(status);
    return Results.Ok(items);
});

app.MapPost("/api/admin/license-requests/{id:guid}/issue", async (
    Guid id, HttpContext ctx, IConfiguration config, LicenseAdminService admin, AdminIssueRequest body) =>
{
    if (!IsAdmin(ctx, config)) return Results.Unauthorized();
    var result = await admin.IssueAsync(id, body);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/admin/license-requests/{id:guid}/reject", async (
    Guid id, HttpContext ctx, IConfiguration config, LicenseAdminService admin, AdminIssueRequest? body) =>
{
    if (!IsAdmin(ctx, config)) return Results.Unauthorized();
    var result = await admin.RejectAsync(id, body?.Notes);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/account/licenses", async (HttpContext http, LicenseAdminService admin) =>
{
    var email = http.User.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrWhiteSpace(email)) return Results.Unauthorized();
    var licenses = await admin.GetLicensesForEmailAsync(email);
    return Results.Ok(licenses);
}).RequireAuthorization();

// Self-service multi-seat activation: proves ownership with (LicenseId + account email),
// both of which were emailed to the customer when the license was first issued. No admin
// approval needed per machine as long as seats remain.
app.MapPost("/api/license/activate", async (LicenseActivateRequest request, LicenseSeatService seats) =>
{
    var result = await seats.ActivateAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/license/deactivate", async (LicenseDeactivateRequest request, LicenseSeatService seats) =>
{
    var result = await seats.DeactivateAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/admin/license-requests/{id:guid}/activations", async (
    Guid id, HttpContext ctx, IConfiguration config, LicenseAdminService admin) =>
{
    if (!IsAdmin(ctx, config)) return Results.Unauthorized();
    var activations = await admin.GetActivationsAsync(id);
    return Results.Ok(activations);
});

app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService auth) =>
{
    var result = await auth.RegisterAsync(request);
    return result is null ? Results.BadRequest(new { error = "Registration failed. Email may already exist." }) : Results.Ok(result);
});

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth) =>
{
    var result = await auth.LoginAsync(request);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapGet("/api/backup/info", async (HttpContext http, BackupService backup) =>
{
    var orgId = CloudClaims.GetOrganizationId(http.User);
    if (orgId == Guid.Empty) return Results.Unauthorized();
    return Results.Ok(await backup.GetInfoAsync(orgId));
}).RequireAuthorization();

app.MapPost("/api/backup", async (HttpContext http, BackupService backup) =>
{
    var orgId = CloudClaims.GetOrganizationId(http.User);
    if (orgId == Guid.Empty) return Results.Unauthorized();

    await using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    ms.Position = 0;

    if (ms.Length == 0) return Results.BadRequest(new { error = "Empty backup payload." });

    await backup.SaveBackupAsync(orgId, ms, ms.Length);
    return Results.Ok(new { success = true, sizeBytes = ms.Length });
}).RequireAuthorization();

app.MapGet("/api/backup", async (HttpContext http, BackupService backup) =>
{
    var orgId = CloudClaims.GetOrganizationId(http.User);
    if (orgId == Guid.Empty) return Results.Unauthorized();

    var backupFile = await backup.GetBackupAsync(orgId);
    if (backupFile is null) return Results.NotFound(new { error = "No backup found for this organization." });

    var (stream, fileName) = backupFile.Value;
    return Results.File(stream, "application/gzip", fileName);
}).RequireAuthorization();

app.MapPost("/api/sync/push", async (HttpContext http, SyncService sync, SyncPushRequest request) =>
{
    var orgId = CloudClaims.GetOrganizationId(http.User);
    if (orgId == Guid.Empty) return Results.Unauthorized();

    var accepted = await sync.PushAsync(orgId, request);
    return Results.Ok(new SyncPushResponse(accepted));
}).RequireAuthorization();

app.MapGet("/api/sync/pull", async (HttpContext http, SyncService sync, DateTime? since) =>
{
    var orgId = CloudClaims.GetOrganizationId(http.User);
    if (orgId == Guid.Empty) return Results.Unauthorized();

    var sinceUtc = since ?? DateTime.MinValue;
    var response = await sync.PullAsync(orgId, sinceUtc.ToUniversalTime());
    return Results.Ok(response);
}).RequireAuthorization();

app.Run();

public partial class Program;
