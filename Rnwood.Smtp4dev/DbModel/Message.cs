using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.DbModel
{
    public class Message
    {
        public Message(SmtpServer.IMessage message)
        {
            Id = Guid.NewGuid();

            From = message.From;
            To = message.To;
            ReceivedDate = message.ReceivedDate;
            Stream dataStream = message.GetData();
            byte[] data = new byte[dataStream.Length];
            dataStream.Read(data, 0, data.Length);

            Data = data;
        }

        public Guid Id { get; private set; }

        public string From { get; set; }
        public string[] To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public byte[] Data { get; set; }
    }
}
