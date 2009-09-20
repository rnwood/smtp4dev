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

        public virtual void OnMessageReceived(IConnection connection, Message message)
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

        public virtual long? GetMaximumMessageSize(IConnection connection)
        {
            return null;
        }

        public virtual X509Certificate GetSSLCertificate(IConnection connection)
        {
            return _sslCertificate;
        }

        public virtual IExtension[] GetExtensions(IConnection connection)
        {
            return new IExtension[] {new EightBitMimeExtension(), new SizeExtension()};
        }

        public virtual void OnSessionCompleted(IConnection connection, Session session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionEventArgs(session));
            }
        }

        public void OnSessionStarted(IConnection connection, Session session)
        {
            if (SessionStarted != null)
            {
                SessionStarted(this, new SessionEventArgs(session));
            }
        }

        public virtual int GetReceiveTimeout(IConnection connection)
        {
            return (int) new TimeSpan(0, 5, 0).TotalMilliseconds;
        }

        public virtual AuthenticationResult ValidateAuthenticationRequest(IConnection connection,
                                                                          IAuthenticationRequest request)
        {
            return AuthenticationResult.Failure;
        }

        public virtual void OnMessageStart(IConnection connection, string from)
        {
        }

        public virtual bool IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism)
        {
            return false;
        }

        public void OnCommandReceived(IConnection connection, SmtpCommand command)
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