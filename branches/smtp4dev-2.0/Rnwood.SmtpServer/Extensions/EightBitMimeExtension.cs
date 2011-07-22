#region

using System;
using System.Text;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class EightBitMimeExtension : IExtension
    {
        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new EightBitMimeExtensionProcessor(connection);
        }

        #region Nested type: EightBitMimeExtensionProcessor

        private class EightBitMimeExtensionProcessor : IExtensionProcessor
        {
            public EightBitMimeExtensionProcessor(IConnection connection)
            {
                EightBitMimeDataVerb verb = new EightBitMimeDataVerb();
                connection.VerbMap.SetVerbProcessor("DATA", verb);

                MailVerb mailVerbProcessor = connection.MailVerb;
                MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
                mailFromProcessor.ParameterProcessorMap.SetProcessor("BODY", verb);
            }

            public string[] EHLOKeywords
            {
                get { return new[] {"8BITMIME"}; }
            }
        }

        #endregion
    }

    public class EightBitMimeDataVerb : DataVerb, IParameterProcessor
    {
        private bool _eightBitMessage;

        #region IParameterProcessor Members

        public void SetParameter(string key, string value)
        {
            if (key.Equals("BODY", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value.Equals("8BITMIME", StringComparison.InvariantCultureIgnoreCase))
                {
                    _eightBitMessage = true;
                }
                else if (value.Equals("7BIT", StringComparison.InvariantCultureIgnoreCase))
                {
                    _eightBitMessage = false;
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
            if (_eightBitMessage)
            {
                connection.SetReaderEncoding(Encoding.Default);
            }

            try
            {
                base.Process(connection, command);
            }
            finally
            {
                if (_eightBitMessage)
                {
                    connection.SetReaderEncodingToDefault();
                }
            }
        }
    }
}