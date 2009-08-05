using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Rnwood.SmtpServer;
using System.Security.Cryptography.X509Certificates;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.Smtp4dev
{
    public class Smtp4DevServerBehaviour : ServerBehaviour
    {
        public override void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }
                
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<SessionCompletedEventArgs> SessionCompleted;

        public override string DomainName
        {
            get { return Properties.Settings.Default.DomainName; }
        }

        public override IPAddress IpAddress
        {
            get 
            {     
                return IPAddress.Parse(Properties.Settings.Default.IPAddress); }
        }

        public override int PortNumber
        {
            get { return Properties.Settings.Default.PortNumber; }
        }

        public override bool RunOverSSL
        {
            get { return Properties.Settings.Default.EnableSSL; }
        }

        public override System.Security.Cryptography.X509Certificates.X509Certificate GetSSLCertificate(ConnectionProcessor processor)
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

        public override Extension[] GetExtensions(ConnectionProcessor processor)
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

        public override long? GetMaximumMessageSize(ConnectionProcessor processor)
        {
            long value = Properties.Settings.Default.MaximumMessageSize;
            return value != 0 ? value : (long?) null;
        }

        public override void OnSessionCompleted(Session Session)
        {
            if (SessionCompleted != null)
            {
                SessionCompleted(this, new SessionCompletedEventArgs(Session));
            }
        }
    }

    public class SessionCompletedEventArgs : EventArgs
    {
        public SessionCompletedEventArgs(Session session)
        {
            Session = session;
        }

        public Session Session { get; private set; }

    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }

    }
}
