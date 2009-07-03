using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class MailFromVerb : Verb
    {
        public MailFromVerb()
        {
            ParameterProcessorMap = new ParameterProcessorMap();
        }

        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (connectionProcessor.CurrentMessage != null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "You already told me who the message was from"));
                return;
            }

            connectionProcessor.NewMessage();

            try
            {
                connectionProcessor.CurrentMessage.EnvelopeFrom = request.Arguments[0];

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
