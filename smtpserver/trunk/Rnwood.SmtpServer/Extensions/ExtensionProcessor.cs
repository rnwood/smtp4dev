using System;

namespace Rnwood.SmtpServer.Extensions
{
    public abstract class ExtensionProcessor : IExtensionProcessor
    {
        public ExtensionProcessor(IConnection connection)
        {
            Connection = connection;
        }

        public IConnection Connection { get; private set; }

        public abstract string[] EHLOKeywords
        { get;
        }
    }
}
