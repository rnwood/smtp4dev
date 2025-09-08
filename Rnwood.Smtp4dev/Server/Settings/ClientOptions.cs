namespace Rnwood.Smtp4dev.Server.Settings
{
    public record ClientOptions
    {
        /// <summary>
        /// Page size for message pagination
        /// </summary>
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// Whether to automatically view new messages as they arrive
        /// </summary>
        public bool AutoViewNewMessages { get; set; } = false;
    }
}