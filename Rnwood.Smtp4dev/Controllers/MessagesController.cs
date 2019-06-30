using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using HtmlAgilityPack;

using Microsoft.AspNetCore.Mvc;

using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using System.Linq.Dynamic.Core;

using Message = Rnwood.Smtp4dev.DbModel.Message;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[UseEtagFilterAttribute]
	public class MessagesController : Controller
	{
		public MessagesController(IMessagesRepository messagesRepository)
		{
			this.messagesRepository = messagesRepository;
		}

		private IMessagesRepository messagesRepository;

		[HttpGet]

		public IEnumerable<ApiModel.MessageSummary> GetSummaries(string sortColumn = "receivedDate", bool sortIsDescending = true)
		{
			return messagesRepository.GetMessages()
			.OrderBy(sortColumn + (sortIsDescending ? " DESC" : ""))
			.Select(m => new ApiModel.MessageSummary(m));
		}

		private DbModel.Message GetDbMessage(Guid id)
		{
			return messagesRepository.GetMessages().FirstOrDefault(m => m.Id == id) ??
				throw new FileNotFoundException($"Message with id {id} was not found.");
		}

		[HttpGet("{id}")]
		public ApiModel.Message GetMessage(Guid id)
		{
			var result = new ApiModel.Message(GetDbMessage(id));
			return result;
		}

		[HttpPost("{id}")]
		public Task MarkMessageRead(Guid id)
		{
			return messagesRepository.MarkMessageRead(id);
		}

		[HttpGet("{id}/source")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]

		public FileStreamResult DownloadMessage(Guid id)
		{
			Message result = GetDbMessage(id);
			return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822") { FileDownloadName = $"{id}.eml" };
		}

		[HttpGet("{id}/part/{partid}/content")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
		public FileStreamResult GetPartContent(Guid id, string partid)
		{
			return ApiModel.Message.GetPartContent(GetMessage(id), partid);
		}

		[HttpGet("{id}/part/{partid}/source")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
		public string GetPartSource(Guid id, string partid)
		{
			return ApiModel.Message.GetPartContentAsText(GetMessage(id), partid);
		}

		[HttpGet("{id}/part/{partid}/raw")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
		public string GetPartSourceRaw(Guid id, string partid)
		{
			return ApiModel.Message.GetPartSource(GetMessage(id), partid);
		}

		[HttpGet("{id}/html")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
		public string GetMessageHtml(Guid id)
		{
			ApiModel.Message message = GetMessage(id);

			string html = message.MimeMessage?.HtmlBody;

			if (html == null)
			{
				html = "<pre>" + HtmlAgilityPack.HtmlDocument.HtmlEncode(message.MimeMessage?.TextBody) + "</pre>";
			}

	
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);

			

			HtmlNodeCollection imageElements = doc.DocumentNode.SelectNodes("//img[starts-with(@src, 'cid:')]");

			if (imageElements != null)
			{
				foreach (HtmlNode imageElement in imageElements)
				{
					string cid = imageElement.Attributes["src"].Value.Replace("cid:", "", StringComparison.OrdinalIgnoreCase);

					var part = message.Parts.Flatten(p => p.ChildParts).FirstOrDefault(p => p.ContentId == cid);

					imageElement.Attributes["src"].Value = $"api/Messages/{id.ToString()}/part/{part?.Id ?? "notfound"}/content";
				}
			}

			return doc.DocumentNode.OuterHtml;
		}

		[HttpDelete("{id}")]
		public async Task Delete(Guid id)
		{
			await messagesRepository.DeleteMessage(id);
		}

		[HttpDelete("*")]
		public async Task DeleteAll()
		{
			await messagesRepository.DeleteAllMessages();
		}

	}
}
