using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Session
    {
        public Session(DbModel.Session dbSession)
        {
            this.Id = dbSession.Id;
            this.Error = dbSession.SessionError;
            this.ErrorType = dbSession.SessionErrorType.ToString();
        }


        public Guid Id { get; private set; }
     
        public string ErrorType { get; private set; }
        public string Error { get; private set; }


    }
}
