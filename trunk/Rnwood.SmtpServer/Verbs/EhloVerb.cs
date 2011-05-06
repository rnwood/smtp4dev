#region

using System.Linq;
using System.Text;

#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class EhloVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            connection.Session.ClientName = string.Join(" ", command.Arguments);

            StringBuilder text = new StringBuilder();
            text.AppendLine("Nice to meet you.");

            foreach (string extnName in connection.ExtensionProcessors.SelectMany(extn => extn.EHLOKeywords))
            {
                text.AppendLine(extnName);
            }

            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, text.ToString().TrimEnd()));
        }
    }
}