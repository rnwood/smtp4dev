using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Rnwood.SmtpServer;
using System.Security.Cryptography.X509Certificates;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.Smtp4dev
{
    public class ServerBehaviour : IServerBehaviour
    {
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
                return IPAddress.Parse(Properties.Settings.Default.IPAddress); }
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
                return new X509Certificate(Properties.Resources.localhost);
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
            return value != 0 ? value : (long?) null;
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
    }

}
