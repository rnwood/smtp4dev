#region

using System;

#endregion

namespace Rnwood.SmtpServer
{
    public class SmtpServerException : Exception
    {
        public SmtpServerException(SmtpResponse smtpResponse) : base(smtpResponse.Message)
        {
            SmtpResponse = smtpResponse;
        }

        public SmtpServerException(SmtpResponse smtpResponse, Exception innerException)
            : base(smtpResponse.Message, innerException)
        {
            SmtpResponse = smtpResponse;
        }

        public SmtpResponse SmtpResponse { get; private set; }
    }
}