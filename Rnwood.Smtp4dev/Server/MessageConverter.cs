using MimeKit;
using Rnwood.Smtp4dev.ApiModel;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rnwood.SmtpServer;
using System.Text.Json;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Rnwood.Smtp4dev.Server
{
    public class MessageConverter
    {
        public async Task<DbModel.Message> ConvertAsync(IMessage message, string[] deliveredTo)
        {
            string subject = "";
            string mimeParseError = null;
            string toAddress = string.Join(", ", message.Recipients);
            MimeMetadata mimeMetadata = new MimeMetadata();
            string bodyText = "";

            byte[] data;
            using (Stream messageData = await message.GetData())
            {
                data = new byte[messageData.Length];
                await messageData.ReadAsync(data, 0, data.Length);

                bool foundHeaders = false;
                bool foundSeparator = false;
                using (StreamReader dataReader = new StreamReader(new MemoryStream(data)))
                {
                    while (!dataReader.EndOfStream)
                    {
                        if (dataReader.ReadLine().Length != 0)
                        {
                            foundHeaders = true;
                        }
                        else
                        {
                            foundSeparator = true;
                            break;
                        }
                    }
                }

                if (!foundHeaders || !foundSeparator)
                {
                    mimeParseError = "Malformed MIME message. No headers found";
                    // If MIME parsing fails, use the complete message as body text
                    bodyText = Encoding.UTF8.GetString(data);
                }
                else
                {
                    messageData.Seek(0, SeekOrigin.Begin);
                    try
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromSeconds(30));
                        MimeMessage mime = await MimeMessage.LoadAsync(messageData, true, cts.Token).ConfigureAwait(false);
                        subject = mime.Subject;

                        // Extract MIME metadata
                        mimeMetadata = ExtractMimeMetadata(mime);
                        
                        // Extract body text
                        bodyText = ExtractBodyText(mime);
                    }
                    catch (OperationCanceledException e)
                    {
                        mimeParseError = e.Message;
                        bodyText = Encoding.UTF8.GetString(data);
                    }
                    catch (FormatException e)
                    {
                        mimeParseError = e.Message;
                        bodyText = Encoding.UTF8.GetString(data);
                    }
                }
            }

            DbModel.Message result = new DbModel.Message
            {
                Id = Guid.NewGuid(),
                From = PunyCodeReplacer.DecodePunycode(message.From),
                To = PunyCodeReplacer.DecodePunycode(toAddress),
                DeliveredTo = PunyCodeReplacer.DecodePunycode(string.Join(", ", deliveredTo)),
                ReceivedDate = DateTime.Now,
                Subject = PunyCodeReplacer.DecodePunycode(subject),
                Data = data,
                MimeParseError = mimeParseError,
                AttachmentCount = 0,
                SecureConnection = message.SecureConnection,
                SessionEncoding = message.EightBitTransport ? Encoding.UTF8.WebName : Encoding.Latin1.WebName,
                HasBareLineFeed = message.HasBareLineFeed,
                MimeMetadata = JsonSerializer.Serialize(mimeMetadata),
                BodyText = bodyText
            };

            var parts = new Message(result).Parts;
            foreach (var part in parts)
            {
                result.AttachmentCount += CountAttachments(part);
            }

            return result;
        }

        private int CountAttachments(MessageEntitySummary part)
        {
            return part.Attachments.Count + part.ChildParts.Sum(CountAttachments);
        }

        private MimeMetadata ExtractMimeMetadata(MimeMessage mime)
        {
            var metadata = new MimeMetadata();

            try
            {
                // Extract CC recipients
                if (mime.Cc != null)
                {
                    foreach (var cc in mime.Cc)
                    {
                        metadata.CcRecipients.Add(cc.ToString());
                    }
                }

                // Extract attachment filenames and body content info
                ExtractPartMetadata(mime.Body, metadata);

                // Set content type
                metadata.ContentType = mime.Body?.ContentType?.MimeType ?? "";

            }
            catch (Exception)
            {
                // If extraction fails, return basic metadata
            }

            return metadata;
        }

        private void ExtractPartMetadata(MimeEntity entity, MimeMetadata metadata)
        {
            if (entity == null) return;

            metadata.PartCount++;

            // Check for attachments
            if (entity.IsAttachment)
            {
                var filename = entity.ContentDisposition?.FileName ?? entity.ContentType?.Name;
                if (!string.IsNullOrEmpty(filename))
                {
                    metadata.AttachmentFilenames.Add(filename);
                }
            }

            // Check for body content types
            if (entity is TextPart textPart)
            {
                if (textPart.IsPlain)
                {
                    metadata.HasTextBody = true;
                }
                else if (textPart.IsHtml)
                {
                    metadata.HasHtmlBody = true;
                }
            }
            else if (entity is Multipart multipart)
            {
                foreach (var part in multipart)
                {
                    ExtractPartMetadata(part, metadata);
                }
            }
        }

        private string ExtractBodyText(MimeMessage mime)
        {
            try
            {
                var bodyText = new StringBuilder();

                // Get text parts
                var textPart = mime.TextBody;
                if (!string.IsNullOrEmpty(textPart))
                {
                    bodyText.AppendLine(textPart);
                }

                // Get HTML parts and convert to text
                var htmlPart = mime.HtmlBody;
                if (!string.IsNullOrEmpty(htmlPart))
                {
                    try
                    {
                        var doc = new HtmlDocument();
                        doc.LoadHtml(htmlPart);
                        var plainText = doc.DocumentNode.InnerText;
                        bodyText.AppendLine(plainText);
                    }
                    catch
                    {
                        // If HTML parsing fails, use raw HTML
                        bodyText.AppendLine(htmlPart);
                    }
                }

                return bodyText.ToString();
            }
            catch
            {
                return "";
            }
        }
    }
}