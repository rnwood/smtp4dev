using System;
using System.ComponentModel.DataAnnotations;

namespace Rnwood.Smtp4dev.DbModel
{
    public class ImapState
    {
        [Key]
        public Guid Id { get; set; }

        public long LastUid { get; set; }
    }
}
