#region

using System.Linq;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class MailFromVerb : IVerb
    {
        public MailFromVerb()
        {
            ParameterProcessorMap = new ParameterProcessorMap();
        }

        public ParameterProcessorMap ParameterProcessorMap { get; private set; }

        public void Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage != null)
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "You already told me who the message was from"));
                return;
            }

            if (command.ArgumentsText.Length == 0)
            {
                connection.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify from address or <>"));
                return;
            }

            string from = command.ArgumentsText.TrimStart('<').TrimEnd('>');
            connection.Server.Behaviour.OnMessageStart(connection, from);
            connection.NewMessage();
            connection.CurrentMessage.From = from;


            try
            {
                ParameterProcessorMap.Process(command.Arguments.Skip(1).ToArray(), true);
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Okey dokey"));
            }
            catch
            {
                connection.AbortMessage();
                throw;
            }
        }
    }
}