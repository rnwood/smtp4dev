using Rnwood.SmtpServer.Verbs;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions
{
    public class StartTlsVerb : IVerb
    {
        public async Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            X509Certificate certificate = connection.Server.Behaviour.GetSSLCertificate(connection);

            if (certificate == null)
            {
                await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.CommandNotImplemented, "TLS configuration error - no certificate"));
                return;
            }

            await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                                      "Ready to start TLS"));

#pragma warning disable 0618
            var sslProtos = SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls;
#pragma warning restore 0618

            await connection.ApplyStreamFilterAsync(async stream =>
                                                     {
                                                         SslStream sslStream = new SslStream(stream);
                                                         await sslStream.AuthenticateAsServerAsync(certificate
                                                             , false,
                                                             sslProtos,
                                                             false);
                                                         return sslStream;
                                                     });

            connection.Session.SecureConnection = true;
        }
    }
}