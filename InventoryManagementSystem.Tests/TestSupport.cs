using System;
using System.IO;

namespace InventoryManagementSystem.Tests;

/// <summary>
/// Helpers for creating and cleaning up per-test scratch files (SQLite DB / settings.json)
/// so tests never touch the real per-user AppData locations and never pollute each other.
/// </summary>
internal static class TempFile
{
    public static string CreateDbPath() =>
        Path.Combine(Path.GetTempPath(), $"ims_test_{Guid.NewGuid():N}.db");

    public static string CreateSettingsPath() =>
        Path.Combine(Path.GetTempPath(), $"ims_test_settings_{Guid.NewGuid():N}.json");

    public static void DeleteDbFiles(string dbPath)
    {
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            DeleteFile(dbPath + suffix);
        }
    }

    public static void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; a leftover temp file should never fail a test run.
        }
    }
}
