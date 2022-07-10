namespace Rnwood.Smtp4dev.ApiModel
{
    internal class ServerSessionSummary : SessionSummary
    { 
        public ServerSessionSummary(DbModel.Session dbSession)
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
    }
}
