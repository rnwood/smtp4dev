using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class EhloVerb : Verb
    {
        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (!string.IsNullOrEmpty(connectionProcessor.Session.ClientName))
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "You already said HELO"));
                return;
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine("Nice to meet you.");

            IEnumerable<string> extnNames = connectionProcessor.ExtensionProcessors.SelectMany(extn => extn.GetEHLOKeywords());
            foreach (string extnName in extnNames)
            {
                text.AppendLine(extnName);
            }

            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, text.ToString().TrimEnd()));
        }
    }
}
