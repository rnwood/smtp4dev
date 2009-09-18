#region

using System.Net.Security;
using System.Security.Authentication;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class StartTlsExtension : Extension
    {
        public override ExtensionProcessor CreateExtensionProcessor(IConnectionProcessor processor)
        {
            return new StartTlsExtensionProcessor(processor);
        }

        #region Nested type: StartTlsExtensionProcessor

        private class StartTlsExtensionProcessor : ExtensionProcessor
        {
            public StartTlsExtensionProcessor(IConnectionProcessor processor)
            {
                Processor = processor;
                processor.VerbMap.SetVerbProcessor("STARTTLS", new StartTlsVerb());
            }

            public IConnectionProcessor Processor { get; private set; }

            public override string[] GetEHLOKeywords()
            {
                if (!Processor.Session.SecureConnection)
                {
                    return new[] {"STARTTLS"};
                }

                return new string[] {};
            }
        }

        #endregion
    }

    public class StartTlsVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                                               "Ready to start TLS"));
            connectionProcessor.ApplyStreamFilter(stream =>
                                                      {
                                                          SslStream sslStream = new SslStream(stream);
                                                          sslStream.AuthenticateAsServer(
                                                              connectionProcessor.Server.Behaviour.GetSSLCertificate(
                                                                  connectionProcessor), false,
                                                              SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls,
                                                              false);
                                                          return sslStream;
                                                      });

            connectionProcessor.Session.SecureConnection = true;
        }
    }
}