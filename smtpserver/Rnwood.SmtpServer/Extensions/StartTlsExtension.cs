#region

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
                Connection = connection;
                Connection.VerbMap.SetVerbProcessor("STARTTLS", new StartTlsVerb());
            }

            public IConnection Connection { get; private set; }

            public string[] EHLOKeywords
            {
                get
                {
                    if (!Connection.Session.SecureConnection)
                    {
                        return new[] { "STARTTLS" };
                    }

                    return new string[] { };
                }
            }
        }

        #endregion
    }
}