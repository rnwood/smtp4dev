namespace Rnwood.SmtpServer.Verbs
{
    public class RsetVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            connection.AbortMessage();
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Rset completed"));
        }
    }
}