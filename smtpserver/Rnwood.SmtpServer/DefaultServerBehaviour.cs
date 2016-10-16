#region

using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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

        public IEditableSession OnCreateNewSession(IConnection connection, IPAddress clientAddress, DateTime startDate)
        {
            return new MemorySession(clientAddress, startDate);
        }

        public virtual Encoding GetDefaultEncoding(IConnection connection)
        {
            return new ASCIISevenBitTruncatingEncoding();
        }

        public virtual void OnMessageReceived(IConnection connection, IMessage message)
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

        public virtual void OnMessageRecipientAdding(IConnection connection, IMessageBuilder message, string recipient)
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

        public virtual void OnSessionStarted(IConnection connection, ISession session)
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

        public int MaximumNumberOfSequentialBadCommands
        {
            get { return 10; }
        }

        public async virtual Task<AuthenticationResult> ValidateAuthenticationCredentialsAsync(IConnection connection,
                                                                          IAuthenticationCredentials request)
        {
            var handlers = AuthenticationCredentialsValidationRequiredAsync;

            if (handlers != null)
            {
                var tasks = handlers.GetInvocationList()
                    .Cast<Func<object, AuthenticationCredentialsValidationEventArgs, Task>>()
                    .Select(h =>
                    {
                        AuthenticationCredentialsValidationEventArgs args = new AuthenticationCredentialsValidationEventArgs(request);
                        return new { Args = args, Task = h(this, args) };
                    });

                await Task.WhenAll(tasks.Select(t => t.Task).ToArray());

                AuthenticationResult? failureResult = tasks.Select(t => t.Args.AuthenticationResult)
                    .Where(r => r != AuthenticationResult.Success)
                    .FirstOrDefault() ;

                return failureResult ?? AuthenticationResult.Success;
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

        public virtual void OnCommandReceived(IConnection connection, SmtpCommand command)
        {
            if (CommandReceived != null)
            {
                CommandReceived(this, new CommandEventArgs(command));
            }
        }

        public virtual IMessageBuilder OnCreateNewMessage(IConnection connection)
        {
            return new MemoryMessage.Builder();
        }

        public virtual void OnMessageCompleted(IConnection connection)
        {
            if (MessageCompleted != null)
            {
                MessageCompleted(this, new ConnectionEventArgs(connection));
            }
        }

        #endregion

        public event EventHandler<CommandEventArgs> CommandReceived;

        public event EventHandler<ConnectionEventArgs> MessageCompleted;

        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<SessionEventArgs> SessionCompleted;

        public event EventHandler<SessionEventArgs> SessionStarted;

        public event Func<object, AuthenticationCredentialsValidationEventArgs, Task> AuthenticationCredentialsValidationRequiredAsync;
    }
}