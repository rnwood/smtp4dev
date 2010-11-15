using System;

namespace Rnwood.SmtpServer
{
    public class SessionEventArgs : EventArgs
    {
        public SessionEventArgs(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; private set; }
    }
}