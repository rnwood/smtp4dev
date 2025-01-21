using System;

namespace Rnwood.SmtpServer
{
    public interface ICloseNotifyingStream
    {
        event EventHandler Closing;
    }
}
