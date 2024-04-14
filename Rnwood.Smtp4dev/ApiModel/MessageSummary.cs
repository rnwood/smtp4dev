using System;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MessageSummary : ICacheByKey
    {
        public MessageSummary(DbModel.Message dbMessage)
        {
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To.Split(',');
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;
            AttachmentCount = dbMessage.AttachmentCount;
            IsUnread = dbMessage.IsUnread;
            IsRelayed = dbMessage.Relays.Count > 0;
        }

        public bool IsRelayed { get; set; }

        public Guid Id { get; set; }

        public string From { get; set; }
        public string[] To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public int AttachmentCount { get; set; }

        public bool IsUnread { get; set; }


        [JsonIgnore]
        string ICacheByKey.CacheKey => Id.ToString() + IsUnread + IsRelayed + "v2";
    }
}
