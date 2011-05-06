#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class EightBitMimeExtension : IExtension
    {
        public EightBitMimeExtension()
        {
        }

        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new EightBitMimeExtensionProcessor(connection);
        }

        #region Nested type: EightBitMimeExtensionProcessor

        private class EightBitMimeExtensionProcessor : ExtensionProcessor
        {
            public EightBitMimeExtensionProcessor(IConnection connection)
                : base(connection)
            {
                EightBitMimeDataVerb verb = new EightBitMimeDataVerb();
                connection.VerbMap.SetVerbProcessor("DATA", verb);

                MailVerb mailVerbProcessor = connection.MailVerb;
                MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
                mailFromProcessor.ParameterProcessorMap.SetProcessor("BODY", verb);
            }

            public override string[] EHLOKeywords
            {
                get { return new[] { "8BITMIME" }; }
            }
        }

        #endregion
    }
}