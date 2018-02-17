using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using HtmlAgilityPack;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        public MessagesController(Smtp4devDbContext dbContext, MessagesHub messagesHub)
        {
            _dbContext = dbContext;
            _messagesHub = messagesHub;
        }


        private Smtp4devDbContext _dbContext;
        private MessagesHub _messagesHub;

        private MessageData GetMessageData(Guid MessageId)
        {
            return _dbContext.MessageDatas.FirstOrDefault(b => b.MessageId == MessageId);
        }

        private ApiModel.Message GetApiMessag(DbModel.Message dbMessage)
        {
            return new ApiModel.Message(dbMessage, _dbContext.MessageDatas.FirstOrDefault(b => b.MessageId == dbMessage.Id));
        }

        [HttpGet]
        public IEnumerable<ApiModel.MessageSummary> GetSummaries()
        {
            return _dbContext.Messages.Select(m => new ApiModel.MessageSummary(m));
        }

        [HttpGet("{id}")]
        public ApiModel.Message GetMessage(Guid id)
        {
            Message result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);
            return result == null ? null : GetApiMessag(result);
        }

        [HttpGet("last/to/{to}")]
        public ApiModel.MessageSummary GetLastMessageTo(string to)
        {
            Message result = _dbContext.Messages.OrderByDescending(b => b.ReceivedDate).FirstOrDefault(b => b.To == to);
            return result == null ? null : new ApiModel.MessageSummary(result);
        }

        [HttpGet("last")]
        public ApiModel.MessageSummary GetLastMessage()
        {
            Message result = _dbContext.Messages.OrderByDescending(b => b.ReceivedDate).FirstOrDefault();
            return result == null ? null : new ApiModel.MessageSummary(result);
        }

        [HttpGet("{id}/part/{cid}/content")]
        public FileStreamResult GetPartContent(Guid id, string cid)
        {
            MessageData result = _dbContext.MessageDatas.FirstOrDefault(m => m.MessageId == id);

            return ApiModel.Message.GetPartContent(result, cid);
        }

        [HttpGet("{id}/regex/{regex}/value")]
        public string GetRegexFromMessageHtml(Guid id, string regex)
        {
            MessageData result = _dbContext.MessageDatas.FirstOrDefault(m => m.MessageId == id);

            string html = ApiModel.Message.GetHtml(result);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            Regex r = new Regex(regex);
            Match match = r.Match(html);
            if (match.Success)
            {
                return match.Value;
            }
            return string.Empty;
        }

        [HttpGet("{id}/html")]
        public string GetMessageHtml(Guid id)
        {
            MessageData result = _dbContext.MessageDatas.FirstOrDefault(m => m.MessageId == id);

            string html = ApiModel.Message.GetHtml(result);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection imageElements = doc.DocumentNode.SelectNodes("//img[starts-with(@src, 'cid:')]");

            if (imageElements != null)
            {
                foreach (HtmlNode imageElement in imageElements)
                {
                    string cid = imageElement.Attributes["src"].Value.Replace("cid:", "", StringComparison.OrdinalIgnoreCase);
                    imageElement.Attributes["src"].Value = $"/api/Messages/{id.ToString()}/part/{cid}/content";
                }
            }

            return doc.DocumentNode.OuterHtml;
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
