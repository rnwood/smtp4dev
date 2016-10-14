using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class ConnectionUnexpectedlyClosedException : IOException
    {
        public ConnectionUnexpectedlyClosedException()
        {
        }

        public ConnectionUnexpectedlyClosedException(string message) : base(message)
        {
        }

        public ConnectionUnexpectedlyClosedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}