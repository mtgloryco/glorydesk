using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem;

[SupportedOSPlatform("browser")]
public partial class BrowserSessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public UserSessionData? GetSession()
    {
        try
        {
            var raw = BrowserInterop.GetSession();
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return JsonSerializer.Deserialize<UserSessionData>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BrowserSessionStore] Error reading session: {ex.Message}");
            return null;
        }
    }

    public void SaveSession(UserSessionData session)
    {
        try
        {
            var json = JsonSerializer.Serialize(session, JsonOptions);
            BrowserInterop.SetSession(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BrowserSessionStore] Error saving session: {ex.Message}");
        }
    }

    public void ClearSession()
    {
        try
        {
            BrowserInterop.ClearSession();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BrowserSessionStore] Error clearing session: {ex.Message}");
        }
    }
}

[SupportedOSPlatform("browser")]
internal static partial class BrowserInterop
{
    [JSImport("session.get", "main.js")]
    internal static partial string? GetSession();

    [JSImport("session.set", "main.js")]
    internal static partial void SetSession(string data);

    [JSImport("session.clear", "main.js")]
    internal static partial void ClearSession();

    [JSImport("settings.get", "main.js")]
    internal static partial string? GetSettings();

    [JSImport("settings.set", "main.js")]
    internal static partial void SetSettings(string data);
}
