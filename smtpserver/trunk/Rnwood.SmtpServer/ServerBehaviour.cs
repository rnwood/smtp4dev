using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace Rnwood.SmtpServer
{
    public abstract class ServerBehaviour
    {
        public abstract void OnMessageReceived(Message message);

        public abstract string DomainName
        { 
            get;
        }

        public abstract long? GetMaximumMessageSize(ConnectionProcessor processor);

        public abstract IPAddress IpAddress { get; }
        public abstract int PortNumber { get; }
        public abstract bool RunOverSSL { get; }

        public abstract X509Certificate GetSSLCertificate(ConnectionProcessor processor);

        public abstract Extension[] GetExtensions(ConnectionProcessor processor);

        public abstract void OnSessionCompleted(Session Session);
        public abstract int GetReceiveTimeout(ConnectionProcessor processor);
    }
}
