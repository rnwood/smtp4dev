using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.DbModel;
using System.Net.Http.Headers;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Message
    {
        public Message(DbModel.Message dbMessage, DbModel.MessageData dbMessageData)
        {
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To;
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;

            Parts = new List<ApiModel.MessageEntitySummary>();

            using (MemoryStream stream = new MemoryStream(dbMessageData.Data))
            {
                MimeMessage mime = MimeMessage.Load(stream);

                Headers = mime.Headers.Select(h => new Header { Name = h.Field, Value = h.Value }).ToList();

                IEnumerable<MimeEntity> parts = mime.BodyParts;
                Parts.Add(HandleMimeEntity(mime.Body));
            }
        }


        private MessageEntitySummary HandleMimeEntity(MimeEntity entity)
        {
            MessageEntitySummary result = new MessageEntitySummary()
            {
                Name = entity.ContentId + " - " + entity.ContentType.MimeType,
                Headers = entity.Headers.Select(h => new Header { Name = h.Field, Value = h.Value }).ToList(),
                ChildParts = new List<MessageEntitySummary>(),
                Source = entity.ToString()
            };

            using (MemoryStream memoryStream = new MemoryStream())
            {
                entity.WriteTo(memoryStream, true);
                result.Body = Encoding.UTF8.GetString(memoryStream.ToArray());
            }


            if (entity is Multipart multipart)
            {
                foreach (MimeEntity childEntity in multipart)
                {
                    result.ChildParts.Add(HandleMimeEntity(childEntity));
                }
            }
            else if (entity is MimeKit.MessagePart rfc822)
            {
                result.Html = rfc822.Message.HtmlBody;
                result.ChildParts.Add(HandleMimeEntity(rfc822.Message.Body));
            }
            else
            {
                var part = (MimePart)entity;

            }

            return result;

        }

        internal static FileStreamResult GetPartContent(DbModel.MessageData result, string cid)
        {
            using (MemoryStream stream = new MemoryStream(result.Data))
            {
                MimeMessage message = MimeMessage.Load(stream);

                Func<IEnumerable<MimeEntity>, MimeEntity> visit = null;

                visit = mimeEntities =>
                {
                    foreach (MimeEntity mimeEntity in mimeEntities)
                    {

                        if (mimeEntity.ContentId == cid)
                        {
                            return mimeEntity;
                        }

                        if (mimeEntity is Multipart multiPart)
                        {
                            var childResult = visit(mimeEntities);
                            if (childResult != null)
                            {
                                return childResult;
                            }
                        }
                    }

                    return null;
                };

                MimePart contentEntity = (MimePart) visit(message.BodyParts) ;
                return new FileStreamResult(contentEntity.ContentObject.Open(), contentEntity.ContentType.MimeType);
            }
        }

        public static string GetHtml(DbModel.MessageData dbMessageData)
        {
            using (MemoryStream stream = new MemoryStream(dbMessageData.Data))
            {
                MimeMessage mime = MimeMessage.Load(stream);

                

                return mime.HtmlBody;
            }
        }

        public Guid Id { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public List<MessageEntitySummary> Parts { get; set; }

        public List<Header> Headers { get; set; }
    }
}
