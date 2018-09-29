using System;
using System.Collections.Generic;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MessageEntitySummary
    {
        public List<Header> Headers { get; set; }
        public List<MessageEntitySummary> ChildParts { get; set; }
        public string Name { get; set; }
        public Guid MessageId { get; set; }
        public string ContentId { get; set; }
        public List<AttachmentSummary> Attachments { get; set; }
    }
}