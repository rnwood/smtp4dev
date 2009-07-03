using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class HeloVerb : Verb
    {
        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (!string.IsNullOrEmpty(connectionProcessor.Session.ClientName))
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "You already said HELO"));
                return;
            }

            connectionProcessor.Session.ClientName = request.Arguments[0];
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Nice to meet you"));
        }


    }
}
