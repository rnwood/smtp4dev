using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IMessage : IDisposable
    {
        DateTime ReceivedDate { get; }
        ISession Session { get; }
        string From { get; }
        string[] To { get; }

        bool SecureConnection { get; }
        bool EightBitTransport { get; }
        long? DeclaredMessageSize { get;  }

        Stream GetData();
        
    }
}