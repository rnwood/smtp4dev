using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Verbs
{
    public interface IVerb
    {
        Task ProcessAsync(Rnwood.SmtpServer.IConnection connection, Rnwood.SmtpServer.SmtpCommand command);
    }
}