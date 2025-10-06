namespace Rnwood.Smtp4dev.ApiModel
{
    public class GitHubRelease
    {
        public string TagName { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public bool Prerelease { get; set; }
        public string PublishedAt { get; set; }
        public string HtmlUrl { get; set; }
    }
}
