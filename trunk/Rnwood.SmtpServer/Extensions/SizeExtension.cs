#region

using System;
using System.Net.Mail;

#endregion

namespace Rnwood.SmtpServer.Extensions
{
    public class SizeExtension : IExtension
    {
        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new SizeExtensionProcessor(connection);
        }

        #region Nested type: SizeExtensionProcessor

        private class SizeExtensionProcessor : IExtensionProcessor, IParameterProcessor
        {
            public SizeExtensionProcessor(IConnection connection)
            {
                Connection = connection;
                Connection.MailVerb.FromSubVerb.ParameterProcessorMap.SetProcessor("SIZE", this);
            }

            public IConnection Connection { get; private set; }

            #region IParameterProcessor Members

            public void SetParameter(string key, string value)
            {
                if (key.Equals("SIZE", StringComparison.InvariantCultureIgnoreCase))
                {
                    int messageSize;

                    if (int.TryParse(value, out messageSize) && messageSize > 0)
                    {
                        long? maxMessageSize = Connection.Server.Behaviour.GetMaximumMessageSize(Connection);

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

            public string[] EHLOKeywords
            {
                get
                {
                    long? maxMessageSize = Connection.Server.Behaviour.GetMaximumMessageSize(Connection);

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
        }

        #endregion
    }
}