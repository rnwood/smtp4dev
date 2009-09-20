#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class EhloVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            if (!string.IsNullOrEmpty(connection.Session.ClientName))
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "You already said HELO"));
                return;
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine("Nice to meet you.");

            IEnumerable<string> extnNames =
                connection.ExtensionProcessors.SelectMany(extn => extn.GetEHLOKeywords());
            foreach (string extnName in extnNames)
            {
                text.AppendLine(extnName);
            }

            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, text.ToString().TrimEnd()));
        }
    }
}