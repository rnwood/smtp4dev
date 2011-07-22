#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;
using System.Linq;

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
            _sslCertificate = sslCertificate;
        }

        #region IServerBehaviour Members

        public virtual void OnMessageReceived(IConnection connection, Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageEventArgs(message));
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

        public bool IsSSLEnabled(IConnection connection)
        {
            return _sslCertificate != null;
        }

        public bool IsSessionLoggingEnabled(IConnection connection)
        {
            return false;
        }

        public virtual long? GetMaximumMessageSize(IConnection connection)
        {
            return null;
        }

        public virtual X509Certificate GetSSLCertificate(IConnection connection)
        {
            return _sslCertificate;
        }

        public void OnMessageRecipientAdding(IConnection connection, Message message, string recipient)
        {
        }

        public virtual IEnumerable<IExtension> GetExtensions(IConnection connection)
        {
            List<IExtension> extensions = new List<IExtension>(new IExtension[] { new EightBitMimeExtension(), new SizeExtension() });

            if (_sslCertificate != null)
            {
                extensions.Add(new StartTlsExtension());
            }

            return extensions;
        }

        public virtual void OnSessionCompleted(IConnection connection, ISession session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionEventArgs(session));
            }
        }

        public void OnSessionStarted(IConnection connection, ISession session)
        {
            if (SessionStarted != null)
            {
                SessionStarted(this, new SessionEventArgs(session));
            }
        }

        public virtual int GetReceiveTimeout(IConnection connection)
        {
            return (int)new TimeSpan(0, 5, 0).TotalMilliseconds;
        }

        public virtual AuthenticationResult ValidateAuthenticationCredentials(IConnection connection,
                                                                          IAuthenticationRequest request)
        {
            if (AuthenticationCredentialsValidationRequired != null)
            {
                AuthenticationCredentialsValidationRequired(this, new AuthenticationCredentialsValidationEventArgs(request));
            }

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

        public IMessage CreateMessage(IConnection connection)
        {
            return new Message(connection.Session);
        }

        public virtual void OnMessageCompleted(IConnection connection)
        {
            if (MessageCompleted != null)
            {
                MessageCompleted(this, new MessageEventArgs(connection.CurrentMessage));
            }
        }

        #endregion

        public event EventHandler<MessageEventArgs> MessageCompleted;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<SessionEventArgs> SessionCompleted;
        public event EventHandler<SessionEventArgs> SessionStarted;
        public event EventHandler<AuthenticationCredentialsValidationEventArgs> AuthenticationCredentialsValidationRequired;
    }

    public class AuthenticationCredentialsValidationEventArgs : EventArgs
    {
        public AuthenticationCredentialsValidationEventArgs(IAuthenticationRequest credentials)
        {
            Credentials = credentials;
        }

        public IAuthenticationRequest Credentials { get; private set; }

        public AuthenticationResult AuthenticationResult { get; set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }
    }

    public class SessionEventArgs : EventArgs
    {
        public SessionEventArgs(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; private set; }
    }
}