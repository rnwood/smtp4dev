#region

using System;
using System.Text;

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

    public class EightBitMimeDataVerb : DataVerb, IParameterProcessor
    {
        public EightBitMimeDataVerb()
        {
        }

        #region IParameterProcessor Members

        public void SetParameter(IConnection connection, string key, string value)
        {
            if (key.Equals("BODY", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value.Equals("8BITMIME", StringComparison.InvariantCultureIgnoreCase))
                {
                    connection.CurrentMessage.EightBitTransport = true;
                }
                else if (value.Equals("7BIT", StringComparison.InvariantCultureIgnoreCase))
                {
                    connection.CurrentMessage.EightBitTransport = false;
                }
                else
                {
                    throw new SmtpServerException(
                        new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                         "BODY parameter value invalid - must be either 7BIT or 8BITMIME"));
                }
            }
        }

        #endregion

        public override void Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage != null && connection.CurrentMessage.EightBitTransport)
            {
                connection.SetReaderEncoding(Encoding.Default);
            }

            try
            {
                base.Process(connection, command);
            }
            finally
            {
                connection.SetReaderEncodingToDefault();
            }
        }
    }
}