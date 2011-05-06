using System.Net.Security;
using System.Security.Authentication;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Extensions
{
    public class StartTlsVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                                      "Ready to start TLS"));
            connection.Channel.ApplyStreamFilter(stream =>
                                                     {
                                                         SslStream sslStream = new SslStream(stream);
                                                         sslStream.AuthenticateAsServer(
                                                             connection.Server.Behaviour.GetSSLCertificate(
                                                                 connection), false,
                                                             SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls,
                                                             false);
                                                         return sslStream;
                                                     });

            connection.Session.SecureConnection = true;
        }
    }
}