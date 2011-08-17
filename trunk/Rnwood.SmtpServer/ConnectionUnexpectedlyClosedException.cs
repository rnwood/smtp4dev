using System;
using System.IO;
using System.Runtime.Serialization;

namespace Rnwood.SmtpServer
{
    [Serializable]
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

        protected ConnectionUnexpectedlyClosedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}