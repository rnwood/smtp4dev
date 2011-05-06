#region



#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class HeloVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            connection.Session.ClientName = string.Join(" ", command.Arguments);
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Nice to meet you"));
        }
    }
}