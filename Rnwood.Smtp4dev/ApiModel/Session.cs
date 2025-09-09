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
            this.ErrorType = dbSession.SessionErrorType?.ToString();
            this.StartDate = dbSession.StartDate;
            if (dbSession.HasBareLineFeed)
            {
                this.Warnings.Add(new SessionWarning { Details = "Session contains bare line feeds (LF without CR). RFC 5321 requires CRLF line endings." });
            }
        }


        public Guid Id { get; private set; }

        public string ErrorType { get; private set; }
        public DateTime StartDate { get; }
        public string Error { get; private set; }
        
        public List<SessionWarning> Warnings { get; set; } = new List<SessionWarning>();


    }
}
