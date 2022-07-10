using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Message : ICacheByKey
    {
      

        public Guid Id { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public DateTime ReceivedDate { get; set; }
        
        public bool SecureConnection { get; set; }

        public string Subject { get; set; }

        public List<MessageEntitySummary> Parts { get; set; } 

        public List<Header> Headers { get; set; }

        public string MimeParseError { get; set; }

        public string RelayError { get; set; }

        string ICacheByKey.CacheKey => Id.ToString();
    }
}
