using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Rnwood.SmtpServer;
using System.Security.Cryptography.X509Certificates;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.Smtp4dev
{
    public class ServerBehaviour : IServerBehaviour
    {
        public ServerBehaviour()
        {
        }

        public void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<SessionCompletedEventArgs> SessionCompleted;

        public string DomainName
        {
            get { return Properties.Settings.Default.DomainName; }
        }

        public IPAddress IpAddress
        {
            get
            {
                return IPAddress.Parse(Properties.Settings.Default.IPAddress);
            }
        }

        public int PortNumber
        {
            get { return Properties.Settings.Default.PortNumber; }
        }

        public bool RunOverSSL
        {
            get { return Properties.Settings.Default.EnableSSL; }
        }

        public System.Security.Cryptography.X509Certificates.X509Certificate GetSSLCertificate(IConnectionProcessor processor)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.SSLCertificatePath))
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows", false);
                string sdkPath = (string)key.GetValue("CurrentInstallFolder", null);

                if (sdkPath != null)
                {
                    string makeCertPath = sdkPath + "\\bin\\makecert.exe";
                    string makeCertArgs =
                        "-r -pe -n CN=\"{0}\" -e {1} -eku 1.3.6.1.5.5.7.3.1 -sky exchange -ss my -sp \"Microsoft RSA SChannel Cryptographic Provider\" -sy 12";

                    if (Directory.Exists(sdkPath))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(makeCertPath, string.Format(makeCertArgs, DomainName, DateTime.Today.AddYears(1).ToString("MM/dd/yyyy"))) { CreateNoWindow = true, UseShellExecute = false };
                        Process process = Process.Start(psi);
                        process.Start();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                            store.Open(OpenFlags.ReadOnly);

                            return store.Certificates.Find(X509FindType.FindBySubjectName, DomainName, false)[0];
                        }
                    }
                }

                return null;
            }

            return new X509Certificate(Properties.Settings.Default.SSLCertificatePath);
        }

        private StartTlsExtension _startTLSExtension = new StartTlsExtension();
        private EightBitMimeExtension _eightBitMimeExtension = new EightBitMimeExtension();
        private AuthExtension _authExtension = new AuthExtension();
        private SizeExtension _sizeExtension = new SizeExtension();

        public Extension[] GetExtensions(IConnectionProcessor processor)
        {
            List<Extension> extensions = new List<Extension>();

            if (Properties.Settings.Default.Enable8BITMIME)
            {
                extensions.Add(_eightBitMimeExtension);
            }

            if (Properties.Settings.Default.EnableSTARTTLS)
            {
                extensions.Add(_startTLSExtension);
            }

            if (Properties.Settings.Default.EnableAUTH)
            {
                extensions.Add(_authExtension);
            }

            if (Properties.Settings.Default.EnableSIZE)
            {
                extensions.Add(_sizeExtension);
            }

            return extensions.ToArray();
        }

        public long? GetMaximumMessageSize(IConnectionProcessor processor)
        {
            long value = Properties.Settings.Default.MaximumMessageSize;
            return value != 0 ? value : (long?)null;
        }

        public void OnSessionCompleted(Session Session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionCompletedEventArgs(Session));
            }
        }

        public int GetReceiveTimeout(IConnectionProcessor processor)
        {
            return Properties.Settings.Default.ReceiveTimeout;
        }

        public AuthenticationResult ValidateAuthenticationRequest(IConnectionProcessor processor, AuthenticationRequest authenticationRequest)
        {
            return AuthenticationResult.Success;
        }

        public void OnMessageStart(IConnectionProcessor processor, string from)
        {
            if (Properties.Settings.Default.RequireAuthentication && !processor.Session.Authenticated)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "Must authenticate before sending mail"));
            }

            if (Properties.Settings.Default.RequireSecureConnection && !processor.Session.SecureConnection)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "Mail must be sent over secure connection"));
            }
        }

        public bool IsAuthMechanismEnabled(IConnectionProcessor processor, IAuthMechanism authMechanism)
        {
            if (Properties.Settings.Default.OnlyAllowClearTextAuthOverSecureConnection)
            {
                return (!authMechanism.IsPlainText) || processor.Session.SecureConnection;
            }

            return true;
        }
    }

}
