using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MimeKit;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Message
    {
        public Message(DbModel.Message dbMessage)
        {
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To;
            Cc = "";
            Bcc = "";
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;

            Parts = new List<MessageEntitySummary>();

            if (dbMessage.MimeParseError != null)
            {
                MimeParseError = dbMessage.MimeParseError;
                Headers = new List<Header>();
                Parts = new List<MessageEntitySummary>();
            }
            else
            {
                using (var stream = new MemoryStream(dbMessage.Data))
                {
                    var mime = MimeMessage.Load(stream);

                    if (mime.From != null) From = mime.From.ToString();

                    var recipients = new List<string>(dbMessage.To.Split(",")
                        .Select(r => r.Trim())
                        .Where(r => !string.IsNullOrEmpty(r)));

                    if (mime.To != null)
                    {
                        To = string.Join(", ", mime.To.Select(t => t.ToString()));

                        foreach (MailboxAddress to in mime.To.Where(t => t is MailboxAddress))
                            recipients.Remove(to.Address);
                    }

                    if (mime.Cc != null)
                    {
                        Cc = string.Join(", ", mime.Cc.Select(t => t.ToString()));

                        foreach (MailboxAddress cc in mime.Cc.Where(t => t is MailboxAddress))
                            recipients.Remove(cc.Address);
                    }

                    Bcc = string.Join(", ", recipients);

                    Headers = mime.Headers.Select(h => new Header {Name = h.Field, Value = h.Value}).ToList();
                    Parts.Add(HandleMimeEntity(mime.Body));
                }
            }
        }

        public Guid Id { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string Subject { get; set; }

        public List<MessageEntitySummary> Parts { get; set; }

        public List<Header> Headers { get; set; }

        public string MimeParseError { get; set; }


        private MessageEntitySummary HandleMimeEntity(MimeEntity entity)
        {
            var index = 0;

            return MimeEntityVisitor.Visit<MessageEntitySummary>(entity, null, (e, p) =>
            {
                var cid = e.ContentId ?? index.ToString();

                var result = new MessageEntitySummary
                {
                    MessageId = Id,
                    ContentId = cid,
                    Name = e.ContentId + " - " + e.ContentType.MimeType,
                    Headers = e.Headers.Select(h => new Header {Name = h.Field, Value = h.Value}).ToList(),
                    ChildParts = new List<MessageEntitySummary>(),
                    Attachments = new List<AttachmentSummary>()
                };

                if (p != null)
                {
                    p.ChildParts.Add(result);

                    if (e.IsAttachment)
                        p.Attachments.Add(new AttachmentSummary
                        {
                            ContentId = result.ContentId,
                            FileName = e.ContentDisposition?.FileName,
                            Url = $"/api/messages/{Id}/part/{result.ContentId}/content"
                        });
                }

                index++;
                return result;
            });
        }

        internal static FileStreamResult GetPartContent(DbModel.Message result, string cid)
        {
            var contentEntity = (MimePart) GetPart(result, cid);
            return new FileStreamResult(contentEntity.ContentObject.Open(), contentEntity.ContentType.MimeType);
        }

        internal static string GetPartSource(DbModel.Message result, string cid)
        {
            var contentEntity = GetPart(result, cid);
            return contentEntity.ToString();
        }

        private static MimeEntity GetPart(DbModel.Message message, string cid)
        {
            MimeEntity result = null;

            using (var stream = new MemoryStream(message.Data))
            {
                var mime = MimeMessage.Load(stream);

                var index = 0;
                MimeEntityVisitor.Visit<DBNull>(mime.Body, null, (e, p) =>
                {
                    if (((e as MimePart)?.ContentId ?? index++.ToString()) == cid) result = e;

                    return DBNull.Value;
                });
            }

            return result;
        }

        public static string GetHtml(DbModel.Message dbMessage)
        {
            using (var stream = new MemoryStream(dbMessage.Data))
            {
                var mime = MimeMessage.Load(stream);

                return mime.HtmlBody;
            }
        }

        public static string GetText(DbModel.Message dbMessage)
        {
            using (var stream = new MemoryStream(dbMessage.Data))
            {
                var mime = MimeMessage.Load(stream);

                return mime.TextBody;
            }
        }
    }
}