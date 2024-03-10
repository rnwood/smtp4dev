using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.ApiModel;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Message = Rnwood.Smtp4dev.DbModel.Message;
using Rnwood.Smtp4dev.Server;
using MimeKit;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilterAttribute]
    public class MessagesController : Controller
    {
        public MessagesController(IMessagesRepository messagesRepository, ISmtp4devServer server)
        {
            this.messagesRepository = messagesRepository;
            this.server = server;
        }

        private const int CACHE_DURATION = 31556926;
        private readonly IMessagesRepository messagesRepository;
        private readonly ISmtp4devServer server;

        [HttpGet]
        public ApiModel.PagedResult<MessageSummary> GetSummaries(string sortColumn = "receivedDate", bool sortIsDescending = true, int page = 1,
            int pageSize = 5)
        {
            return messagesRepository.GetMessages(false).Include(m => m.Relays)
                .OrderBy(sortColumn + (sortIsDescending ? " DESC" : ""))
                .Select(m => new MessageSummary(m))
                .GetPaged(page, pageSize);
        }

        private async Task<Message> GetDbMessage(Guid id, bool tracked)
        {
            return (await this.messagesRepository.TryGetMessageById(id, tracked)) ??
                   throw new FileNotFoundException($"Message with id {id} was not found.");
        }

        [HttpGet("{id}")]
        public async Task<ApiModel.Message> GetMessage(Guid id)
        {
            return new ApiModel.Message(await GetDbMessage(id, false));
        }

        [HttpPost("{id}")]
        public Task MarkMessageRead(Guid id)
        {
            return messagesRepository.MarkMessageRead(id);
        }

        [HttpPost("markAllRead")]
        public Task MarkAllRead()
        {
            return messagesRepository.MarkAllMessagesRead();
        }

        [HttpGet("{id}/download")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<FileStreamResult> DownloadMessage(Guid id)
        {
            Message result = await GetDbMessage(id, false);
            return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822") { FileDownloadName = $"{id}.eml" };
        }

        [HttpPost("{id}/relay")]
        public async Task<IActionResult> RelayMessage(Guid id, [FromBody] MessageRelayOptions options)
        {
            var message = await GetDbMessage(id, true);
            var relayResult = server.TryRelayMessage(message,
                options?.OverrideRecipientAddresses?.Length > 0
                    ? options?.OverrideRecipientAddresses.Select(a => MailboxAddress.Parse(a)).ToArray()
                    : null);

            if (relayResult.Exceptions.Any())
            {
                var relayErrorSummary = string.Join(". ", relayResult.Exceptions.Select(e => e.Key.Address + ": " + e.Value.Message));
                return Problem("Failed to relay to recipients: " + relayErrorSummary);
            }

            if (relayResult.WasRelayed)
            {
                foreach (var relay in relayResult.RelayRecipients)
                {
                    message.AddRelay(new MessageRelay { SendDate = relay.RelayDate, To = relay.Email });
                }

                messagesRepository.DbContext.SaveChanges();
            }

            return Ok();
        }

        [HttpGet("{id}/part/{partid}/content")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<FileStreamResult> GetPartContent(Guid id, string partid)
        {
            return ApiModel.Message.GetPartContent(await GetMessage(id), partid);
        }

        [HttpGet("{id}/part/{partid}/source")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<string> GetPartSource(Guid id, string partid)
        {
            return ApiModel.Message.GetPartContentAsText(await GetMessage(id), partid);
        }

        [HttpGet("{id}/part/{partid}/raw")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<string> GetPartSourceRaw(Guid id, string partid)
        {
            return ApiModel.Message.GetPartSource(await GetMessage(id), partid);
        }

        [HttpGet("{id}/raw")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<string> GetMessageSourceRaw(Guid id)
        {
            ApiModel.Message message = await GetMessage(id);
            var encoding = message.MimeMessage?.Body?.ContentType.CharsetEncoding ?? ApiModel.Message.GetSessionEncodingOrAssumed(message);
            return encoding.GetString(message.Data);
        }

        [HttpGet("{id}/source")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<string> GetMessageSource(Guid id)
        {
            ApiModel.Message message = await GetMessage(id);

            return message.MimeMessage?.HtmlBody ?? message.MimeMessage?.TextBody ?? "";
        }

        [HttpGet("{id}/plaintext")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<IActionResult> GetMessagePlainText(Guid id)
        {
            ApiModel.Message message = await GetMessage(id);

            if (message.MimeMessage == null)
            {
                return Content(ApiModel.Message.GetSessionEncodingOrAssumed(message).GetString(message.Data));
            }

            string plaintext = message.MimeMessage?.HtmlBody;
            if (plaintext == null)
            {
                return NotFound("MIME message does not have a plain text body");
            }

            return Content(plaintext);
        }

        [HttpGet("{id}/html")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<IActionResult> GetMessageHtml(Guid id)
        {
            ApiModel.Message message = await GetMessage(id);

            string html = message.MimeMessage?.HtmlBody;

            if (html == null)
            {
                return NotFound("Message does not have a HTML body");
            }
            
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);


            HtmlNodeCollection imageElements = doc.DocumentNode.SelectNodes("//img[starts-with(@src, 'cid:')]");

            if (imageElements != null)
            {
                foreach (HtmlNode imageElement in imageElements)
                {
                    string cid = imageElement.Attributes["src"].Value.Replace("cid:", "", StringComparison.OrdinalIgnoreCase);

                    var part = message.Parts.Flatten(p => p.ChildParts).SingleOrDefault(p => p.ContentId == cid);

                    imageElement.Attributes["src"].Value = $"api/Messages/{id.ToString()}/part/{part?.Id ?? "notfound"}/content";
                }
            }

            return Content(doc.DocumentNode.OuterHtml);
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