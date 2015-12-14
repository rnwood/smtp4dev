using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IEditableMessage : IMessage
    {
        Stream GetData(DataAccessMode dataAccessMode);

        new DateTime ReceivedDate { get; set; }
        new string From { get; set; }
        void AddTo(string to);

        new bool SecureConnection { get; set; }
        new bool EightBitTransport { get; set; }
        new long? DeclaredMessageSize { get; set; }
    }
}