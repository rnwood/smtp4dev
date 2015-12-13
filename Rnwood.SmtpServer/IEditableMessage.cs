using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IEditableMessage : IMessage
    {
        Stream GetData(DataAccessMode dataAccessMode);

        DateTime ReceivedDate { get; set; }
        string From { get; set; }
        void AddTo(string to);

        bool SecureConnection { get; set; }
        bool EightBitTransport { get; set; }
        long? DeclaredMessageSize { get; set; }
    }
}