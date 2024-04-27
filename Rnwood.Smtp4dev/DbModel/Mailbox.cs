using System;
using System.ComponentModel.DataAnnotations;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev.DbModel
{
    public class Mailbox
    {
        public Mailbox()
        {

        }

        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; }
    }
}
