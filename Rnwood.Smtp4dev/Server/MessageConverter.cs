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

            MimeMessage mime = MimeMessage.Load(messageData);

            Message message = new Message()
            {
                Id = Guid.NewGuid(),

                From = mime.From.ToString(),
                To = mime.To.ToString(),
                ReceivedDate = DateTime.Now,
                Subject = mime.Subject
            };

            message.Parts = new List<DbModel.MessagePart>();

            foreach (MimeEntity mimeEntity in mime.BodyParts)
            {
                DbModel.MessagePart part = new DbModel.MessagePart()
                {
                    Id = Guid.NewGuid(),
                    Owner = message,
                    Content = mimeEntity.ToString()
                };

                message.Parts.Add(part);
            }


            return message;
        }
    }
}
