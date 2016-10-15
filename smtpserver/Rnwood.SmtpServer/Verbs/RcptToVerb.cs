#region

using Rnwood.SmtpServer.Verbs;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class RcptToVerb : IVerb
    {
        public async Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage == null)
            {
                await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "No current message"));
                return;
            }

            if (command.ArgumentsText == "<>" || !command.ArgumentsText.StartsWith("<") ||
                !command.ArgumentsText.EndsWith(">") || command.ArgumentsText.Count(c => c == '<') != command.ArgumentsText.Count(c => c == '>'))
            {
                await connection.WriteResponseAsync(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify to address <address>"));
                return;
            }

            string address = command.ArgumentsText.Remove(0, 1).Remove(command.ArgumentsText.Length - 2);
            connection.Server.Behaviour.OnMessageRecipientAdding(connection, connection.CurrentMessage, address);
            connection.CurrentMessage.To.Add(address);
            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.OK, "Recipient accepted"));
        }
    }
}