using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.DbModel
{
    public class ImapState
    {
        [Key]
        public Guid Id { get; set; }

        public long LastUid { get; set; }
    }
}
