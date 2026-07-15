using System;
using System.IO;

namespace InventoryManagementSystem.Services
{
    public static class AppPaths
    {
        public static string GetLocalAppDataFolder() =>
            ResolveFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppBranding.AppDataFolder,
                AppBranding.LegacyAppDataFolder);

        public static string GetRoamingAppDataFolder() =>
            ResolveFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppBranding.AppDataFolder,
                AppBranding.LegacyAppDataFolder);

        public static string GetDocumentsRoot() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AppBranding.DocumentsFolder);

        public static string EnsureDocumentsSubfolder(params string[] segments)
        {
            var path = GetDocumentsRoot();
            foreach (var segment in segments)
            {
                path = Path.Combine(path, segment);
            }

            Directory.CreateDirectory(path);
            return path;
        }

        private static string ResolveFolder(string root, string preferred, string legacy)
        {
            var preferredPath = Path.Combine(root, preferred);
            var legacyPath = Path.Combine(root, legacy);

            if (Directory.Exists(preferredPath))
            {
                return preferredPath;
            }

            if (Directory.Exists(legacyPath))
            {
                return legacyPath;
            }

            Directory.CreateDirectory(preferredPath);
            return preferredPath;
        }
    }
}
