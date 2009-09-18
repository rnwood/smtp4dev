namespace Rnwood.SmtpServer.Verbs
{
    public class RsetVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            connectionProcessor.AbortMessage();
        }
    }
}