using System;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class SessionSummary
    {
        public SessionSummary(DbModel.Session dbSession)
        {
            ClientAddress = dbSession.ClientAddress;
            ClientName = dbSession.ClientName;
            NumberOfMessages = dbSession.NumberOfMessages;
            Id = dbSession.Id;
            EndDate = dbSession.EndDate;
        }

        public string ClientAddress { get; }
        public string ClientName { get; }
        public int NumberOfMessages { get; }
        public Guid Id { get; }
        public DateTime EndDate { get; }
    }
}