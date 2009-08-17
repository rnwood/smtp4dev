using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer.Verbs;

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

            if (request.ArgumentsText.Length < 3 || !request.ArgumentsText.StartsWith("<") || !request.ArgumentsText.EndsWith(">"))
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "Must specify to address <address>"));
                return;
            }

            connectionProcessor.CurrentMessage.ToList.Add(request.ArgumentsText.TrimStart('<').TrimEnd('>'));
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Recipient accepted"));
        }
    }
}
