using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.IO;

namespace Rnwood.Smtp4dev.Server
{
    public class MessageConverter
    {
        public Tuple<Message, MessageData>  Convert(Stream messageStream)
        {
            MimeMessage mime = MimeMessage.Load(messageStream, false);

            messageStream.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[messageStream.Length];
            messageStream.Read(data, 0, data.Length);

            Message message = new Message
            {
                Id = Guid.NewGuid(),

                From = mime.From.ToString(),
                To = mime.To.ToString(),
                ReceivedDate = DateTime.Now,
                Subject = mime.Subject
            };

            MessageData messageData = new MessageData
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Data = data
            };


            return new Tuple<Message, MessageData>(message, messageData);
        }
    }
}
