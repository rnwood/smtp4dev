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
using Rnwood.Smtp4dev;
using System.Web.Http;

namespace Rnwood.Smtp4dev.ApiModel
{
	public class Message : ICacheByKey
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

			Parts = new List<ApiModel.MessageEntitySummary>();

			if (dbMessage.MimeParseError != null)
			{
				MimeParseError = dbMessage.MimeParseError;
				Headers = new List<Header>();
				Parts = new List<MessageEntitySummary>();
			}
			else
			{
				using (MemoryStream stream = new MemoryStream(dbMessage.Data))
				{
					MimeMessage = MimeMessage.Load(stream);

					if (MimeMessage.From != null)
					{
						From = MimeMessage.From.ToString();
					}

					List<string> recipients = new List<string>(dbMessage.To.Split(",")
						.Select(r => r.Trim())
						.Where(r => !string.IsNullOrEmpty(r)));

					if (MimeMessage.To != null)
					{
						To = string.Join(", ", MimeMessage.To.Select(t => t.ToString()));

						foreach (MailboxAddress to in MimeMessage.To.Where(t => t is MailboxAddress))
						{
							recipients.Remove(to.Address);
						}
					}

					if (MimeMessage.Cc != null)
					{
						Cc = string.Join(", ", MimeMessage.Cc.Select(t => t.ToString()));

						foreach (MailboxAddress cc in MimeMessage.Cc.Where(t => t is MailboxAddress))
						{
							recipients.Remove(cc.Address);
						}
					}

					Bcc = string.Join(", ", recipients);

					Headers = MimeMessage.Headers.Select(h => new Header { Name = h.Field, Value = h.Value }).ToList();
					Parts.Add(HandleMimeEntity(MimeMessage.Body));
				}
			}
		}


		private MessageEntitySummary HandleMimeEntity(MimeEntity entity)
		{
			int index = 0;

			return MimeEntityVisitor.VisitWithResults<MessageEntitySummary>(entity, (e, p) =>
		   {
			   MessageEntitySummary result = new MessageEntitySummary()
			   {
				   MessageId = Id,
				   Id = index.ToString(),
				   ContentId = e.ContentId,
				   Name = e.ContentId + " - " + e.ContentType.MimeType,
				   Headers = e.Headers.Select(h => new Header { Name = h.Field, Value = h.Value }).ToList(),
				   ChildParts = new List<MessageEntitySummary>(),
				   Attachments = new List<AttachmentSummary>(),
				   Size = e.ToString().Length,
				   MimeEntity = e
			   };

			   if (p != null)
			   {
				   p.ChildParts.Add(result);

				   if (e.IsAttachment)
				   {
					   p.Attachments.Add(new AttachmentSummary()
					   {
						   Id = result.Id,
						   ContentId = result.ContentId,
						   FileName = string.IsNullOrEmpty(e.ContentType?.Name)
							   ? e.ContentDisposition?.FileName
							   : e.ContentType.Name,
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
			MimeEntity contentEntity = GetPart(result, cid);

			if (contentEntity is MimePart mimePart)
			{
				return new FileStreamResult(mimePart.Content.Open(), contentEntity.ContentType?.MimeType ?? "application/text")
				{
					FileDownloadName = mimePart.FileName
				};
			}
			else
			{
				MemoryStream outputStream = new MemoryStream();
				contentEntity.WriteTo(outputStream, true);
				outputStream.Seek(0, SeekOrigin.Begin);

				return new FileStreamResult(outputStream, contentEntity.ContentType?.MimeType ?? "application/text");
			}
		}

		internal static string GetPartContentAsText(Message result, string id)
		{
			MimeEntity contentEntity = GetPart(result, id);

			if (contentEntity is MimePart part)
			{
				using (StreamReader reader = new StreamReader(part.Content.Open()))
				{
					return reader.ReadToEnd();
				}
			}
			else
			{
				return contentEntity.ToString();
			}

		}



		internal static string GetPartSource(Message message, string id)
		{
			MimeEntity contentEntity = GetPart(message, id);
			return contentEntity.ToString();
		}

		
		private static MimeEntity GetPart(Message message, string id)
		{
			MessageEntitySummary part = message.Parts.Flatten(p => p.ChildParts).FirstOrDefault(p => p.Id == id);

			if (part == null)
			{
				throw new FileNotFoundException($"Part with id '{id}' in message {message.Id} is not found");
			}

			return part.MimeEntity;
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

		internal MimeMessage MimeMessage { get; set; }

		string ICacheByKey.CacheKey => Id.ToString();
	}
}
