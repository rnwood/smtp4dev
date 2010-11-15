using System;

namespace Rnwood.SmtpServer
{
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(SmtpCommand command)
        {
            Command = command;
        }

        public SmtpCommand Command { get; private set; }
    }
}