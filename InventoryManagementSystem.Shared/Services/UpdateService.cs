using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace InventoryManagementSystem.Services
{
    public class AppConfig
    {
        /// <summary>Velopack update feed — GitHub repo URL or static web updates folder.</summary>
        public string updateUrl { get; set; } = AppBranding.GitHubReleasesRepoUrl;

        /// <summary>Optional base URL for human-readable release notes (glorydesk.mtglory.com/releases).</summary>
        public string releaseNotesBaseUrl { get; set; } = $"{AppBranding.WebsiteUrl}/releases";
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
        public string CurrentVersion { get; private set; } = "1.0.0";
        private bool _isInitialized = false;
        private readonly HttpClient _httpClient;
        private string _releaseNotesBaseUrl = $"{AppBranding.WebsiteUrl}/releases";

        public UpdateService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppBranding.ShortName}/UpdateService");
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            // On Android/Mobile, Velopack might not work or be relevant yet. The browser build has
            // no installer to update at all, and Velopack's native calls can raise WASM traps that
            // bypass .NET try/catch entirely (observed as an "Uncaught RuntimeError: unreachable"
            // that kills the whole runtime) — never construct an UpdateManager there.
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser())
            {
                CurrentVersion = "1.0-Web";
                _isInitialized = true;
                return;
            }

            // Default: Velopack assets published on the glorydesk GitHub releases feed
            string updateUrl = AppBranding.GitHubReleasesRepoUrl;
            _releaseNotesBaseUrl = $"{AppBranding.WebsiteUrl}/releases";

            try
            {
                var config = await _httpClient.GetFromJsonAsync<AppConfig>(AppBranding.PublicConfigUrl);
                if (config != null)
                {
                    if (!string.IsNullOrWhiteSpace(config.updateUrl))
                    {
                        updateUrl = config.updateUrl.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(config.releaseNotesBaseUrl))
                    {
                        _releaseNotesBaseUrl = config.releaseNotesBaseUrl.Trim().TrimEnd('/');
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch Glory Desk public config: {ex.Message}. Using GitHub releases default.");
            }

            // Never fall back to the retired ims-lilac-beta feed
            if (updateUrl.Contains("ims-lilac-beta", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Ignoring legacy ims-lilac-beta update URL; using glorydesk GitHub releases.");
                updateUrl = AppBranding.GitHubReleasesRepoUrl;
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
                CurrentVersion = _mgr.CurrentVersion?.ToString() ?? "1.0.0";
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
                string releaseNotesUrl = BuildReleaseNotesUrl(targetVersion);

                return new UpdateResult(Success: true, Version: targetVersion, ReleaseNotesUrl: releaseNotesUrl);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not installed", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Update check skipped (App is not installed/packaged).");
                    return new UpdateResult(Success: false, IsDevMode: true);
                }

                Console.WriteLine($"Error checking for updates: {ex.Message}");
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

        private string BuildReleaseNotesUrl(string version)
        {
            var clean = (version ?? string.Empty).Trim().TrimStart('v', 'V');
            if (string.IsNullOrWhiteSpace(_releaseNotesBaseUrl))
            {
                return AppBranding.ReleaseNotesUrlForVersion(clean);
            }

            // Prefer Glory Desk site pages; append version when using the download page
            if (_releaseNotesBaseUrl.Contains("glorydesk.", StringComparison.OrdinalIgnoreCase))
            {
                if (_releaseNotesBaseUrl.Contains("/releases", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{_releaseNotesBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(clean)}";
                }

                return $"{_releaseNotesBaseUrl}?v={Uri.EscapeDataString(clean)}";
            }

            return AppBranding.ReleaseNotesUrlForVersion(clean);
        }
    }
}
