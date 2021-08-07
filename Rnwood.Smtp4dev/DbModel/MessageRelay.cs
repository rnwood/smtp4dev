using System;
using System.ComponentModel.DataAnnotations;

namespace Rnwood.Smtp4dev.DbModel
{
    public class MessageRelay
    {
        [Key] public Guid Id { get; set; }

        public Guid MessageId { get; set; }

        public virtual Message Message { get; set; }

        public string To { get; set; }
        public DateTime SendDate { get; set; }
    }
}