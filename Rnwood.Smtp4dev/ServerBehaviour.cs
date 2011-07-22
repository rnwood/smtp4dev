#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Rnwood.Smtp4dev.Properties;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

#endregion

namespace Rnwood.Smtp4dev
{
    public class ServerBehaviour : IServerBehaviour
    {
        private readonly AuthExtension _authExtension = new AuthExtension();
        private readonly EightBitMimeExtension _eightBitMimeExtension = new EightBitMimeExtension();
        private readonly SizeExtension _sizeExtension = new SizeExtension();
        private readonly StartTlsExtension _startTLSExtension = new StartTlsExtension();

        #region IServerBehaviour Members

        public void OnMessageCompleted(IConnection connection)
        {

        }

        public void OnMessageReceived(IConnection connection, Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageEventArgs(message));
            }
        }

        public void OnMessageRecipientAdding(IConnection connection, Message message, string recipient)
        {

        }

        public void OnSessionStarted(IConnection connection, ISession session)
        {
        }

        public void OnCommandReceived(IConnection connection, SmtpCommand command)
        {
        }

        public string DomainName
        {
            get { return Settings.Default.DomainName; }
        }

        public IPAddress IpAddress
        {
            get { return IPAddress.Parse(Settings.Default.IPAddress); }
        }

        public int PortNumber
        {
            get { return Settings.Default.PortNumber; }
        }

        public bool IsSSLEnabled(IConnection connection)
        {
            return Settings.Default.EnableSSL;
        }

        public bool IsSessionLoggingEnabled(IConnection connection)
        {
            return true;
        }

        public X509Certificate GetSSLCertificate(IConnection connection)
        {
            if (string.IsNullOrEmpty(Settings.Default.SSLCertificatePath))
            {
                //RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows", false);
                //string sdkPath = (string)key.GetValue("CurrentInstallFolder", null);

                //if (sdkPath != null)
                //{
                //    string makeCertPath = sdkPath + "\\bin\\makecert.exe";
                //    string makeCertArgs =
                //        "-r -pe -n CN=\"{0}\" -e {1} -eku 1.3.6.1.5.5.7.3.1 -sky exchange -ss my -sp \"Microsoft RSA SChannel Cryptographic Provider\" -sy 12";

                //    if (Directory.Exists(sdkPath))
                //    {
                //        ProcessStartInfo psi = new ProcessStartInfo(makeCertPath, string.Format(makeCertArgs, DomainName, DateTime.Today.AddYears(1).ToString("MM/dd/yyyy"))) { CreateNoWindow = true, UseShellExecute = false };
                //        Process process = Process.Start(psi);
                //        process.Start();
                //        process.WaitForExit();

                //        if (process.ExitCode == 0)
                //        {
                //            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                //            store.Open(OpenFlags.ReadOnly);

                //            return store.Certificates.Find(X509FindType.FindBySubjectName, DomainName, false)[0];
                //        }
                //    }
                //}

                return null;
            }

            if (string.IsNullOrEmpty(Settings.Default.SSLCertificatePassword))
            {
                return new X509Certificate(Settings.Default.SSLCertificatePath);
            }

            return new X509Certificate(Settings.Default.SSLCertificatePath, Settings.Default.SSLCertificatePassword);
        }

        public IEnumerable<IExtension> GetExtensions(IConnection connection)
        {
            List<IExtension> extensions = new List<IExtension>();

            if (Settings.Default.Enable8BITMIME)
            {
                extensions.Add(_eightBitMimeExtension);
            }

            if (Settings.Default.EnableSTARTTLS)
            {
                extensions.Add(_startTLSExtension);
            }

            if (Settings.Default.EnableAUTH)
            {
                extensions.Add(_authExtension);
            }

            if (Settings.Default.EnableSIZE)
            {
                extensions.Add(_sizeExtension);
            }

            return extensions;
        }

        public long? GetMaximumMessageSize(IConnection connection)
        {
            long value = Settings.Default.MaximumMessageSize;
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
            return Settings.Default.ReceiveTimeout;
        }

        public AuthenticationResult ValidateAuthenticationCredentials(IConnection connection,
                                                                  IAuthenticationRequest authenticationRequest)
        {
            return AuthenticationResult.Success;
        }

        public void OnMessageStart(IConnection connection, string from)
        {
            if (Settings.Default.RequireAuthentication && !connection.Session.Authenticated)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationRequired,
                                                               "Must authenticate before sending mail"));
            }

            if (Settings.Default.RequireSecureConnection && !connection.Session.SecureConnection)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                               "Mail must be sent over secure connection"));
            }
        }

        public bool IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism)
        {
            if (Settings.Default.OnlyAllowClearTextAuthOverSecureConnection)
            {
                return (!authMechanism.IsPlainText) || connection.Session.SecureConnection;
            }

            return true;
        }

        public IMessage CreateMessage(IConnection connection)
        {
            return new Message(connection.Session);
        }

        #endregion

        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<SessionEventArgs> SessionCompleted;
    }
}