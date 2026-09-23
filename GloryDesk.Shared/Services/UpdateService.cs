using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
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
        public string CurrentVersion { get; private set; } = GetAppVersion();
        private bool _isInitialized = false;
        private readonly HttpClient _httpClient;
        private string _releaseNotesBaseUrl = $"{AppBranding.WebsiteUrl}/releases";
        private string _effectiveUpdateUrl = AppBranding.GitHubReleasesRepoUrl;
        private string? _fallbackDownloadUrl;
        private string? _fallbackVersion;

        public UpdateService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppBranding.ShortName}/UpdateService");
        }

        public static string GetAppVersion()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? typeof(UpdateService).Assembly;
                var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(infoVer))
                {
                    var plusIdx = infoVer.IndexOf('+');
                    if (plusIdx > 0) infoVer = infoVer.Substring(0, plusIdx);
                    return infoVer.Trim().TrimStart('v', 'V');
                }

                var ver = asm.GetName().Version;
                if (ver != null)
                {
                    return $"{ver.Major}.{ver.Minor}.{ver.Build}";
                }
            }
            catch { }
            return "1.2.0";
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
            _effectiveUpdateUrl = AppBranding.GitHubReleasesRepoUrl;
            _releaseNotesBaseUrl = $"{AppBranding.WebsiteUrl}/releases";

            try
            {
                var config = await _httpClient.GetFromJsonAsync<AppConfig>(AppBranding.PublicConfigUrl);
                if (config != null)
                {
                    if (!string.IsNullOrWhiteSpace(config.updateUrl))
                    {
                        _effectiveUpdateUrl = config.updateUrl.Trim();
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
            if (_effectiveUpdateUrl.Contains("ims-lilac-beta", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Ignoring legacy ims-lilac-beta update URL; using glorydesk GitHub releases.");
                _effectiveUpdateUrl = AppBranding.GitHubReleasesRepoUrl;
            }

            Console.WriteLine($"Initializing UpdateManager with Source: {_effectiveUpdateUrl}");

            try
            {
                IUpdateSource source;
                if (_effectiveUpdateUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                {
                    source = new GithubSource(_effectiveUpdateUrl, null, false);
                }
                else
                {
                    source = new SimpleWebSource(_effectiveUpdateUrl);
                }

                _mgr = new UpdateManager(source);
                if (_mgr.IsInstalled && _mgr.CurrentVersion != null)
                {
                    CurrentVersion = _mgr.CurrentVersion.ToString();
                }
                else
                {
                    CurrentVersion = GetAppVersion();
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize UpdateManager: {ex.Message}");
                CurrentVersion = GetAppVersion();
                _isInitialized = true;
            }
        }

        public async Task<UpdateResult> CheckForUpdatesAsync()
        {
            if (!_isInitialized) await InitializeAsync();

            // Skip for mobile and browser
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser())
            {
                return new UpdateResult(Success: false);
            }

            // 1. Try Velopack first IF the app was installed and is managed by Velopack
            if (_mgr != null && _mgr.IsInstalled)
            {
                try
                {
                    var newVersion = await _mgr.CheckForUpdatesAsync();
                    if (newVersion != null)
                    {
                        string targetVersion = newVersion.TargetFullRelease.Version.ToString();
                        string releaseNotesUrl = BuildReleaseNotesUrl(targetVersion);
                        return new UpdateResult(Success: true, Version: targetVersion, ReleaseNotesUrl: releaseNotesUrl);
                    }

                    // Velopack reports app is up to date
                    return new UpdateResult(Success: false, IsDevMode: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Velopack update check encountered error: {ex.Message}. Falling back to direct check.");
                }
            }

            // 2. If app is not installed via Velopack (e.g. Inno Setup install, portable build, or Velopack check failed):
            // Fall back to direct release check against GitHub / release assets.
            return await FallbackCheckForUpdatesAsync();
        }

        private async Task<UpdateResult> FallbackCheckForUpdatesAsync()
        {
            try
            {
                var currentVer = CurrentVersion;
                if (string.IsNullOrWhiteSpace(currentVer) || currentVer == "Unknown")
                {
                    currentVer = GetAppVersion();
                    CurrentVersion = currentVer;
                }

                var releaseInfo = await FetchLatestReleaseInfoAsync();
                if (releaseInfo == null)
                {
                    // Could not reach remote server (e.g. offline)
                    return new UpdateResult(Success: false, IsDevMode: false);
                }

                if (IsNewerVersion(releaseInfo.Version, currentVer))
                {
                    _fallbackDownloadUrl = releaseInfo.DownloadUrl;
                    _fallbackVersion = releaseInfo.Version;
                    string releaseNotesUrl = BuildReleaseNotesUrl(releaseInfo.Version);
                    return new UpdateResult(Success: true, Version: releaseInfo.Version, ReleaseNotesUrl: releaseNotesUrl);
                }

                // Currently on the latest version or newer!
                return new UpdateResult(Success: false, IsDevMode: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback update check error: {ex.Message}");
                return new UpdateResult(Success: false, IsDevMode: false);
            }
        }

        private record RemoteReleaseInfo(string Version, string DownloadUrl);

        private async Task<RemoteReleaseInfo?> FetchLatestReleaseInfoAsync()
        {
            string repoPath = "mtgloryco/glorydesk";
            if (_effectiveUpdateUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(_effectiveUpdateUrl);
                    repoPath = uri.AbsolutePath.Trim('/');
                }
                catch { }
            }

            // Attempt 1: GitHub Releases API
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repoPath}/releases/latest");
                req.Headers.Accept.ParseAdd("application/vnd.github.v3+json");
                req.Headers.UserAgent.ParseAdd($"{AppBranding.ShortName}/UpdateService");

                var resp = await _httpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("tag_name", out var tagProp))
                    {
                        var tag = tagProp.GetString() ?? "";
                        var version = tag.Trim().TrimStart('v', 'V');
                        string? downloadUrl = null;

                        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var asset in assets.EnumerateArray())
                            {
                                var name = asset.GetProperty("name").GetString() ?? "";
                                var url = asset.GetProperty("browser_download_url").GetString() ?? "";

                                if (OperatingSystem.IsWindows())
                                {
                                    // Prefer Inno Setup executable
                                    if (name.StartsWith("GloryDesk_Setup", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        downloadUrl = url;
                                        break;
                                    }
                                    if (name.EndsWith("-win-Setup.exe", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        downloadUrl ??= url;
                                    }
                                }
                                else if (OperatingSystem.IsLinux())
                                {
                                    if (name.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                                    {
                                        downloadUrl = url;
                                        break;
                                    }
                                }
                            }
                        }

                        downloadUrl ??= AppBranding.DownloadUrl;
                        return new RemoteReleaseInfo(version, downloadUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GitHub API release query failed: {ex.Message}. Trying releases.json fallback...");
            }

            // Attempt 2: Direct download of releases.win.json or releases.linux.json (avoids GitHub API rate limits)
            try
            {
                string jsonName = OperatingSystem.IsLinux() ? "releases.linux.json" : "releases.win.json";
                string jsonUrl = $"https://github.com/{repoPath}/releases/latest/download/{jsonName}";

                var jsonStr = await _httpClient.GetStringAsync(jsonUrl);
                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("Assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        if (asset.TryGetProperty("Version", out var vProp))
                        {
                            var version = vProp.GetString() ?? "";
                            return new RemoteReleaseInfo(version, AppBranding.DownloadUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Direct releases.json fetch failed: {ex.Message}");
            }

            return null;
        }

        public async Task DownloadAndRestartAsync()
        {
            if (!_isInitialized) await InitializeAsync();

            // 1. If Velopack is installed, let Velopack update and restart
            if (_mgr != null && _mgr.IsInstalled)
            {
                try
                {
                    var newVersion = await _mgr.CheckForUpdatesAsync();
                    if (newVersion != null)
                    {
                        await _mgr.DownloadUpdatesAsync(newVersion);
                        _mgr.ApplyUpdatesAndRestart(newVersion);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Velopack ApplyUpdates error: {ex.Message}. Trying installer fallback.");
                }
            }

            // 2. Fallback: If installed via Inno Setup or standalone
            if (!string.IsNullOrWhiteSpace(_fallbackDownloadUrl))
            {
                if (OperatingSystem.IsWindows() && _fallbackDownloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fileName = Path.GetFileName(new Uri(_fallbackDownloadUrl).LocalPath);
                        if (string.IsNullOrWhiteSpace(fileName)) fileName = "GloryDesk_Setup.exe";
                        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

                        Console.WriteLine($"Downloading installer to {tempPath} from {_fallbackDownloadUrl}...");
                        var bytes = await _httpClient.GetByteArrayAsync(_fallbackDownloadUrl);
                        await File.WriteAllBytesAsync(tempPath, bytes);

                        Console.WriteLine("Launching installer and exiting app...");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = tempPath,
                            UseShellExecute = true
                        });

                        // Exit current app so the installer can overwrite files
                        Environment.Exit(0);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to auto-download/run installer: {ex.Message}. Opening browser.");
                    }
                }

                // Fallback for Linux, Mac, or failed download: open browser
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _fallbackDownloadUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open download link: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = AppBranding.DownloadUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open website download page: {ex.Message}");
                }
            }
        }

        public static bool IsNewerVersion(string remoteVer, string localVer)
        {
            var cleanRemote = (remoteVer ?? "").Trim().TrimStart('v', 'V');
            var cleanLocal = (localVer ?? "").Trim().TrimStart('v', 'V');

            // Remove build metadata (e.g. +gitsha)
            var plusR = cleanRemote.IndexOf('+');
            if (plusR >= 0) cleanRemote = cleanRemote.Substring(0, plusR);
            var plusL = cleanLocal.IndexOf('+');
            if (plusL >= 0) cleanLocal = cleanLocal.Substring(0, plusL);

            if (Version.TryParse(cleanRemote, out var rVer) && Version.TryParse(cleanLocal, out var lVer))
            {
                return rVer > lVer;
            }

            var rParts = cleanRemote.Split('.');
            var lParts = cleanLocal.Split('.');
            int maxLen = Math.Max(rParts.Length, lParts.Length);

            for (int i = 0; i < maxLen; i++)
            {
                int rNum = i < rParts.Length && int.TryParse(rParts[i], out var rn) ? rn : 0;
                int lNum = i < lParts.Length && int.TryParse(lParts[i], out var ln) ? ln : 0;
                if (rNum > lNum) return true;
                if (rNum < lNum) return false;
            }

            return false;
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
