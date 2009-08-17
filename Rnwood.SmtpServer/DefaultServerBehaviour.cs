using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer
{
    public class DefaultServerBehaviour : IServerBehaviour
    {
        public DefaultServerBehaviour() : this(25)
        {
        }

        public DefaultServerBehaviour(int portNumber)
        {
            _portNumber = portNumber;
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public virtual void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }

        public virtual string DomainName
        {
            get { return Environment.MachineName; }
        }

        public virtual IPAddress IpAddress
        {
            get { return IPAddress.Any; }
        }

        public virtual int PortNumber
        {
            get { return _portNumber; }
        }

        private int _portNumber;

        public virtual bool RunOverSSL
        {
            get { return false; }
        }

        public virtual long? GetMaximumMessageSize(ConnectionProcessor processor)
        {
            return null;
        }

        public virtual X509Certificate GetSSLCertificate(ConnectionProcessor processor)
        {
            return null;
        }

        public virtual Extension[] GetExtensions(ConnectionProcessor processor)
        {
            return new Extension[] { new EightBitMimeExtension(), new SizeExtension() };
        }

        public event EventHandler<SessionCompletedEventArgs> SessionCompleted;

        public virtual void OnSessionCompleted(Session session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionCompletedEventArgs(session));
            }
        }

        public virtual int GetReceiveTimeout(ConnectionProcessor processor)
        {
            return (int) new TimeSpan(0, 5, 0).TotalMilliseconds;
        }

        public virtual AuthenticationResult ValidateAuthenticationRequest(ConnectionProcessor processor, AuthenticationRequest request)
        {
            return AuthenticationResult.Failure;
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }

    }

    public class SessionCompletedEventArgs : EventArgs
    {
        public SessionCompletedEventArgs(Session session)
        {
            Session = session;
        }

        public Session Session { get; private set; }

    }
}
