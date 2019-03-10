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
        public MessagesController(Smtp4devDbContext dbContext, Smtp4devServer server)
        {
            this.dbContext = dbContext;
            this.server = server;
        }

        private Smtp4devDbContext dbContext;
        private Smtp4devServer server;

        [HttpGet]

        public IEnumerable<ApiModel.MessageSummary> GetSummaries(string sortColumn = "receivedDate", bool sortIsDescending = true)
        {
            return dbContext.Messages
            .OrderBy(sortColumn + (sortIsDescending ? " DESC" : ""))
            .Select(m => new ApiModel.MessageSummary(m));
        }

        private DbModel.Message GetDbMessage(Guid id)
        {
            return dbContext.Messages.FirstOrDefault(m => m.Id == id) ??
                throw new FileNotFoundException($"Message with id {id} was not found.");
        }

        [HttpGet("{id}")]

        public ApiModel.Message GetMessage(Guid id)
        {
            Message result = GetDbMessage(id);

            return new ApiModel.Message(result);
        }

        [HttpGet("{id}/source")]

        public FileStreamResult DownloadMessage(Guid id)
        {
            Message result = GetDbMessage(id);
            return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822") { FileDownloadName = $"{id}.eml" };
        }

        [HttpGet("{id}/part/{cid}/content")]

        public FileStreamResult GetPartContent(Guid id, string cid)
        {
            Message result = GetDbMessage(id);

            return ApiModel.Message.GetPartContent(result, cid);
        }

        [HttpGet("{id}/part/{cid}/source")]

        public string GetPartSource(Guid id, string cid)
        {
            Message result = GetDbMessage(id);

            return ApiModel.Message.GetPartContentAsText(result, cid);
        }

        [HttpGet("{id}/part/{cid}/raw")]

        public string GetPartSourceRaw(Guid id, string cid)
        {
            Message result = GetDbMessage(id);
            return ApiModel.Message.GetPartSource(result, cid);
        }

        [HttpGet("{id}/html")]

        public string GetMessageHtml(Guid id)
        {
            Message result = GetDbMessage(id);

            string html = ApiModel.Message.GetHtml(result);

            if (html == null)
            {
                html = "<pre>" + HtmlAgilityPack.HtmlDocument.HtmlEncode(ApiModel.Message.GetText(result)) + "</pre>";
            }

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

        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            await server.DeleteMessage(id);
        }

        [HttpDelete("*")]
        public async Task DeleteAll()
        {
            await server.DeleteAllMessages();
         }

    }
}
