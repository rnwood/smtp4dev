using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Serilog;

namespace Rnwood.Smtp4dev.Server
{
    public class MimeMetadataPopulationService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Serilog.ILogger _logger = Log.ForContext<MimeMetadataPopulationService>();

        public MimeMetadataPopulationService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task PopulateExistingMessagesAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Smtp4devDbContext>();

            try
            {
                // Find messages without MIME metadata
                var messagesWithoutMetadata = await dbContext.Messages
                    .Where(m => string.IsNullOrEmpty(m.MimeMetadata) || string.IsNullOrEmpty(m.BodyText))
                    .ToListAsync();

                if (!messagesWithoutMetadata.Any())
                {
                    _logger.Information("All messages already have MIME metadata populated");
                    return;
                }

                _logger.Information("Populating MIME metadata for {count} existing messages", messagesWithoutMetadata.Count);

                int processed = 0;
                int batchSize = 50; // Process in batches to avoid memory issues

                foreach (var batch in messagesWithoutMetadata.Chunk(batchSize))
                {
                    foreach (var message in batch)
                    {
                        try
                        {
                            var (mimeMetadata, bodyText) = ExtractMimeDataFromMessage(message);
                            message.MimeMetadata = JsonSerializer.Serialize(mimeMetadata);
                            message.BodyText = bodyText;
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to extract MIME metadata for message {messageId}", message.Id);
                            // Set fallback values
                            message.MimeMetadata = JsonSerializer.Serialize(new MimeMetadata());
                            message.BodyText = Encoding.UTF8.GetString(message.Data);
                        }
                    }

                    await dbContext.SaveChangesAsync();
                    processed += batch.Length;
                    _logger.Information("Processed {processed}/{total} messages", processed, messagesWithoutMetadata.Count);
                }

                _logger.Information("Successfully populated MIME metadata for all existing messages");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to populate MIME metadata for existing messages");
                throw;
            }
        }

        private (MimeMetadata metadata, string bodyText) ExtractMimeDataFromMessage(Message message)
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

                // Extract CC recipients
                if (mimeMessage.Cc != null)
                {
                    foreach (var cc in mimeMessage.Cc)
                    {
                        metadata.CcRecipients.Add(cc.ToString());
                    }
                }

                // Extract attachment filenames and body content info
                ExtractPartMetadata(mimeMessage.Body, metadata);

                // Set content type
                metadata.ContentType = mimeMessage.Body?.ContentType?.MimeType ?? "";

                // Extract body text
                bodyText = ExtractBodyText(mimeMessage);
            }
            catch (Exception)
            {
                // If MIME parsing fails, use the complete message as body text
                bodyText = Encoding.UTF8.GetString(message.Data);
            }

            return (metadata, bodyText);
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