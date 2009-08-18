using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer
{
    public class HeloVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpRequest request)
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
