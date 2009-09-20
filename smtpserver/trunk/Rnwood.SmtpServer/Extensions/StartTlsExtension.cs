#region

using System.Net.Security;
using System.Security.Authentication;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class StartTlsExtension : IExtension
    {
        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new StartTlsExtensionProcessor(connection);
        }

        #region Nested type: StartTlsExtensionProcessor

        private class StartTlsExtensionProcessor : IExtensionProcessor
        {
            public StartTlsExtensionProcessor(IConnection connection)
            {
                connection = connection;
                connection.VerbMap.SetVerbProcessor("STARTTLS", new StartTlsVerb());
            }

            public IConnection connection { get; private set; }

            public string[] GetEHLOKeywords()
            {
                if (!connection.Session.SecureConnection)
                {
                    return new[] {"STARTTLS"};
                }

                return new string[] {};
            }
        }

        #endregion
    }

    public class StartTlsVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                                               "Ready to start TLS"));
            connection.ApplyStreamFilter(stream =>
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