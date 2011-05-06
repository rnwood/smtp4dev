namespace Rnwood.SmtpServer.Verbs
{
    public interface IVerbMap
    {
        void SetVerbProcessor(string verb, IVerb verbProcessor);
        IVerb GetVerbProcessor(string verb);
    }
}