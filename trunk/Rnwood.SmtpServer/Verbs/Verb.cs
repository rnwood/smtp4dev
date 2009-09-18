namespace Rnwood.SmtpServer.Verbs
{
    public abstract class Verb
    {
        public abstract void Process(IConnectionProcessor connectionProcessor, SmtpCommand command);
    }
}