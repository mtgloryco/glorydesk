using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace InventoryManagementSystem.Services
{
    public class AppConfig
    {
        public string updateUrl { get; set; } = "https://ims-lilac-beta.vercel.app/updates";
    }

    public class UpdateResult
    {
        public bool Success { get; }
        public bool IsDevMode { get; }
        public string Version { get; }
        public string ReleaseNotesUrl { get; }

        public UpdateResult(bool Success, bool IsDevMode = false, string Version = "", string ReleaseNotesUrl = "")
        {
            this.Success = Success;
            this.IsDevMode = IsDevMode;
            this.Version = Version;
            this.ReleaseNotesUrl = ReleaseNotesUrl;
        }
    }

    public class UpdateService
    {
        private UpdateManager? _mgr;
        public string CurrentVersion { get; private set; } = "1.2.1";
        private bool _isInitialized = false;
        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _httpClient = new HttpClient();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            // On Android/Mobile, Velopack might not work or be relevant yet.
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            {
                CurrentVersion = "1.0-Mobile";
                _isInitialized = true;
                return; 
            }

            string updateUrl = "https://ims-lilac-beta.vercel.app/updates"; // Default fallback

            try
            {
                var config = await _httpClient.GetFromJsonAsync<AppConfig>("https://ims-lilac-beta.vercel.app/api/public/config");
                if (config != null && !string.IsNullOrWhiteSpace(config.updateUrl))
                {
                    updateUrl = config.updateUrl;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch remote config: {ex.Message}. Using default URL.");
            }

            Console.WriteLine($"Initializing UpdateManager with Source: {updateUrl}");

            try 
            {
                IUpdateSource source;
                if (updateUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                {
                    source = new GithubSource(updateUrl, null, false);
                }
                else
                {
                    source = new SimpleWebSource(updateUrl);
                }

                _mgr = new UpdateManager(source);
                CurrentVersion = _mgr.CurrentVersion?.ToString() ?? "1.2.1";
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize UpdateManager: {ex.Message}");
                CurrentVersion = "Unknown";
            }
        }

        public async Task<UpdateResult> CheckForUpdatesAsync()
        {
            if (!_isInitialized) await InitializeAsync();

             // Skip for mobile for now
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            {
                 return new UpdateResult(Success: false);
            }

            try
            {
                if (_mgr == null) return new UpdateResult(Success: false, IsDevMode: true);

                var newVersion = await _mgr.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    return new UpdateResult(Success: false);
                }

                string targetVersion = newVersion.TargetFullRelease.Version.ToString();
                string releaseNotesUrl = $"https://ims-lilac-beta.vercel.app/releases/{targetVersion}";

                return new UpdateResult(Success: true, Version: targetVersion, ReleaseNotesUrl: releaseNotesUrl);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not installed", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Update check skipped (App is not installed/packaged).");
                    return new UpdateResult(Success: false, IsDevMode: true);
                }
                else
                {
                    Console.WriteLine($"Error checking for updates: {ex.Message}");
                }
                return new UpdateResult(Success: false);
            }
        }

        public async Task DownloadAndRestartAsync()
        {
            if (!_isInitialized) await InitializeAsync();
            
            try
            {
                if (_mgr == null) return;

                var newVersion = await _mgr.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await _mgr.DownloadUpdatesAsync(newVersion);
                    _mgr.ApplyUpdatesAndRestart(newVersion);
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error applying updates: {ex.Message}");
            }
        }
    }
}
