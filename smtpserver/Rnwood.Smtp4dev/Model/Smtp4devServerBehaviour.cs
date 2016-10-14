using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    internal class Smtp4devServerBehaviour : IServerBehaviour
    {
        internal Smtp4devServerBehaviour(Settings settings, Action<ISmtp4devMessage> messageRecievedHandler)
        {
            _settings = settings;
            _messageReceivedHandler = messageRecievedHandler;
        }

        private Action<ISmtp4devMessage> _messageReceivedHandler;
        private Settings _settings;

        public string DomainName
        {
            get
            {
                return "smtp4dev";
            }
        }

        public IPAddress IpAddress
        {
            get
            {
                return IPAddress.Any;
            }
        }

        public int MaximumNumberOfSequentialBadCommands
        {
            get
            {
                return 10;
            }
        }

        public int PortNumber
        {
            get
            {
                return _settings.Port;
            }
        }

        public Encoding GetDefaultEncoding(IConnection connection)
        {
            return new ASCIISevenBitTruncatingEncoding();
        }

        public IEnumerable<IExtension> GetExtensions(IConnection connection)
        {
            return Enumerable.Empty<IExtension>();
        }

        public long? GetMaximumMessageSize(IConnection connection)
        {
            return null;
        }

        public int GetReceiveTimeout(IConnection connection)
        {
            return int.MaxValue;
        }

        public X509Certificate GetSSLCertificate(IConnection connection)
        {
            return null;
        }

        public bool IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism)
        {
            return true;
        }

        public bool IsSessionLoggingEnabled(IConnection connection)
        {
            return true;
        }

        public bool IsSSLEnabled(IConnection connection)
        {
            return false;
        }

        public void OnCommandReceived(IConnection connection, SmtpCommand command)
        {
        }

        public IMessageBuilder OnCreateNewMessage(IConnection connection)
        {
            return new Smtp4devMessage.Builder();
        }

        public IEditableSession OnCreateNewSession(IConnection connection, IPAddress clientAddress, DateTime startDate)
        {
            return new MemorySession(clientAddress, startDate);
        }

        public void OnMessageCompleted(IConnection connection)
        {
        }

        public void OnMessageReceived(IConnection connection, IMessage message)
        {
            _messageReceivedHandler((ISmtp4devMessage)message);
        }

        public void OnMessageRecipientAdding(IConnection connection, IMessageBuilder message, string recipient)
        {
        }

        public void OnMessageStart(IConnection connection, string from)
        {
        }

        public void OnSessionCompleted(IConnection connection, ISession Session)
        {
        }

        public void OnSessionStarted(IConnection connection, ISession session)
        {
        }

        public AuthenticationResult ValidateAuthenticationCredentials(IConnection connection, IAuthenticationCredentials authenticationRequest)
        {
            return AuthenticationResult.Success;
        }
    }
}