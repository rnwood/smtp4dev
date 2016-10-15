using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Verbs
{
    public class RsetVerb : IVerb
    {
        public async Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            connection.AbortMessage();
            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.OK, "Rset completed"));
        }
    }
}