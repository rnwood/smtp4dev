using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rnwood.Smtp4dev.DbModel
{
    public class Message
    {
        [Key] public Guid Id { get; set; }

        public long ImapUid { get; internal set; }

        public string From { get; set; }
        public string To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public byte[] Data { get; set; }

        public string MimeParseError { get; set; }
        
        public string SessionEncoding { get; set; }

        public Session Session { get; set; }
        public int AttachmentCount { get; set; }

        public bool IsUnread { get; set; }
        public string RelayError { get; internal set; }

        public bool SecureConnection { get; set; }
        
        public bool? EightBitTransport { get; set; }

        public virtual List<MessageRelay> Relays { get; set; } = new List<MessageRelay>();


        public void AddRelay(MessageRelay messageRelay)
        {
            messageRelay.Message = this;
            this.Relays.Add(messageRelay);
        }
    }
}