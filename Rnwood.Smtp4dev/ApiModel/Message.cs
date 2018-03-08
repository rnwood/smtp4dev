using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

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
                Parts.Add(HandleMimeEntity(mime.Body));
            }
        }


        private MessageEntitySummary HandleMimeEntity(MimeEntity entity)
        {
            int index = 0;

            return MimeEntityVisitor.Visit< MessageEntitySummary>(entity, null, (e, p) =>
            {
                MessageEntitySummary result = new MessageEntitySummary()
                {
                    MessageId = Id,
                    ContentId = e.ContentId ?? index.ToString(),
                    Name = e.ContentId + " - " + e.ContentType.MimeType,
                    Headers = e.Headers.Select(h => new Header { Name = h.Field, Value = h.Value }).ToList(),
                    ChildParts = new List<MessageEntitySummary>(),
                    Attachments = new List<AttachmentSummary>()
                };

                if (p != null)
                {
                    p.ChildParts.Add(result);

                    if (e.IsAttachment)
                    {
                        p.Attachments.Add(new AttachmentSummary() { ContentId = result.ContentId, FileName = e.ContentDisposition?.FileName });
                    }
                }

                index++;
                return result;
            });

        }

        internal static FileStreamResult GetPartContent(DbModel.MessageData result, string cid)
        {
            MimePart contentEntity = (MimePart) GetPart(result, cid);
            return new FileStreamResult(contentEntity.ContentObject.Open(), contentEntity.ContentType.MimeType);
        }

        internal static string GetPartSource(DbModel.MessageData result, string cid)
        {
            MimeEntity contentEntity = GetPart(result, cid);
            return contentEntity.ToString();
        }

        private static MimeEntity GetPart(DbModel.MessageData message, string cid)
        {
            MimeEntity result = null;

            using (MemoryStream stream = new MemoryStream(message.Data))
            {
                MimeMessage mime = MimeMessage.Load(stream);

                int index = 0;
                MimeEntityVisitor.Visit<DBNull>(mime.Body, null, (e, p) =>
                {
                    if (((e as MimePart)?.ContentId ?? (index++.ToString())) == cid)
                    {
                        result = e;
                    }

                    return DBNull.Value;
                });
            }

            return result;
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
