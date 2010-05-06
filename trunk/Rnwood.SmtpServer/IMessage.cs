using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IMessage
    {
        DateTime ReceivedDate { get; set; }
        ISession Session { get; }
        string From { get; set; }
        string[] To { get; }

        Stream GetData();
        Stream GetData(bool forWriting);
    }
}