using Rnwood.Smtp4dev.Migrations;
using Rnwood.Smtp4dev.Server;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MessageSummary : ICacheByKey
    {
        public MessageSummary(DbModel.Message dbMessage)
        {
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To?.Split(',').Select(email => email.Trim()).ToArray() ?? Array.Empty<string>();
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;
            AttachmentCount = dbMessage.AttachmentCount;
            IsUnread = dbMessage.IsUnread;
            IsRelayed = dbMessage.Relays.Count > 0;
            DeliveredTo = dbMessage.DeliveredTo ?? "";
            HasWarnings = dbMessage.HasBareLineFeed;
        }
        
        public MessageSummary(DbModel.Projections.MessageSummaryProjection messagesSummaryProjection)
        {
            Id = messagesSummaryProjection.Id;
            From = messagesSummaryProjection.From;
            To = messagesSummaryProjection.To?.Split(',').Select(email => email.Trim()).ToArray() ?? Array.Empty<string>();
            ReceivedDate = messagesSummaryProjection.ReceivedDate;
            Subject = messagesSummaryProjection.Subject;
            AttachmentCount = messagesSummaryProjection.AttachmentCount;
            IsUnread = messagesSummaryProjection.IsUnread;
            IsRelayed = messagesSummaryProjection.IsRelayed;
            DeliveredTo = messagesSummaryProjection.DeliveredTo ?? "";

            MimeMetadata mimeMetadata = !string.IsNullOrEmpty(messagesSummaryProjection.MimeMetadata) ?
                JsonSerializer.Deserialize<MimeMetadata>(messagesSummaryProjection.MimeMetadata)
                : null;

            HasWarnings = messagesSummaryProjection.HasBareLineFeed
            || (mimeMetadata?.HasDuplicatedContentIds.GetValueOrDefault() ?? false);
        }

        public bool IsRelayed { get; set; }
        public string DeliveredTo { get; set; }
        public Guid Id { get; set; }

        public string From { get; set; }
        public string[] To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public int AttachmentCount { get; set; }

        public bool IsUnread { get; set; }

        public bool HasWarnings { get; set; }

        [JsonIgnore]
        string ICacheByKey.CacheKey => Id.ToString() + IsUnread + IsRelayed + HasWarnings + "v5";
    }
}
