using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    public class MimeProcessingService
    {
        public (MimeMetadata metadata, string bodyText) ExtractMimeDataFromMessage(Message message)
        {
            var metadata = new MimeMetadata();
            string bodyText = "";

            if (!string.IsNullOrEmpty(message.MimeParseError) || message.Data == null)
            {
                // If there's a parse error, use fallback
                bodyText = message.Data != null ? Encoding.UTF8.GetString(message.Data) : "";
                return (metadata, bodyText);
            }

            try
            {
                using var stream = new MemoryStream(message.Data);
                var mimeMessage = MimeMessage.Load(stream);

                metadata = ExtractMimeMetadata(mimeMessage);
                bodyText = ExtractBodyText(mimeMessage);
            }
            catch (Exception)
            {
                // If MIME parsing fails, use the complete message as body text
                bodyText = Encoding.UTF8.GetString(message.Data);
            }

            return (metadata, bodyText);
        }

        public MimeMetadata ExtractMimeMetadata(MimeMessage mime)
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
                ExtractPartMetadata(mime.Body, metadata, new HashSet<string>());

                // Set content type
                metadata.ContentType = mime.Body?.ContentType?.MimeType ?? "";
            }
            catch (Exception)
            {
                // If extraction fails, return basic metadata
            }

            return metadata;
        }

        public void ExtractPartMetadata(MimeEntity entity, MimeMetadata metadata, HashSet<string> seenContentIds)
        {
            if (entity == null) return;

            metadata.PartCount++;

            if (!string.IsNullOrEmpty(entity.ContentId) && seenContentIds.Contains(entity.ContentId))
            {
                metadata.HasDuplicatedContentIds = true;
            } else
            {
                seenContentIds.Add(entity.ContentId);
            }

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
                    ExtractPartMetadata(part, metadata, seenContentIds);
                }
            }
        }

        public string ExtractBodyText(MimeMessage mime)
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