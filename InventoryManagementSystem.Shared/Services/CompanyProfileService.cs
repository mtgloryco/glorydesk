using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InventoryManagementSystem.Services
{
    /// <summary>
    /// One local SQLite file, representing one company/organization's data on this machine.
    /// </summary>
    public class CompanyProfile
    {
        public string? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string DbFileName { get; set; } = "inventory.db";
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    }

    public class CompanyProfileRegistry
    {
        public List<CompanyProfile> Profiles { get; set; } = new();
        public string? ActiveDbFileName { get; set; }
    }

    /// <summary>
    /// Resolves which local database file this install should open, and lets a user add an
    /// isolated file for a second company (e.g. an employee who moves to a different
    /// organization but keeps using the same installed app). Deliberately does NOT hot-swap
    /// the live database connection — the composition root in App.axaml.cs wires ~50 services
    /// to a single DatabaseService instance at startup, so switching which file is active
    /// requires an app restart (the same "restart recommended" pattern already used after
    /// cloud restore).
    ///
    /// When no registry file exists yet (every install today), <see cref="ResolveActiveDatabasePath"/>
    /// returns the original fixed "inventory.db" path unchanged — single-company installs are
    /// completely unaffected by this feature.
    /// </summary>
    public static class CompanyProfileService
    {
        private const string DefaultDbFileName = "inventory.db";
        private const string RegistryFileName = "company-profiles.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string RegistryPath => Path.Combine(AppPaths.GetLocalAppDataFolder(), RegistryFileName);

        public static string ResolveActiveDatabasePath()
        {
            var folder = AppPaths.GetLocalAppDataFolder();
            var registry = LoadRegistry();

            if (registry == null || registry.Profiles.Count == 0)
            {
                return Path.Combine(folder, DefaultDbFileName);
            }

            var activeFileName = !string.IsNullOrWhiteSpace(registry.ActiveDbFileName)
                ? registry.ActiveDbFileName!
                : registry.Profiles[0].DbFileName;

            return Path.Combine(folder, activeFileName);
        }

        public static IReadOnlyList<CompanyProfile> ListProfiles()
        {
            var registry = LoadRegistry();
            if (registry == null || registry.Profiles.Count == 0)
            {
                return new List<CompanyProfile> { new() { DbFileName = DefaultDbFileName } };
            }

            return registry.Profiles;
        }

        public static string? GetActiveDbFileName() => LoadRegistry()?.ActiveDbFileName ?? DefaultDbFileName;

        /// <summary>
        /// Creates a brand-new, empty local database file for a different organization and marks
        /// it active for the next launch. The currently running app keeps using its current file
        /// until restarted — callers must tell the user to restart.
        /// </summary>
        public static CompanyProfile CreateProfileForNextLaunch(string organizationName)
        {
            var registry = LoadRegistry() ?? new CompanyProfileRegistry();

            if (registry.Profiles.Count == 0)
            {
                // First time this install uses multiple profiles: register the existing file
                // as-is so switching back to "the original company" later remains possible.
                registry.Profiles.Add(new CompanyProfile
                {
                    DbFileName = DefaultDbFileName,
                    LastUsedAt = DateTime.UtcNow
                });
                registry.ActiveDbFileName = DefaultDbFileName;
            }

            var fileName = $"inventory-{Guid.NewGuid():N}.db";
            var profile = new CompanyProfile
            {
                OrganizationName = string.IsNullOrWhiteSpace(organizationName) ? null : organizationName.Trim(),
                DbFileName = fileName,
                LastUsedAt = DateTime.UtcNow
            };

            registry.Profiles.Add(profile);
            registry.ActiveDbFileName = fileName;
            SaveRegistry(registry);
            return profile;
        }

        /// <summary>Marks an existing profile active for the next launch (switch back to a company
        /// already set up on this machine). Returns false if the file name isn't a known profile.</summary>
        public static bool SwitchToProfileForNextLaunch(string dbFileName)
        {
            var registry = LoadRegistry();
            if (registry == null || registry.Profiles.All(p => p.DbFileName != dbFileName))
            {
                return false;
            }

            registry.ActiveDbFileName = dbFileName;
            var profile = registry.Profiles.First(p => p.DbFileName == dbFileName);
            profile.LastUsedAt = DateTime.UtcNow;
            SaveRegistry(registry);
            return true;
        }

        private static CompanyProfileRegistry? LoadRegistry()
        {
            try
            {
                if (!File.Exists(RegistryPath)) return null;
                var json = File.ReadAllText(RegistryPath);
                return JsonSerializer.Deserialize<CompanyProfileRegistry>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveRegistry(CompanyProfileRegistry registry)
        {
            var folder = AppPaths.GetLocalAppDataFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(RegistryPath, JsonSerializer.Serialize(registry, JsonOptions));
        }
    }
}
