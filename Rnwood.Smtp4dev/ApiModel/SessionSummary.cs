using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class SessionSummary : ICacheById
    {
        public SessionSummary(DbModel.Session dbSession)
        {
            this.ClientAddress = dbSession.ClientAddress;
            this.ClientName = dbSession.ClientName;
            this.NumberOfMessages = dbSession.NumberOfMessages;
            this.Id = dbSession.Id;
            this.EndDate = dbSession.EndDate;
        }

        public string ClientAddress { get; private set; }
        public string ClientName { get; private set; }
        public int NumberOfMessages { get; private set; }
        public Guid Id { get; private set; }
        public DateTime EndDate { get; private set; }
    }
}
