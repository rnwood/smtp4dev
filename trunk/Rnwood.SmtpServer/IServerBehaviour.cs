using System.Net;
using System.Security.Cryptography.X509Certificates;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer
{
    public interface IServerBehaviour
    {
        void OnMessageReceived(Message message);
        string DomainName { get; }
        IPAddress IpAddress { get; }
        int PortNumber { get; }
        bool RunOverSSL { get; }
        long? GetMaximumMessageSize(IConnectionProcessor processor);
        X509Certificate GetSSLCertificate(IConnectionProcessor processor);
        Extension[] GetExtensions(IConnectionProcessor processor);
        void OnSessionCompleted(Session Session);
        int GetReceiveTimeout(IConnectionProcessor processor);
        AuthenticationResult ValidateAuthenticationRequest(IConnectionProcessor processor, AuthenticationRequest authenticationRequest);
    }
}