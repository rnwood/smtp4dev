using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Objects.DataClasses;

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

        public Mailbox Mailbox { get; set; }
        public Guid? MailboxFolderId { get; set; }
        public MailboxFolder MailboxFolder { get; set; }
        public int AttachmentCount { get; set; }

        public bool IsUnread { get; set; }
        public string RelayError { get; internal set; }

        public bool SecureConnection { get; set; }
        
        public bool? EightBitTransport { get; set; }
        
        public bool HasBareLineFeed { get; set; }

        /// <summary>
        /// JSON metadata extracted from MIME message including CC recipients, attachment filenames, etc.
        /// </summary>
        public string MimeMetadata { get; set; }
        
        /// <summary>
        /// Plain text body content extracted from MIME message for searching
        /// </summary>
        public string BodyText { get; set; }

        public virtual List<MessageRelay> Relays { get; set; } = new List<MessageRelay>();
        public string DeliveredTo { get; set; }

        public void AddRelay(MessageRelay messageRelay)
        {
            messageRelay.Message = this;
            this.Relays.Add(messageRelay);
        }
    }
}