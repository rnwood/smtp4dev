using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Rnwood.Smtp4dev.Migrations;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Message : ICacheByKey
    {
        public Message(DbModel.Message dbMessage)
        {
            Data = dbMessage.Data;
            Id = dbMessage.Id;
            From = dbMessage.From;
            To = dbMessage.To.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Cc = Array.Empty<string>();
            Bcc = Array.Empty<string>();
            DeliveredTo = dbMessage.DeliveredTo.Split(',', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
            ReceivedDate = dbMessage.ReceivedDate;
            Subject = dbMessage.Subject;
            SecureConnection = dbMessage.SecureConnection;
            SessionEncoding = dbMessage.SessionEncoding;
            EightBitTransport = dbMessage.EightBitTransport;

            Parts = new List<MessageEntitySummary>(1);
            RelayError = dbMessage.RelayError;

            if (dbMessage.MimeParseError != null)
            {
                MimeParseError = dbMessage.MimeParseError;
                Headers = new List<Header>(0);
                HasPlainTextBody = true;
            }
            else
            {
                using var stream = new MemoryStream(dbMessage.Data);
                MimeMessage = MimeMessage.Load(stream);

                if (MimeMessage.From != null)
                {
                    From = MimeMessage.From.ToString();
                }

                var recipients = new List<string>(dbMessage.To.Split(",")
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r)));

                if (MimeMessage.To != null)
                {
                    To = MimeMessage.To.Select(t => PunyCodeReplacer.DecodePunycode(t.ToString())).ToArray();

                    foreach (var internetAddress in MimeMessage.To.Where(t => t is MailboxAddress))
                    {
                        var to = (MailboxAddress)internetAddress;
                        var decodedToAddress = PunyCodeReplacer.DecodePunycode(to.Address);
                        recipients.Remove(decodedToAddress.TrimEnd('.'));
                        recipients.Remove($"{decodedToAddress}.");
                    }
                }

                if (MimeMessage.Cc != null)
                {
                    Cc = MimeMessage.Cc.Select(t => PunyCodeReplacer.DecodePunycode(t.ToString())).ToArray();

                    foreach (var internetAddress in MimeMessage.Cc.Where(t => t is MailboxAddress))
                    {
                        var cc = (MailboxAddress)internetAddress;
                        var decodedCCAddress = PunyCodeReplacer.DecodePunycode(cc.Address);
                        recipients.Remove(decodedCCAddress.TrimEnd('.'));
                        recipients.Remove($"{decodedCCAddress}.");
                    }
                }

                Bcc = recipients.ToArray();

                Headers = MimeMessage.Headers.Select(h => new Header { Name = h.Field, Value = PunyCodeReplacer.DecodePunycode(h.Value) }).ToList();
                Parts.Add(HandleMimeEntity(MimeMessage.Body));
                HasHtmlBody = MimeMessage.HtmlBody != null;
                HasPlainTextBody = MimeMessage.TextBody != null;
            }
        }

        public string SessionEncoding { get; set; }

        public bool? EightBitTransport { get; set; }


        private MessageEntitySummary HandleMimeEntity(MimeEntity entity)
        {
            var index = 0;

            return MimeEntityVisitor.VisitWithResults<MessageEntitySummary>(entity, (e, p) =>
           {
               var fileName = PunyCodeReplacer.DecodePunycode(!string.IsNullOrEmpty(e.ContentDisposition?.FileName)
            ? e.ContentDisposition?.FileName
            : e.ContentType?.Name);

               var result = new MessageEntitySummary
               {
                   MessageId = Id,
                   Id = index.ToString(),
                   ContentId = e.ContentId,
                   Name = (fileName ?? e.ContentId ?? index.ToString()) + " - " + e.ContentType.MimeType,
                   Headers = e.Headers.Select(h => new Header { Name = h.Field, Value = PunyCodeReplacer.DecodePunycode(h.Value) }).ToList(),
                   ChildParts = new List<MessageEntitySummary>(),
                   Attachments = new List<AttachmentSummary>(),
                   Warnings = new List<MessageWarning>(),
                   Size = e.ToString().Length,
                   IsAttachment = (e.ContentDisposition?.Disposition != "inline" && !string.IsNullOrEmpty(fileName)) || e.ContentDisposition?.Disposition == "attachment",
                   MimeEntity = e
               };

               if (p != null)
               {
                   p.ChildParts.Add(result);

                   if (result.IsAttachment)
                   {
                       if (e.ContentDisposition?.Disposition != "attachment")
                       {
                           result.Warnings.Add(new MessageWarning { Details = $"Attachment '{fileName}' should have \"Content-Disposition: attachment\" header." });
                       }

                       if (string.IsNullOrEmpty(fileName))
                       {
                           result.Warnings.Add(new MessageWarning { Details = $"Attachment with content ID '{e.ContentId}' should have filename specified in either 'Content-Type' or 'Content-Disposition' header." });
                       }

                       p.Attachments.Add(new AttachmentSummary()
                       {
                           Id = result.Id,
                           ContentId = result.ContentId,
                           FileName = fileName,
                           Url = $"api/messages/{Id}/part/{result.Id}/content"
                       });
                   }
               }

               index++;
               return result;
           });

        }

        internal static FileStreamResult GetPartContent(Message result, string cid)
        {
            var contentEntity = GetPart(result, cid);

            if (contentEntity is MimePart mimePart && mimePart.Content != null)
            {
                return new FileStreamResult(mimePart.Content.Open(), contentEntity.ContentType?.MimeType ?? "application/text")
                {
                    FileDownloadName = mimePart.FileName ??
                                       ((contentEntity.ContentId ?? "content") + (MimeTypes.TryGetExtension(mimePart.ContentType.MimeType, out string extn) ? extn : ""))
                };
            }
            else
            {
                var outputStream = new MemoryStream();
                contentEntity.WriteTo(outputStream, true);
                outputStream.Seek(0, SeekOrigin.Begin);

                return new FileStreamResult(outputStream, contentEntity.ContentType?.MimeType ?? "application/text");
            }
        }

        public bool HasHtmlBody { get; set; }

        public bool HasPlainTextBody { get; set; }

        internal static string GetPartContentAsText(Message result, string id)
        {
            var contentEntity = GetPart(result, id);

            if (contentEntity is MimePart part && part.Content != null)
            {
                var encoding = part.ContentType.CharsetEncoding ?? ApiModel.Message.GetSessionEncodingOrAssumed(result);
                using var reader = new StreamReader(part.Content.Open(), encoding);
                return reader.ReadToEnd();
            }

            return contentEntity.ToString();

        }

        internal static Encoding GetSessionEncodingOrAssumed(Message result)
        {
            return !string.IsNullOrEmpty(result.SessionEncoding) ? Encoding.GetEncoding(result.SessionEncoding) : Encoding.Latin1;
        }


        internal static string GetPartSource(Message message, string id)
        {
            var contentEntity = GetPart(message, id);
            using (MemoryStream ms = new MemoryStream())
            {
                contentEntity.WriteTo(ms, false);
                var encoding = contentEntity.ContentType.CharsetEncoding ?? ApiModel.Message.GetSessionEncodingOrAssumed(message);
                return encoding.GetString(ms.GetBuffer());
            }

        }


        private static MimeEntity GetPart(Message message, string id)
        {
            var part = message.Parts.Flatten(p => p.ChildParts).SingleOrDefault(p => p.Id == id);

            if (part == null)
            {
                throw new FileNotFoundException($"Part with id '{id}' in message {message.Id} is not found");
            }

            return part.MimeEntity;
        }

        public Guid Id { get; set; }

        public string From { get; set; }
        public string[] To { get; set; }
        public string[] Cc { get; set; }
        public string[] Bcc { get; set; }
        public string[] DeliveredTo { get; set; }
        public DateTime ReceivedDate { get; set; }

        public bool SecureConnection { get; set; }

        public string Subject { get; set; }

        public List<MessageEntitySummary> Parts { get; set; }

        public List<Header> Headers { get; set; }

        public string MimeParseError { get; set; }

        public string RelayError { get; set; }

        [JsonIgnore]
        internal MimeMessage MimeMessage { get; set; }

        internal byte[] Data { get; set; }

        [JsonIgnore]
        string ICacheByKey.CacheKey => Id.ToString() + "v4";
    }
}
