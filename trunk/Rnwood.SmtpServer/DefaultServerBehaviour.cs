#region

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

#endregion

namespace Rnwood.SmtpServer
{
    public class DefaultServerBehaviour : IServerBehaviour
    {
        private readonly X509Certificate _sslCertificate;

        public DefaultServerBehaviour(X509Certificate sslCertificate)
            : this(587, sslCertificate)
        {
        }

        public DefaultServerBehaviour()
            : this(25, null)
        {
        }

        public DefaultServerBehaviour(int portNumber)
            : this(portNumber, null)
        {
        }

        public DefaultServerBehaviour(int portNumber, X509Certificate sslCertificate)
        {
            PortNumber = portNumber;
            RunOverSSL = sslCertificate != null;
            _sslCertificate = sslCertificate;
        }

        #region IServerBehaviour Members

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

        public virtual int PortNumber { get; private set; }

        public virtual bool RunOverSSL { get; private set; }

        public virtual long? GetMaximumMessageSize(IConnectionProcessor processor)
        {
            return null;
        }

        public virtual X509Certificate GetSSLCertificate(IConnectionProcessor processor)
        {
            return _sslCertificate;
        }

        public virtual Extension[] GetExtensions(IConnectionProcessor processor)
        {
            return new Extension[] {new EightBitMimeExtension(), new SizeExtension()};
        }

        public virtual void OnSessionCompleted(Session session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionEventArgs(session));
            }
        }

        public void OnSessionStarted(IConnectionProcessor processor, Session session)
        {
            if (SessionStarted != null)
            {
                SessionStarted(this, new SessionEventArgs(session));
            }
        }

        public virtual int GetReceiveTimeout(IConnectionProcessor processor)
        {
            return (int) new TimeSpan(0, 5, 0).TotalMilliseconds;
        }

        public virtual AuthenticationResult ValidateAuthenticationRequest(IConnectionProcessor processor,
                                                                          AuthenticationRequest request)
        {
            return AuthenticationResult.Failure;
        }

        public virtual void OnMessageStart(IConnectionProcessor processor, string from)
        {
        }

        public virtual bool IsAuthMechanismEnabled(IConnectionProcessor processor, IAuthMechanism authMechanism)
        {
            return false;
        }

        public void OnCommandReceived(IConnectionProcessor processor, SmtpCommand command)
        {
        }

        #endregion

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<SessionEventArgs> SessionCompleted;
        public event EventHandler<SessionEventArgs> SessionStarted;
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }
    }

    public class SessionEventArgs : EventArgs
    {
        public SessionEventArgs(Session session)
        {
            Session = session;
        }

        public Session Session { get; private set; }
    }
}