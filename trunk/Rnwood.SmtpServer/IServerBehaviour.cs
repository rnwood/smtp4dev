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
        long? GetMaximumMessageSize(ConnectionProcessor processor);
        X509Certificate GetSSLCertificate(ConnectionProcessor processor);
        Extension[] GetExtensions(ConnectionProcessor processor);
        void OnSessionCompleted(Session Session);
        int GetReceiveTimeout(ConnectionProcessor processor);
        AuthenticationResult ValidateAuthenticationRequest(ConnectionProcessor processor, AuthenticationRequest authenticationRequest);
    }
}