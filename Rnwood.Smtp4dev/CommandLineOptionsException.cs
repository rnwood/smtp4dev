using System;
using System.Runtime.Serialization;

namespace Rnwood.Smtp4dev
{
    [Serializable]
    public class CommandLineOptionsException : Exception
    {
        public bool IsHelpRequest { get; set; }

        public CommandLineOptionsException()
        {
        }

        public CommandLineOptionsException(string message) : base(message)
        {
        }

        public CommandLineOptionsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CommandLineOptionsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}