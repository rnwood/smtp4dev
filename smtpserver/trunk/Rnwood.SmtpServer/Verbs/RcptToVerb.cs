using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class RcptToVerb : Verb
    {
        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (connectionProcessor.CurrentMessage == null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "No current message"));
                return;
            }
            
            connectionProcessor.CurrentMessage.EnvelopeTo.Add(request.ArgumentsText);
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Recipient accepted"));
        }
    }
}
