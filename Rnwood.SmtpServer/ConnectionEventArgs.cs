using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(IConnection connection)
        {
            Connection = connection;
        }

        public IConnection Connection { get; private set; }
    }
}