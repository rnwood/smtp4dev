using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace Rnwood.SmtpServer
{
    public class DefaultServer : Server
    {
        /// <summary>
        /// Initializes a new SMTP over SSL server on port 465 using the
        /// supplied SSL certificate.
        /// </summary>
        /// <param name="sslCertificate">The SSL certificate to use for the server.</param>
        public DefaultServer(X509Certificate sslCertificate)
            : this(465, sslCertificate)
        {
        }

        /// <summary>
        /// Initializes a new SMTP server on port 25.
        /// </summary>
        public DefaultServer()
            : this(25, null)
        {
        }

        /// <summary>
        /// Initializes a new SMTP server on the specified port number.
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        public DefaultServer(int portNumber)
            : this(portNumber, null)
        {
        }

        /// <summary>
        /// Initializes a new SMTP over SSL server on the specified port number
        /// using the supplied SSL ceritificate.
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        /// <param name="sslCertificate">The SSL certificate.</param>
        public DefaultServer(int portNumber, X509Certificate sslCertificate)
            : this(new DefaultServerBehaviour(portNumber, sslCertificate))
        {
        }

        /// <summary>
        /// Initializes a new SMTP over SSL server on the specified standard port number
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        /// <param name="sslCertificate">The SSL certificate.</param>
        public DefaultServer(Ports port)
            : this(new DefaultServerBehaviour((int)port))
        {
        }

        private DefaultServer(DefaultServerBehaviour behaviour) : base(behaviour)
        {
        }

        new protected DefaultServerBehaviour Behaviour
        {
            get
            {
                return (DefaultServerBehaviour) base.Behaviour;
            }
        }

        public event EventHandler<MessageEventArgs> MessageReceived
        {
            add { Behaviour.MessageReceived += value; }
            remove { Behaviour.MessageReceived -= value; }
        }

        public event EventHandler<SessionEventArgs> SessionCompleted
        {
            add { Behaviour.SessionCompleted += value; }
            remove { Behaviour.SessionCompleted -= value; }
        }

        public event EventHandler<SessionEventArgs> SessionStarted
        {
            add { Behaviour.SessionStarted += value; }
            remove { Behaviour.SessionStarted -= value; }
        }

        public event EventHandler<AuthenticationCredentialsValidationEventArgs> AuthenticationCredentialsValidationRequired
        {
            add { Behaviour.AuthenticationCredentialsValidationRequired += value; }
            remove { Behaviour.AuthenticationCredentialsValidationRequired -= value; }
        }

        public event EventHandler<MessageEventArgs> MessageCompleted
        {
            add { Behaviour.MessageCompleted += value; }
            remove { Behaviour.MessageCompleted -= value; }
        }
    }

    public enum Ports
    {
        AssignAutomatically = 0,
        SMTP = 25,
        SMTPOverSSL=465
    }
}
