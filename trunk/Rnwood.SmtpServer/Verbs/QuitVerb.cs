using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer
{
    public class QuitVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel, "See you later aligator" ));
            connectionProcessor.CloseConnection();
            connectionProcessor.Session.SessionCompleted = true;
        }
    }
}
