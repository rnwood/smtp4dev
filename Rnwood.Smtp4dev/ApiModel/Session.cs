using System;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Session
    {
        public Session(DbModel.Session dbSession)
        {
            Id = dbSession.Id;
            Log = dbSession.Log;
        }


        public Guid Id { get; }
        public string Log { get; }
    }
}