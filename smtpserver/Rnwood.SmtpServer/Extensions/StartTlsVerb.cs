using Rnwood.SmtpServer.Verbs;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Rnwood.SmtpServer.Extensions
{
    public class StartTlsVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            X509Certificate certificate = connection.Server.Behaviour.GetSSLCertificate(connection);

            if (certificate == null)
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.CommandNotImplemented, "TLS configuration error - no certificate"));
                return;
            }

            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                                      "Ready to start TLS"));
            connection.ApplyStreamFilter(stream =>
                                                     {
                                                         SslStream sslStream = new SslStream(stream);
                                                         sslStream.AuthenticateAsServerAsync(certificate
                                                             , false,
                                                             SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls,
                                                             false).Wait();
                                                         return sslStream;
                                                     });

            connection.Session.SecureConnection = true;
        }
    }
}