using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IEditableMessage : IMessage
    {
        Stream GetData(bool forWriting);

        DateTime ReceivedDate { get; set; }
        string From { get; set; }
        void AddTo(string to);

        bool SecureConnection { get; set; }
        bool EightBitTransport { get; set; }
        long? DeclaredMessageSize { get; set; }
    }

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