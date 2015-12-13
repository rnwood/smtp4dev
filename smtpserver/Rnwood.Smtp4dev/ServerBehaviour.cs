#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Rnwood.Smtp4dev.Properties;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;
using Settings = System.Object;

#endregion

namespace Rnwood.Smtp4dev
{
    class ServerBehaviour : IServerBehaviour
    {
        private readonly AuthExtension _authExtension = new AuthExtension();
        private readonly EightBitMimeExtension _eightBitMimeExtension = new EightBitMimeExtension();
        private readonly SizeExtension _sizeExtension = new SizeExtension();
        private readonly StartTlsExtension _startTLSExtension = new StartTlsExtension();
        private readonly IServerSettings _settings;

        public ServerBehaviour(IServerSettings settings)
        {
            _settings = settings;
        }

        #region IServerBehaviour Members

        public IEditableSession OnCreateNewSession(IConnection connection, IPAddress clientAddress, DateTime startDate)
        {
            DirectoryInfo messageFolder = new DirectoryInfo(_settings.CustomMessageFolder);

            if (!messageFolder.Exists)
            {
                messageFolder.Create();
            }

            FileInfo filename = null;

            while (filename == null || filename.Exists)
            {
                filename = new FileInfo(Path.Combine(_settings.CustomMessageFolder, "Session" +
                             DateTime.Now.ToString("yyyy-MM-dd_HHmmss-ffff") + ".txt"));
            }

            return new FileSession(clientAddress, startDate, filename, false);
        }

        public Encoding GetDefaultEncoding(IConnection connection)
        {
            if (_settings.DefaultTo8Bit)
            {
                return Encoding.Default;
            }

            return new ASCIISevenBitTruncatingEncoding();
        }

        public void OnMessageCompleted(IConnection connection)
        {
            if (_settings.RejectMessages)
            {
                throw new SmtpServerException(
                    new SmtpResponse(StandardSmtpResponseCode.TransactionFailed,
                                     "Message rejected - transaction failed"));
            }
        }

        public void OnMessageReceived(IConnection connection, IMessage message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageEventArgs(message));
            }
        }

        public void OnMessageRecipientAdding(IConnection connection, IMessage message, string recipient)
        {
            if (_settings.RejectRecipients)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.RecipientRejected,
                                                               "Recipient rejected - mailbox unavailable"));
            }
        }

        public void OnSessionStarted(IConnection connection, ISession session)
        {
            if (SessionStarted != null)
            {
                SessionStarted(this, new SessionEventArgs(session));
            }
        }

        public void OnCommandReceived(IConnection connection, SmtpCommand command)
        {
            if (_settings.CauseTimeout)
            {
                connection.ReadLine();
            }
        }

        public string DomainName
        {
            get { return _settings.DomainName; }
        }

        public IPAddress IpAddress
        {
            get { return IPAddress.Parse(_settings.IPAddress); }
        }

        public int PortNumber
        {
            get { return _settings.PortNumber; }
        }

        public bool IsSSLEnabled(IConnection connection)
        {
            return _settings.EnableSSL;
        }


        public int MaximumNumberOfSequentialBadCommands
        {
            get { return 10; }
        }

        public bool IsSessionLoggingEnabled(IConnection connection)
        {
            return true;
        }

        public X509Certificate GetSSLCertificate(IConnection connection)
        {
            if (string.IsNullOrEmpty(_settings.SSLCertificatePath))
            {
                return null;
            }

            if (string.IsNullOrEmpty(_settings.SSLCertificatePassword))
            {
                return new X509Certificate(_settings.SSLCertificatePath);
            }

            return new X509Certificate(_settings.SSLCertificatePath, _settings.SSLCertificatePassword);
        }

        public IEnumerable<IExtension> GetExtensions(IConnection connection)
        {
            List<IExtension> extensions = new List<IExtension>();

            if (_settings.Enable8BITMIME)
            {
                extensions.Add(_eightBitMimeExtension);
            }

            if (_settings.EnableSTARTTLS)
            {
                extensions.Add(_startTLSExtension);
            }

            if (_settings.EnableAUTH)
            {
                extensions.Add(_authExtension);
            }

            if (_settings.EnableSIZE)
            {
                extensions.Add(_sizeExtension);
            }

            return extensions;
        }

        public long? GetMaximumMessageSize(IConnection connection)
        {
            long value = _settings.MaximumMessageSize;
            return value != 0 ? value : (long?)null;
        }

        public void OnSessionCompleted(IConnection connection, ISession Session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionEventArgs(Session));
            }
        }

        public int GetReceiveTimeout(IConnection connection)
        {
            return _settings.ReceiveTimeout;
        }

        public AuthenticationResult ValidateAuthenticationCredentials(IConnection connection,
                                                                      IAuthenticationCredentials authenticationRequest)
        {
            if (_settings.FailAuthentication)
            {
                return AuthenticationResult.Failure;
            }

            return AuthenticationResult.Success;
        }

        public void OnMessageStart(IConnection connection, string from)
        {
            if (_settings.RequireAuthentication && !connection.Session.Authenticated)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationRequired,
                                                               "Must authenticate before sending mail"));
            }

            if (_settings.RequireSecureConnection && !connection.Session.SecureConnection)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                               "Mail must be sent over secure connection"));
            }
        }

        public bool IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism)
        {
            if (_settings.OnlyAllowClearTextAuthOverSecureConnection)
            {
                return (!authMechanism.IsPlainText) || connection.Session.SecureConnection;
            }

            return true;
        }

        public IEditableMessage OnCreateNewMessage(IConnection connection)
        {
            DirectoryInfo messageFolder = new DirectoryInfo(_settings.CustomMessageFolder);

            if (!messageFolder.Exists)
            {
                messageFolder.Create();
            }

            FileInfo filename = null;

            while (filename == null || filename.Exists)
            {
                filename = new FileInfo(Path.Combine(messageFolder.FullName,
                             DateTime.Now.ToString("yyyy-MM-dd_HHmmss-ffff") + ".eml"));
            }

            return new FileMessage(connection.Session, filename, false);
        }

        #endregion

        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<SessionEventArgs> SessionCompleted;

        public event EventHandler<SessionEventArgs> SessionStarted;
    }
}