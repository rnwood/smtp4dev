#region

using System.Linq;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class MailFromVerb : Verb
    {
        public MailFromVerb()
        {
            ParameterProcessorMap = new ParameterProcessorMap();
        }

        public ParameterProcessorMap ParameterProcessorMap { get; private set; }

        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            if (connectionProcessor.CurrentMessage != null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "You already told me who the message was from"));
                return;
            }

            if (command.ArgumentsText.Length == 0)
            {
                connectionProcessor.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify from address or <>"));
                return;
            }

            string from = command.ArgumentsText.TrimStart('<').TrimEnd('>');
            connectionProcessor.Server.Behaviour.OnMessageStart(connectionProcessor, from);
            connectionProcessor.NewMessage();
            connectionProcessor.CurrentMessage.From = from;


            try
            {
                ParameterProcessorMap.Process(command.Arguments.Skip(1).ToArray(), true);
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Okey dokey"));
            }
            catch
            {
                connectionProcessor.AbortMessage();
                throw;
            }
        }
    }
}