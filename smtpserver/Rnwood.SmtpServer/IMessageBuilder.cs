using System;
using System.Collections.Generic;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IMessageBuilder
    {
        Stream WriteData();

        ISession Session { get; set; }

        DateTime ReceivedDate { get; set; }
        string From { get; set; }

        ICollection<string> To { get; }

        bool SecureConnection { get; set; }
        bool EightBitTransport { get; set; }
        long? DeclaredMessageSize { get; set; }

        IMessage ToMessage();

        Stream GetData();
    }
}