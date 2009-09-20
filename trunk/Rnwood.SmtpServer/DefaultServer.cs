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
        public DefaultServer(int portNumber, X509Certificate sslCertificate) : base(new DefaultServerBehaviour(portNumber, sslCertificate))
        {            
        }
    }
}
