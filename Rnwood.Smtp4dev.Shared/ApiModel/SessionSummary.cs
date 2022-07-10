using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class SessionSummary : ICacheByKey
    {

        public string ClientAddress { get; set; }
        public string ClientName { get; set; }
        public int NumberOfMessages { get; set; }
        public Guid Id { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public bool TerminatedWithError { get; set; }

        public int Size { get; set; }

        string ICacheByKey.CacheKey => Id.ToString();
    }
}
