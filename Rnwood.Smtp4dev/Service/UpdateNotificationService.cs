using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.Service
{
    public class UpdateNotificationService
    {
        private readonly ILogger<UpdateNotificationService> _logger;
        private readonly ServerOptions _serverOptions;
        private readonly Smtp4devDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly string _currentVersion;
        private static readonly bool _isPrerelease;
        private static readonly string _prereleasePrefix;

        static UpdateNotificationService()
        {
            var infoVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            _currentVersion = infoVersion ?? "0.0.0";
            
            // Check if current version is a prerelease (contains - after version number)
            var match = Regex.Match(_currentVersion, @"^(\d+\.\d+\.\d+)(?:-([^+]+))?");
            _isPrerelease = match.Success && !string.IsNullOrEmpty(match.Groups[2].Value);
            _prereleasePrefix = match.Success && _isPrerelease ? match.Groups[2].Value.Split('.')[0] : "";
        }

        public UpdateNotificationService(
            ILogger<UpdateNotificationService> logger,
            ServerOptions serverOptions,
            Smtp4devDbContext dbContext,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _serverOptions = serverOptions;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(string username = null)
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = _currentVersion
            };

            try
            {
                // Get last seen version from database or default
                UserVersionInfo userVersionInfo = null;
                if (!string.IsNullOrEmpty(username))
                {
                    userVersionInfo = _dbContext.UserVersionInfos
                        .FirstOrDefault(u => u.Username == username);
                }

                var lastSeenVersion = userVersionInfo?.LastSeenVersion;
                result.LastSeenVersion = lastSeenVersion;

                // Fetch releases from GitHub
                var releases = await FetchGitHubReleasesAsync();
                
                if (releases == null || releases.Count == 0)
                {
                    return result;
                }

                // Filter releases based on prerelease rules
                var relevantReleases = FilterRelevantReleases(releases);

                // Check for what's new
                if (!_serverOptions.DisableWhatsNewNotifications)
                {
                    result.ShowWhatsNew = ShouldShowWhatsNew(userVersionInfo, relevantReleases);
                    if (result.ShowWhatsNew)
                    {
                        result.NewReleases = GetReleasesSince(relevantReleases, lastSeenVersion, 10);
                    }
                }

                // Check for updates
                if (!_serverOptions.DisableUpdateNotifications)
                {
                    var newerReleases = GetNewerReleases(relevantReleases, _currentVersion);
                    if (newerReleases.Any())
                    {
                        result.UpdateAvailable = true;
                        result.NewReleases = newerReleases;
                    }
                }

                // Update last checked date
                if (userVersionInfo != null)
                {
                    userVersionInfo.LastCheckedDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for updates");
            }

            return result;
        }

        public async Task MarkVersionAsSeenAsync(string username, string version)
        {
            var userVersionInfo = _dbContext.UserVersionInfos
                .FirstOrDefault(u => u.Username == username);

            if (userVersionInfo == null)
            {
                userVersionInfo = new UserVersionInfo
                {
                    Id = Guid.NewGuid(),
                    Username = username ?? "anonymous",
                    LastSeenVersion = version,
                    LastCheckedDate = DateTime.UtcNow,
                    WhatsNewDismissed = false,
                    UpdateNotificationDismissed = false
                };
                _dbContext.UserVersionInfos.Add(userVersionInfo);
            }
            else
            {
                userVersionInfo.LastSeenVersion = version;
                userVersionInfo.WhatsNewDismissed = true;
                userVersionInfo.UpdateNotificationDismissed = true;
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task<List<GitHubRelease>> FetchGitHubReleasesAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", $"smtp4dev/{_currentVersion}");
                
                var response = await client.GetAsync("https://api.github.com/repos/rnwood/smtp4dev/releases");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch GitHub releases: {StatusCode}", response.StatusCode);
                    return new List<GitHubRelease>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<List<JsonElement>>(json);

                return releases?.Select(r => new GitHubRelease
                {
                    TagName = r.GetProperty("tag_name").GetString(),
                    Name = r.GetProperty("name").GetString(),
                    Body = r.TryGetProperty("body", out var body) ? body.GetString() : "",
                    Prerelease = r.GetProperty("prerelease").GetBoolean(),
                    PublishedAt = r.GetProperty("published_at").GetString(),
                    HtmlUrl = r.GetProperty("html_url").GetString()
                }).ToList() ?? new List<GitHubRelease>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GitHub releases");
                return new List<GitHubRelease>();
            }
        }

        private List<GitHubRelease> FilterRelevantReleases(List<GitHubRelease> releases)
        {
            if (!_isPrerelease)
            {
                // For stable versions, only include non-prerelease versions
                return releases.Where(r => !r.Prerelease).ToList();
            }
            else
            {
                // For prerelease versions, include same prerelease type
                return releases.Where(r => 
                {
                    if (!r.Prerelease) return false;
                    
                    var match = Regex.Match(r.TagName, @"^v?(\d+\.\d+\.\d+)-([^+]+)");
                    if (!match.Success) return false;
                    
                    var releasePrefix = match.Groups[2].Value.Split('.')[0];
                    return releasePrefix == _prereleasePrefix;
                }).ToList();
            }
        }

        private bool ShouldShowWhatsNew(UserVersionInfo userVersionInfo, List<GitHubRelease> releases)
        {
            if (userVersionInfo == null || string.IsNullOrEmpty(userVersionInfo.LastSeenVersion))
            {
                // First time user
                return true;
            }

            if (userVersionInfo.WhatsNewDismissed && 
                userVersionInfo.LastSeenVersion == _currentVersion)
            {
                // User has already dismissed for current version
                return false;
            }

            // Check if there are releases since last seen version
            return GetReleasesSince(releases, userVersionInfo.LastSeenVersion, 1).Any();
        }

        private List<GitHubRelease> GetReleasesSince(List<GitHubRelease> releases, string sinceVersion, int maxCount)
        {
            if (string.IsNullOrEmpty(sinceVersion))
            {
                // Return last N releases
                return releases.Take(maxCount).ToList();
            }

            var result = new List<GitHubRelease>();
            foreach (var release in releases)
            {
                if (CompareVersions(release.TagName, sinceVersion) > 0)
                {
                    result.Add(release);
                    if (result.Count >= maxCount) break;
                }
            }

            return result;
        }

        private List<GitHubRelease> GetNewerReleases(List<GitHubRelease> releases, string currentVersion)
        {
            // For update notifications, also include stable releases if we're on prerelease
            var relevantReleases = _isPrerelease 
                ? releases.Where(r => !r.Prerelease || FilterRelevantReleases(new List<GitHubRelease> { r }).Any()).ToList()
                : releases;

            return relevantReleases
                .Where(r => CompareVersions(r.TagName, currentVersion) > 0)
                .ToList();
        }

        private int CompareVersions(string version1, string version2)
        {
            // Remove 'v' prefix if present
            version1 = version1.TrimStart('v');
            version2 = version2.TrimStart('v');

            // Try to parse as Version objects
            if (Version.TryParse(version1.Split('-')[0], out var v1) && 
                Version.TryParse(version2.Split('-')[0], out var v2))
            {
                var result = v1.CompareTo(v2);
                if (result != 0) return result;

                // If base versions are equal, compare prerelease suffixes
                var pre1 = version1.Contains('-') ? version1.Substring(version1.IndexOf('-')) : "";
                var pre2 = version2.Contains('-') ? version2.Substring(version2.IndexOf('-')) : "";

                // No prerelease is greater than prerelease
                if (string.IsNullOrEmpty(pre1) && !string.IsNullOrEmpty(pre2)) return 1;
                if (!string.IsNullOrEmpty(pre1) && string.IsNullOrEmpty(pre2)) return -1;

                return string.Compare(pre1, pre2, StringComparison.Ordinal);
            }

            return string.Compare(version1, version2, StringComparison.Ordinal);
        }

        public void LogUpdateNotification(UpdateCheckResult result)
        {
            if (result.UpdateAvailable && result.NewReleases.Any())
            {
                _logger.LogInformation("\u001b[1;33m╔════════════════════════════════════════════════════════════════╗\u001b[0m");
                _logger.LogInformation("\u001b[1;33m║  UPDATE AVAILABLE: {Version,-45}║\u001b[0m", result.NewReleases.First().TagName);
                _logger.LogInformation("\u001b[1;33m║  View release notes: https://github.com/rnwood/smtp4dev/releases ║\u001b[0m");
                _logger.LogInformation("\u001b[1;33m╚════════════════════════════════════════════════════════════════╝\u001b[0m");
            }
        }

        public void LogWhatsNewNotification(UpdateCheckResult result)
        {
            if (result.ShowWhatsNew)
            {
                var releaseCount = result.NewReleases.Count;
                _logger.LogInformation("\u001b[1;36m╔════════════════════════════════════════════════════════════════╗\u001b[0m");
                _logger.LogInformation("\u001b[1;36m║  WHAT'S NEW: {Count} new release(s) since last use              ║\u001b[0m", releaseCount);
                _logger.LogInformation("\u001b[1;36m║  View release notes in the web UI or at:                       ║\u001b[0m");
                _logger.LogInformation("\u001b[1;36m║  https://github.com/rnwood/smtp4dev/releases                   ║\u001b[0m");
                _logger.LogInformation("\u001b[1;36m╚════════════════════════════════════════════════════════════════╝\u001b[0m");
            }
        }
    }
}
