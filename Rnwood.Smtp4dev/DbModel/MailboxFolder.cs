using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rnwood.Smtp4dev.DbModel
{
    public class MailboxFolder
    {
        public MailboxFolder()
        {
        }

        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid MailboxId { get; set; }
        public Mailbox Mailbox { get; set; }

        public List<Message> Messages { get; set; } = new List<Message>();

        public const string INBOX = "INBOX";
        public const string SENT = "Sent";
    }
}