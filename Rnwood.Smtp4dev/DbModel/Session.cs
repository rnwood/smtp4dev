using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.DbModel
{
    public class Session
    {
        public Session()
        {

        }

        [Key]
        public Guid Id { get; set; }

        public string Log { get; set; }
        public string ClientAddress { get; internal set; }
        public string ClientName { get; internal set; }
        public DateTime? EndDate { get; internal set; }

        public DateTime StartDate { get; internal set; }

        public int NumberOfMessages { get; internal set; }
    }
}
