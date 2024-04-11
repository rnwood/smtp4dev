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

        /// <summary>
        /// Returns all new messages since the provided message ID. Returns only the summary without message content.
        /// </summary>
        /// <param name="lastSeenMessageId">If not specified all recently received messages will be returned up to the page limit.</param>
        /// <param name="pageSize">Max number of messages to retrieve. The most recent X are returned.</param>
        /// <returns></returns>
        [HttpGet("new")]
        public MessageSummary[] GetNewSummaries(Guid? lastSeenMessageId, int pageSize = 50)
        {
            return messagesRepository.GetMessages(true)
                .OrderByDescending(m => m.ReceivedDate)
                .ThenByDescending(m => m.Id)
                .AsEnumerable()
                .TakeWhile(m => m.Id != lastSeenMessageId)
                .Select(m => new MessageSummary(m))
                .Take(pageSize)
                .ToArray();
        }

        /// <summary>
        /// Returns a list of message summaries including basic details but not the content.
        /// </summary>
        /// <param name="searchTerms">Case insensitive term to search for in subject,from,to</param>
        /// <param name="sortColumn"></param>
        /// <param name="sortIsDescending">True if sort should be descending</param>
        /// <param name="page">Page number to retrieve</param>
        /// <param name="pageSize">Max number of items to retrieve</param>
        /// <returns></returns>
        [HttpGet]
        public ApiModel.PagedResult<MessageSummary> GetSummaries(string searchTerms, string sortColumn = "receivedDate",
            bool sortIsDescending = true, int page = 1,
            int pageSize = 5)
        {
            IEnumerable<DbModel.Message> query = messagesRepository.GetMessages(true)
                .Include(m => m.Relays)
                .OrderBy(sortColumn + (sortIsDescending ? " DESC" : ""));

            if (!string.IsNullOrEmpty(searchTerms))
            {

                query = query.ToList().Where(m => m.Subject.Contains(searchTerms, StringComparison.CurrentCultureIgnoreCase)
                                         || m.From.Contains(searchTerms, StringComparison.CurrentCultureIgnoreCase)
                                         || m.To.Contains(searchTerms, StringComparison.CurrentCultureIgnoreCase)
                );
            }

            return query
                .Select(m => new MessageSummary(m))
                .GetPaged(page, pageSize);
        }

        private async Task<Message> GetDbMessage(Guid id, bool tracked)
        {
            return (await this.messagesRepository.TryGetMessageById(id, tracked)) ??
                   throw new FileNotFoundException($"Message with id {id} was not found.");
        }

        /// <summary>
        /// Returns the full message details for a message.
        /// </summary>
        /// <param name="id">The message ID to get.</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ApiModel.Message> GetMessage(Guid id)
        {
            return new ApiModel.Message(await GetDbMessage(id, false));
        }

        /// <summary>
        /// Marks a single message as read
        /// </summary>
        /// <param name="id">The ID of the message to mark read.</param>
        /// <returns></returns>
        [HttpPost("{id}/markRead")]
        public Task MarkMessageRead(Guid id)
        {
            return messagesRepository.MarkMessageRead(id);
        }

        /// <summary>
        /// Marks all messages as read.
        /// </summary>
        /// <returns></returns>
        [HttpPost("markAllRead")]
        public Task MarkAllRead()
        {
            return messagesRepository.MarkAllMessagesRead();
        }

        /// <summary>
        /// Downloads message in .eml (message/rfc822) format.
        /// </summary>
        /// <param name="id">The ID of the message to download</param>
        /// <returns></returns>
        [HttpGet("{id}/download")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<FileStreamResult> DownloadMessage(Guid id)
        {
            Message result = await GetDbMessage(id, false);
            return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822") { FileDownloadName = $"{id}.eml" };
        }
        /// <summary>
        /// Relays the specified message either to the original recipients or to those specified.
        /// </summary>
        /// <param name="id">The ID of the message to relay.</param>
        /// <param name="options"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the plain text body of the specified message if one exists.
        /// </summary>
        /// <param name="id">The ID of the message to get body of.</param>
        /// <returns></returns>
        [HttpGet("{id}/plaintext")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<ActionResult<string>> GetMessagePlainText(Guid id)
        {
            ApiModel.Message message = await GetMessage(id);

            if (message.MimeMessage == null)
            {
                return Content(ApiModel.Message.GetSessionEncodingOrAssumed(message).GetString(message.Data));
            }

            string plaintext = message.MimeMessage?.TextBody;
            if (plaintext == null)
            {
                return NotFound("MIME message does not have a plain text body");
            }

            return plaintext;
        }

        /// <summary>
        /// Returns the HTML text body of the specified message if one exists.
        /// </summary>
        /// <param name="id">The ID of the message to get body of.</param>
        /// <returns></returns>

        [HttpGet("{id}/html")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<ActionResult<string>> GetMessageHtml(Guid id)
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