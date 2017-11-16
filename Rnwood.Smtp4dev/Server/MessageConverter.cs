using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public class MessageConverter
    {
        public Message Convert(Stream messageData)
        {
            MimeMessage mime = MimeMessage.Load(messageData, false);

            messageData.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[messageData.Length];
            messageData.Read(data, 0, data.Length);

            Message message = new Message()
            {
                Id = Guid.NewGuid(),

                From = mime.From.ToString(),
                To = mime.To.ToString(),
                ReceivedDate = DateTime.Now,
                Subject = mime.Subject,
                Data = data

            };
            
            return message;
        }
    }
}
