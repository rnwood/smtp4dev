using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Message
    {


        public Message(DbModel.Message dbMessage)
        {
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To;
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;

            Parts = new List<ApiModel.MessagePart>();

            using (MemoryStream stream = new MemoryStream(dbMessage.Data))
            {
                MimeMessage mime = MimeMessage.Load(stream);

                foreach (MimeEntity mimeEntity in mime.BodyParts)
                {
                    MessagePart part = new MessagePart()
                    {
                        Headers = string.Join(Environment.NewLine, mimeEntity.Headers.Select(h => h.ToString()))
                    };

                    Parts.Add(part);
                }
            }
        }

        public Guid Id { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public List<MessagePart> Parts { get; set; }
    }
}
