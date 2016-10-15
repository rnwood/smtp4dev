using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public interface IAuthMechanismProcessor
    {
        Task<AuthMechanismProcessorStatus> ProcessResponseAsync(string data);

        IAuthenticationCredentials Credentials
        {
            get;
        }
    }
}