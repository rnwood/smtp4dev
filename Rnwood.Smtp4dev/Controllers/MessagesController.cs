using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Message = Rnwood.Smtp4dev.ApiModel.Message;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private readonly Smtp4devDbContext _dbContext;
        private readonly MessagesHub _messagesHub;

        public MessagesController(Smtp4devDbContext dbContext, MessagesHub messagesHub)
        {
            _dbContext = dbContext;
            _messagesHub = messagesHub;
        }

        [HttpGet]
        public IEnumerable<MessageSummary> GetSummaries()
        {
            return _dbContext.Messages.Select(m => new MessageSummary(m));
        }

        [HttpGet("{id}")]
        public Message GetMessage(Guid id)
        {
            var result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);
            return new Message(result);
        }

        [HttpGet("{id}/source")]
        public FileStreamResult DownloadMessage(Guid id)
        {
            var result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);
            return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822")
            {
                FileDownloadName = $"{id}.eml"
            };
        }

        [HttpGet("{id}/part/{cid}/content")]
        public FileStreamResult GetPartContent(Guid id, string cid)
        {
            var result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);

            return Message.GetPartContent(result, cid);
        }

        [HttpGet("{id}/part/{cid}/source")]
        public string GetPartSource(Guid id, string cid)
        {
            var result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);

            return Message.GetPartSource(result, cid);
        }

        [HttpGet("{id}/html")]
        public string GetMessageHtml(Guid id)
        {
            var result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);

            var html = Message.GetHtml(result);

            if (html == null) html = "<pre>" + HtmlDocument.HtmlEncode(Message.GetText(result)) + "</pre>";

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imageElements = doc.DocumentNode.SelectNodes("//img[starts-with(@src, 'cid:')]");

            if (imageElements != null)
                foreach (var imageElement in imageElements)
                {
                    var cid = imageElement.Attributes["src"].Value
                        .Replace("cid:", "", StringComparison.OrdinalIgnoreCase);
                    imageElement.Attributes["src"].Value = $"/api/Messages/{id.ToString()}/part/{cid}/content";
                }

            return doc.DocumentNode.OuterHtml;
        }

        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            _dbContext.Messages.RemoveRange(_dbContext.Messages.Where(m => m.Id == id));
            _dbContext.SaveChanges();

            await _messagesHub.OnMessagesChanged();
        }

        [HttpDelete("*")]
        public async Task DeleteAll()
        {
            _dbContext.Messages.RemoveRange(_dbContext.Messages);
            _dbContext.SaveChanges();

            await _messagesHub.OnMessagesChanged();
        }
    }
}