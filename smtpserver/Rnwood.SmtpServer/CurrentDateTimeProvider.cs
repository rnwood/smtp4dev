using System;

namespace Rnwood.SmtpServer
{
    public class CurrentDateTimeProvider : ICurrentDateTimeProvider
    {
        public DateTime GetCurrentDateTime()
        {
            return DateTime.Now;
        }
    }
}