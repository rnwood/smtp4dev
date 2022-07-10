namespace Rnwood.Smtp4dev.ApiModel
{
    internal class ServerSession : Session
    {
        public ServerSession(DbModel.Session dbSession)
        {
            this.Id = dbSession.Id;
            this.Error = dbSession.SessionError;
            this.ErrorType = dbSession.SessionErrorType.ToString();
        }
    }
}
