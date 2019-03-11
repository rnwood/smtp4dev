using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class SessionSummary : ICacheByKey
    {
        public SessionSummary(DbModel.Session dbSession)
        {
            this.ClientAddress = dbSession.ClientAddress;
            this.ClientName = dbSession.ClientName;
            this.NumberOfMessages = dbSession.NumberOfMessages;
            this.Id = dbSession.Id;
            this.EndDate = dbSession.EndDate;
            this.StartDate = dbSession.StartDate;
            this.TerminatedWithError = dbSession.SessionError != null;
            this.Size = dbSession.Log?.Length ?? 0;
        }

        public string ClientAddress { get; private set; }
        public string ClientName { get; private set; }
        public int NumberOfMessages { get; private set; }
        public Guid Id { get; private set; }
        public DateTime? EndDate { get; private set; }
        public DateTime StartDate { get; private set; }
        public bool TerminatedWithError { get; private set; }

        public int Size { get; private set; }

        string ICacheByKey.CacheKey => Id.ToString();
    }
}
