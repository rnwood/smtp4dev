#region

using System.Linq;

#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class RcptToVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage == null)
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "No current message"));
                return;
            }

            if (command.ArgumentsText == "<>" || !command.ArgumentsText.StartsWith("<") ||
                !command.ArgumentsText.EndsWith(">") || command.ArgumentsText.Count(c => c == '<') != command.ArgumentsText.Count(c => c == '>'))
            {
                connection.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify to address <address>"));
                return;
            }

            string address = command.ArgumentsText.Remove(0, 1).Remove(command.ArgumentsText.Length - 2);
            connection.Server.Behaviour.OnMessageRecipientAdding(connection, connection.CurrentMessage, address);
            connection.CurrentMessage.AddTo(address);
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Recipient accepted"));
        }
    }
}