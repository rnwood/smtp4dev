#region

using Rnwood.SmtpServer.Verbs;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class EhloVerb : IVerb
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

            StringBuilder text = new StringBuilder();
            text.AppendLine("Nice to meet you.");

            foreach (string extnName in connection.ExtensionProcessors.SelectMany(extn => extn.EHLOKeywords))
            {
                text.AppendLine(extnName);
            }

            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.OK, text.ToString().TrimEnd()));
        }
    }
}