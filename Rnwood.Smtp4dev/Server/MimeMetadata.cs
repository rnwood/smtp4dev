using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.Server
{
    /// <summary>
    /// Represents MIME metadata extracted from a message for storage in the database
    /// </summary>
    public class MimeMetadata
    {
        /// <summary>
        /// CC recipients extracted from MIME headers
        /// </summary>
        [JsonPropertyName("cc")]
        public List<string> CcRecipients { get; set; } = new List<string>();

        /// <summary>
        /// Attachment filenames from Content-Disposition and Content-Type headers
        /// </summary>
        [JsonPropertyName("attachmentFilenames")]
        public List<string> AttachmentFilenames { get; set; } = new List<string>();

        /// <summary>
        /// Whether the message has HTML body content
        /// </summary>
        [JsonPropertyName("hasHtmlBody")]
        public bool HasHtmlBody { get; set; }

        /// <summary>
        /// Whether the message has plain text body content
        /// </summary>
        [JsonPropertyName("hasTextBody")]
        public bool HasTextBody { get; set; }

        /// <summary>
        /// Content-Type of the main message
        /// </summary>
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Total number of MIME parts in the message
        /// </summary>
        [JsonPropertyName("partCount")]
        public int PartCount { get; set; }

        [JsonPropertyName("hasDuplicatedContentIds")]
        public bool? HasDuplicatedContentIds { get; set; }
    }
}