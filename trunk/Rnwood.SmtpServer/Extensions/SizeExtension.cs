using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Rnwood.SmtpServer.Extensions
{
    public class SizeExtension : Extension
    {
        public SizeExtension()
        {
        }

        public override void ServerStartup(Server server)
        {
        }

        public override ExtensionProcessor CreateExtensionProcessor(ConnectionProcessor processor)
        {
            return new SizeExtensionProcessor(processor);
        }

        class SizeExtensionProcessor : ExtensionProcessor, IParameterProcessor
        {
            public SizeExtensionProcessor(ConnectionProcessor processor)
            {
                Processor = processor;
                processor.MailVerb.FromSubVerb.ParameterProcessorMap.SetProcessor("SIZE", this);
            }

            public ConnectionProcessor Processor { get; private set; }

            public override string[] GetEHLOKeywords()
            {
                if (Processor.Server.MaxMessageSize.HasValue)
                {
                    return new[] { string.Format("SIZE={0}", Processor.Server.MaxMessageSize.Value) };
                }
                else
                {
                    return new[] { "SIZE" };
                }
            }

            public void SetParameter(string key, string value)
            {
                if (key.Equals("SIZE", StringComparison.InvariantCultureIgnoreCase))
                {
                    int messageSize;

                    if (int.TryParse(value, out messageSize) && messageSize > 0)
                    {
                        if (Processor.Server.MaxMessageSize.HasValue && messageSize > Processor.Server.MaxMessageSize)
                        {
                            throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Message exceeds fixes size limit"));
                        }
                    }
                    else
                    {
                        throw new SmtpException(SmtpStatusCode.SyntaxError, "Bad message size specified");
                    }
                }
            }
        }
    }
}
