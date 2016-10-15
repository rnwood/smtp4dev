#region

using Rnwood.SmtpServer.Verbs;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class QuitVerb : IVerb
    {
        public async Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel,
                                                               "See you later aligator"));
            connection.CloseConnection();
            connection.Session.CompletedNormally = true;
        }
    }
}