using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions
{
    public class EightBitMimeExtension : Extension
    {
        public override ExtensionProcessor CreateExtensionProcessor(ConnectionProcessor processor)
        {
            return new EightBitMimeExtensionProcessor(processor);
        }

        class EightBitMimeExtensionProcessor : ExtensionProcessor
        {
            public EightBitMimeExtensionProcessor(ConnectionProcessor processor)
            {
                EightBitMimeDataVerb verb = new EightBitMimeDataVerb();
                processor.VerbMap.SetVerbProcessor("DATA", verb);

                MailVerb mailVerbProcessor = processor.MailVerb;
                MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
                mailFromProcessor.ParameterProcessorMap.SetProcessor("BODY", verb);
            }

            public override string[] GetEHLOKeywords()
            {
                return new[] { "8BITMIME" };
            }
        }
    }

    public class EightBitMimeDataVerb : DataVerb, IParameterProcessor
    {
        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (_eightBitMessage)
            {
                connectionProcessor.SwitchReaderEncoding(Encoding.Default);
            }
           
            try
            {
                base.Process(connectionProcessor, request);
            }
            finally
            {
                if (_eightBitMessage)
                {
                    connectionProcessor.SwitchReaderEncodingToDefault();
                }
            }
        }

        private bool _eightBitMessage = false;

        public void SetParameter(string key, string value)
        {
            if (key.Equals("BODY", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value.Equals("8BITMIME", StringComparison.InvariantCultureIgnoreCase))
                {
                    _eightBitMessage = true;
                } else if (value.Equals("7BIT", StringComparison.InvariantCultureIgnoreCase))
                {
                    _eightBitMessage = false;
                } else
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "BODY parameter value invalid - must be either 7BIT or 8BITMIME"));
                }
            }
        }
    }
}
