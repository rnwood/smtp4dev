using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer
{
    public class MailFromVerb : Verb
    {
        public MailFromVerb()
        {
            ParameterProcessorMap = new ParameterProcessorMap();
        }

        public override void Process(IConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (connectionProcessor.CurrentMessage != null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "You already told me who the message was from"));
                return;
            }

            connectionProcessor.NewMessage();

            if (request.ArgumentsText.Length == 0)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "Must specify from address or <>"));
                return;
            }

            if (!request.ArgumentsText.StartsWith("<") || !request.ArgumentsText.EndsWith(">"))
            {
                connectionProcessor.CurrentMessage.From = request.ArgumentsText.TrimStart('<').TrimEnd('>');
            }


            try
            {
                ParameterProcessorMap.Process(request.Arguments.Skip(1).ToArray(), true);
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Okey dokey"));
            }
            catch
            {
                connectionProcessor.AbortMessage();
                throw;
            }
        }

        public ParameterProcessorMap ParameterProcessorMap
        {
            get;
            private set;
        }
    }
}
