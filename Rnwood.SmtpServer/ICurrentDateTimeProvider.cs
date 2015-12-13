using System;

namespace Rnwood.SmtpServer
{
    public interface ICurrentDateTimeProvider
    {
        DateTime GetCurrentDateTime();
    }
}