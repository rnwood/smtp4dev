// <copyright file="DefaultServer.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// A default subclass of <see cref="SmtpServer"/> which provides a default behaviour which is suitable for many simple
    /// applications.
    /// </summary>
    /// <seealso cref="Rnwood.SmtpServer.SmtpServer" />
    public class DefaultServer : SmtpServer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultServer" /> class.
        /// Initializes a new SMTP over SSL server on port 465 using the
        /// supplied SSL certificate.
        /// </summary>
        /// <param name="allowRemoteConnections">if set to <c>true</c> remote collections are allowed.</param>
        /// <param name="sslCertificate">The SSL certificate to use for the server.</param>
        public DefaultServer(bool allowRemoteConnections, X509Certificate sslCertificate)
            : this(allowRemoteConnections, 465, sslCertificate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultServer" /> class.
        /// Initializes a new SMTP server on port 25.
        /// </summary>
        /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections are allowed.</param>
        public DefaultServer(bool allowRemoteConnections)
            : this(allowRemoteConnections, 25, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultServer"/> class.
        /// Initializes a new SMTP server on the specified port number.
        /// </summary>
        ///
        /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections are allowed.</param>
        /// <param name="portNumber">The port number.</param>
        public DefaultServer(bool allowRemoteConnections, int portNumber)
            : this(allowRemoteConnections, portNumber, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultServer"/> class.
        /// Initializes a new SMTP over SSL server on the specified port number
        /// using the supplied SSL certificate.
        /// </summary>
        ///
        /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections are allowed.</param>
        /// <param name="portNumber">The port number.</param>
        /// <param name="sslCertificate">The SSL certificate.</param>
        public DefaultServer(bool allowRemoteConnections, int portNumber, X509Certificate sslCertificate)
            : this(new DefaultServerBehaviour(allowRemoteConnections, portNumber, sslCertificate))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultServer" /> class.
        /// Initializes a new SMTP over SSL server on the specified standard port number
        /// </summary>
        /// <param name="allowRemoteConnections">if set to <c>true</c> connection from remote computers are allowed.</param>
        /// <param name="port">The standard port (or auto) to use.</param>
        public DefaultServer(bool allowRemoteConnections, StandardSmtpPort port)
            : this(new DefaultServerBehaviour(allowRemoteConnections, (int)port))
        {
        }

        private DefaultServer(DefaultServerBehaviour behaviour)
            : base(behaviour)
        {
        }

        /// <summary>
        /// Occurs when authentication results need to be validated.
        /// </summary>
        public event AsyncEventHandler<AuthenticationCredentialsValidationEventArgs> AuthenticationCredentialsValidationRequiredEventHandler
        {
            add { this.Behaviour.AuthenticationCredentialsValidationRequiredAsync += value; }
            remove { this.Behaviour.AuthenticationCredentialsValidationRequiredAsync -= value; }
        }

        /// <summary>
        /// Occurs when a mesage has been fully received but not yet acknlowledged by the server.
        /// </summary>
        public event AsyncEventHandler<ConnectionEventArgs> MessageCompletedEventHandler
        {
            add { this.Behaviour.MessageCompletedEventHandler += value; }
            remove { this.Behaviour.MessageCompletedEventHandler -= value; }
        }

        /// <summary>
        /// Occurs when a message has been receieved and acknlowledged by the server.
        /// </summary>
        public event AsyncEventHandler<MessageEventArgs> MessageReceivedEventHandler
        {
            add { this.Behaviour.MessageReceivedEventHandler += value; }
            remove { this.Behaviour.MessageReceivedEventHandler -= value; }
        }

        /// <summary>
        /// Occurs when a session is terminated.
        /// </summary>
        public event AsyncEventHandler<SessionEventArgs> SessionCompletedEventHandler
        {
            add { this.Behaviour.SessionCompletedEventHandler += value; }
            remove { this.Behaviour.SessionCompletedEventHandler -= value; }
        }

        /// <summary>
        /// Occurs when a new session is started, when a new client connects to the server.
        /// </summary>
        public event AsyncEventHandler<SessionEventArgs> SessionStartedHandler
        {
            add { this.Behaviour.SessionStartedEventHandler += value; }
            remove { this.Behaviour.SessionStartedEventHandler -= value; }
        }

        /// <summary>
        /// Gets the Behaviour.
        /// </summary>
        protected new DefaultServerBehaviour Behaviour => (DefaultServerBehaviour)base.Behaviour;
    }
}
