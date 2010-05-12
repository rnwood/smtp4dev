using System;
namespace Rnwood.SmtpServer.Verbs
{
    public interface IVerb
    {
        void Process(Rnwood.SmtpServer.IConnection connection, Rnwood.SmtpServer.SmtpCommand command);
    }
}
