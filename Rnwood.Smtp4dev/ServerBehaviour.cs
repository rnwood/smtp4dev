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

        public void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }

        public void OnSessionStarted(IConnectionProcessor processor, Session session)
        {
        }

        public void OnCommandReceived(IConnectionProcessor processor, SmtpCommand command)
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

        public bool RunOverSSL
        {
            get { return Settings.Default.EnableSSL; }
        }

        public X509Certificate GetSSLCertificate(IConnectionProcessor processor)
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

            return new X509Certificate(Settings.Default.SSLCertificatePath);
        }

        public Extension[] GetExtensions(IConnectionProcessor processor)
        {
            List<Extension> extensions = new List<Extension>();

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

            return extensions.ToArray();
        }

        public long? GetMaximumMessageSize(IConnectionProcessor processor)
        {
            long value = Settings.Default.MaximumMessageSize;
            return value != 0 ? value : (long?) null;
        }

        public void OnSessionCompleted(Session Session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionEventArgs(Session));
            }
        }

        public int GetReceiveTimeout(IConnectionProcessor processor)
        {
            return Settings.Default.ReceiveTimeout;
        }

        public AuthenticationResult ValidateAuthenticationRequest(IConnectionProcessor processor,
                                                                  AuthenticationRequest authenticationRequest)
        {
            return AuthenticationResult.Success;
        }

        public void OnMessageStart(IConnectionProcessor processor, string from)
        {
            if (Settings.Default.RequireAuthentication && !processor.Session.Authenticated)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                               "Must authenticate before sending mail"));
            }

            if (Settings.Default.RequireSecureConnection && !processor.Session.SecureConnection)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                               "Mail must be sent over secure connection"));
            }
        }

        public bool IsAuthMechanismEnabled(IConnectionProcessor processor, IAuthMechanism authMechanism)
        {
            if (Settings.Default.OnlyAllowClearTextAuthOverSecureConnection)
            {
                return (!authMechanism.IsPlainText) || processor.Session.SecureConnection;
            }

            return true;
        }

        #endregion

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<SessionEventArgs> SessionCompleted;
    }
}