namespace Rnwood.Smtp4dev.Server.Settings
{
    public record ClientOptions
    {
        /// <summary>
        /// Page size for message pagination
        /// </summary>
        public int PageSize { get; set; } = 25;
    }
}