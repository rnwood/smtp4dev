using System;
using System.ComponentModel.DataAnnotations;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev.DbModel
{
    public class Folder
    {
        public Folder()
        {

        }

        [Key]
        public Guid Id { get; set; }
        
        public Mailbox Mailbox { get; set; }

        public string Name { get; set; }
        
        public string Path { get; set; }
    }
}
