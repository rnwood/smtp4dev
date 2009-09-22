#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
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

            if (command.ArgumentsText.Length < 3 || !command.ArgumentsText.StartsWith("<") ||
                !command.ArgumentsText.EndsWith(">"))
            {
                connection.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify to address <address>"));
                return;
            }

            string address = command.ArgumentsText.TrimStart('<').TrimEnd('>');
            connection.Server.Behaviour.OnMessageRecipientAdding(connection, connection.CurrentMessage, address);
            connection.CurrentMessage.ToList.Add(address);
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Recipient accepted"));
        }
    }
}