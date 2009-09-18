#region

using System;
using System.Text;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class EightBitMimeExtension : Extension
    {
        public override ExtensionProcessor CreateExtensionProcessor(IConnectionProcessor processor)
        {
            return new EightBitMimeExtensionProcessor(processor);
        }

        #region Nested type: EightBitMimeExtensionProcessor

        private class EightBitMimeExtensionProcessor : ExtensionProcessor
        {
            public EightBitMimeExtensionProcessor(IConnectionProcessor processor)
            {
                EightBitMimeDataVerb verb = new EightBitMimeDataVerb();
                processor.VerbMap.SetVerbProcessor("DATA", verb);

                MailVerb mailVerbProcessor = processor.MailVerb;
                MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
                mailFromProcessor.ParameterProcessorMap.SetProcessor("BODY", verb);
            }

            public override string[] GetEHLOKeywords()
            {
                return new[] {"8BITMIME"};
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

        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            if (_eightBitMessage)
            {
                connectionProcessor.SwitchReaderEncoding(Encoding.Default);
            }

            try
            {
                base.Process(connectionProcessor, command);
            }
            finally
            {
                if (_eightBitMessage)
                {
                    connectionProcessor.SwitchReaderEncodingToDefault();
                }
            }
        }
    }
}