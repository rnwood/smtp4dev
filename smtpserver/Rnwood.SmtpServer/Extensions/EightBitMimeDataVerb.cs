using System;
using System.Text;

namespace Rnwood.SmtpServer.Extensions
{
    public class EightBitMimeDataVerb : DataVerb, IParameterProcessor
    {
        public EightBitMimeDataVerb()
        {
        }

        #region IParameterProcessor Members

        public void SetParameter(IConnection connection, string key, string value)
        {
            if (key.Equals("BODY", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Equals("8BITMIME", StringComparison.CurrentCultureIgnoreCase))
                {
                    connection.CurrentMessage.EightBitTransport = true;
                }
                else if (value.Equals("7BIT", StringComparison.OrdinalIgnoreCase))
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

        #endregion IParameterProcessor Members

        public override void Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage != null && connection.CurrentMessage.EightBitTransport)
            {
                connection.SetReaderEncoding(Encoding.UTF8);
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