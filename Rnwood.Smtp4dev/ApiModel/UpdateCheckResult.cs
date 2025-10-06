using System.Collections.Generic;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public List<GitHubRelease> NewReleases { get; set; } = new List<GitHubRelease>();
        public string CurrentVersion { get; set; }
        public bool ShowWhatsNew { get; set; }
        public string LastSeenVersion { get; set; }
    }
}
