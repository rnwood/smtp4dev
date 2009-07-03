using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Rnwood.SmtpServer
{
    public abstract class ServerBehaviour
    {
        public abstract void OnMessageReceived(Message message);

        public abstract string DomainName
        { 
            get;
        }

        public abstract IPAddress IpAddress { get; }
        public abstract int PortNumber { get; }
        public abstract bool RunOverSSL { get; }
    }
}
