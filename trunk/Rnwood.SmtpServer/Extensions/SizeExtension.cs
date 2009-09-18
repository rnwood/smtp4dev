#region

using System;
using System.Net.Mail;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class SizeExtension : Extension
    {
        public override ExtensionProcessor CreateExtensionProcessor(IConnectionProcessor processor)
        {
            return new SizeExtensionProcessor(processor);
        }

        #region Nested type: SizeExtensionProcessor

        private class SizeExtensionProcessor : ExtensionProcessor, IParameterProcessor
        {
            public SizeExtensionProcessor(IConnectionProcessor processor)
            {
                Processor = processor;
                processor.MailVerb.FromSubVerb.ParameterProcessorMap.SetProcessor("SIZE", this);
            }

            public IConnectionProcessor Processor { get; private set; }

            #region IParameterProcessor Members

            public void SetParameter(string key, string value)
            {
                if (key.Equals("SIZE", StringComparison.InvariantCultureIgnoreCase))
                {
                    int messageSize;

                    if (int.TryParse(value, out messageSize) && messageSize > 0)
                    {
                        long? maxMessageSize = Processor.Server.Behaviour.GetMaximumMessageSize(Processor);

                        if (maxMessageSize.HasValue && messageSize > maxMessageSize)
                        {
                            throw new SmtpServerException(
                                new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation,
                                                 "Message exceeds fixes size limit"));
                        }
                    }
                    else
                    {
                        throw new SmtpException(SmtpStatusCode.SyntaxError, "Bad message size specified");
                    }
                }
            }

            #endregion

            public override string[] GetEHLOKeywords()
            {
                long? maxMessageSize = Processor.Server.Behaviour.GetMaximumMessageSize(Processor);

                if (maxMessageSize.HasValue)
                {
                    return new[] {string.Format("SIZE={0}", maxMessageSize.Value)};
                }
                else
                {
                    return new[] {"SIZE"};
                }
            }
        }

        #endregion
    }
}