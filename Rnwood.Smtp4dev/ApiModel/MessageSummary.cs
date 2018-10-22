using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MessageSummary : ICacheById
    {
        public MessageSummary(DbModel.Message dbMessage)
        {
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To;
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;
            AttachmentCount = dbMessage.AttachmentCount;
        }

        public Guid Id { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public int AttachmentCount { get; set; }
        
    }
}
