using System;
using System.Collections.Generic;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IMessageBuilder
    {
        Stream WriteData();

        ISession Session { get; set; }

        new DateTime ReceivedDate { get; set; }
        new string From { get; set; }

        ICollection<string> To { get; }

        new bool SecureConnection { get; set; }
        new bool EightBitTransport { get; set; }
        new long? DeclaredMessageSize { get; set; }

        IMessage ToMessage();

        Stream GetData();
    }
}