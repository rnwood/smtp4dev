namespace Rnwood.SmtpServer.Verbs
{
    public class NoopVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Sucessfully did nothing"));
        }
    }
}