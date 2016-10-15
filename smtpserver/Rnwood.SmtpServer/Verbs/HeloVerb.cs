#region

using Rnwood.SmtpServer.Verbs;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class HeloVerb : IVerb
    {
        public async Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            if (!string.IsNullOrEmpty(connection.Session.ClientName))
            {
                await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "You already said HELO"));
                return;
            }

            connection.Session.ClientName = command.ArgumentsText ?? "";
            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.OK, "Nice to meet you"));
        }
    }
}