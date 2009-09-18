namespace Rnwood.SmtpServer.Verbs
{
    public class NoopVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Sucessfully did nothing"));
        }
    }
}