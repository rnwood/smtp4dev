using System;

namespace Rnwood.SmtpServer
{
    public class BadBase64Exception : SmtpServerException
    {
        public BadBase64Exception(SmtpResponse smtpResponse) : base(smtpResponse)
        {
        }

        public BadBase64Exception(SmtpResponse smtpResponse, Exception innerException)
            : base(smtpResponse, innerException)
        {
        }
    }
}