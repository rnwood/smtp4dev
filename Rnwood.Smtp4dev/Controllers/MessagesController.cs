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
using NSwag.Annotations;
using Rnwood.Smtp4dev.Server.Settings;
using Org.BouncyCastle.Cms;

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
        /// <param name="mailboxName">Mailbox name. If not specified, defaults to the mailboxName with name 'Default'</param>
        /// <param name="pageSize">Max number of messages to retrieve. The most recent X are returned.</param>
        /// <returns></returns>
        [HttpGet("new")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(MessageSummary[]), Description = "")]
        public MessageSummary[] GetNewSummaries(Guid? lastSeenMessageId, string mailboxName = MailboxOptions.DEFAULTNAME, int pageSize = 50)
        {
            return messagesRepository.GetMessages(mailboxName, true)
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
        /// <param name="mailboxName">Mailbox name. If not specified, defaults to the mailboxName with name 'Default'</param>
        /// <param name="sortColumn">Property name from response type to sort by</param>
        /// <param name="sortIsDescending">True if sort should be descending</param>
        /// <param name="page">Page number to retrieve</param>
        /// <param name="pageSize">Max number of items to retrieve</param>
        /// <returns></returns>
        [HttpGet]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(ApiModel.PagedResult<MessageSummary>), Description = "")]
        public ApiModel.PagedResult<MessageSummary> GetSummaries(string searchTerms, string mailboxName = MailboxOptions.DEFAULTNAME, string sortColumn = "receivedDate",
            bool sortIsDescending = true, int page = 1,
            int pageSize = 5)
        {
            IEnumerable<DbModel.Message> query = messagesRepository.GetMessages(mailboxName, true)
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
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(ApiModel.Message), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message does not exist")]
        public async Task<ApiModel.Message> GetMessage(Guid id)
        {
            return new ApiModel.Message(await GetDbMessage(id, false));
        }

        /// <summary>
        /// Replies to the message with the specified ID using the configured relay SMTP server.
        /// </summary>
        /// <param name="id">The Id of the message to reply to</param>
        /// <param name="to">List of email addresses separated by commas</param>
        /// <param name="cc">List of email addresses separated by commas</param>
        /// <param name="bcc">List of email addresses separated by commas</param>
        /// <param name="from">Email address</param>
        /// <param name="deliverToAll">True if the message should be delivered to the CC and BCC recipients in addition to the TO recipients. When false, the message is only delivered to the TO recipients, but the message headers will show the specified other recipients.</param>
        /// <param name="subject">The subject of message</param>
        /// <param name="bodyHtml">UTF8 encoded HTML body for the message</param>
        /// <returns></returns>
        [HttpPost("{id}/reply")]
        [OpenApiBodyParameter("text/html")]
        [Consumes("text/html")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message does not exist")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, typeof(void), Description = "If message fails to send.")]
        public async Task<IActionResult> Reply(Guid id, string to, string cc, string bcc, string from, bool deliverToAll, string subject, [FromBody] string bodyHtml)
        {
            var origMessage = new ApiModel.Message(await GetDbMessage(id, false));
            var origMessageId = origMessage.Headers.FirstOrDefault(h => h.Name.Equals("Message-Id", StringComparison.OrdinalIgnoreCase))?.Value ?? "";


            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers["References"] = (
                origMessageId
                + " " +
                origMessage.Headers.FirstOrDefault(h => h.Name.Equals("References"))?.Value ?? "").Trim();

            if (!string.IsNullOrEmpty(origMessageId))
            {
                headers["In-Reply-To"] = origMessageId;
            }

            var toRecips = to?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            var ccRecips = cc?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            var bccRecips = bcc?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            List<string> envelopeRecips = deliverToAll ? [.. toRecips, .. ccRecips, .. bccRecips] : [.. toRecips];

            this.server.Send(headers,
                toRecips,
                ccRecips,
                from, envelopeRecips.Distinct().ToArray(), subject, bodyHtml);

            return Ok();
        }

        /// <summary>
        /// Sends a message via the configured upstream/relay SMTP server.
        /// </summary>
        /// <param name="to">List of email addresses separated by commas</param>
        /// <param name="cc">List of email addresses separated by commas</param>
        /// <param name="bcc">List of email addresses separated by commas</param>
        /// <param name="from">Email address</param>
        /// <param name="deliverToAll">True if the message should be delivered to the CC and BCC recipients in addition to the TO recipients. When false, the message is only delivered to the TO recipients, but the message headers will show the specified other recipients.</param>
        /// <param name="subject">The subject of message</param>
        /// <param name="bodyHtml">UTF8 encoded HTML body for the message</param>
        /// <returns></returns>
        [HttpPost("send")]
        [OpenApiBodyParameter("text/html")]
        [Consumes("text/html")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, typeof(void), Description = "If message fails to send.")]
        public async Task<IActionResult> Send(string to, string cc, string bcc, string from, bool deliverToAll, string subject, [FromBody] string bodyHtml)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
      
            var toRecips = to?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            var ccRecips = cc?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            var bccRecips = bcc?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            List<string> envelopeRecips = deliverToAll ? [.. toRecips, .. ccRecips, .. bccRecips] : [.. toRecips];

            this.server.Send(headers,
                toRecips,
                ccRecips,
                from, envelopeRecips.Distinct().ToArray(), subject, bodyHtml);

            return Ok();
        }

        /// <summary>
        /// Marks a single message as read
        /// </summary>
        /// <param name="id">The ID of the message to mark read.</param>
        /// <returns></returns>
        [HttpPost("{id}/markRead")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "")]
        public Task MarkMessageRead(Guid id)
        {
            return messagesRepository.MarkMessageRead(id);
        }

        /// <summary>
        /// Marks all messages as read.
        /// </summary>
        /// <returns></returns>
        [HttpPost("markAllRead")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "")]
        public Task MarkAllRead(string mailboxName = MailboxOptions.DEFAULTNAME)
        {
            return messagesRepository.MarkAllMessagesRead(mailboxName);
        }

        /// <summary>
        /// Downloads message in .eml (message/rfc822) format.
        /// </summary>
        /// <param name="id">The ID of the message to download</param>
        /// <returns></returns>
        [HttpGet("{id}/download")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(FileStreamResult), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message does not exist")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]

        public async Task<FileStreamResult> DownloadMessage(Guid id)
        {
            Message result = await GetDbMessage(id, false);
            return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822") { FileDownloadName = $"{id}.eml" };
        }
        /// <summary>
        /// Attempt to relay the specified message either to the original recipients or to those specified.
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

        /// <summary>
        /// Returns the MIME part contents for the specified message and part.
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <param name="partid">Part ID</param>
        /// <returns></returns>
        [HttpGet("{id}/part/{partid}/content")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message or part does not exist")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<FileStreamResult> GetPartContent(Guid id, string partid)
        {
            return ApiModel.Message.GetPartContent(await GetMessage(id), partid);
        }

        /// <summary>
        /// Returns the source text of MIME part contents for the specified message and part.
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <param name="partid">Part ID</param>
        /// <returns></returns>
        [HttpGet("{id}/part/{partid}/source")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message or part does not exist")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<string> GetPartSource(Guid id, string partid)
        {
            return ApiModel.Message.GetPartContentAsText(await GetMessage(id), partid);
        }

        /// <summary>
        /// Returns the raw source of MIME part contents for the specified message and part.
        /// </summary>
        /// RAW source is before any content decoding steps like base64.
        /// <param name="id">Message ID</param>
        /// <param name="partid">Part ID</param>
        /// <returns></returns>
        [HttpGet("{id}/part/{partid}/raw")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message or part does not exist")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        public async Task<string> GetPartSourceRaw(Guid id, string partid)
        {
            return ApiModel.Message.GetPartSource(await GetMessage(id), partid);
        }

        /// <summary>
        /// Returns the raw source text of the specified message.
        /// </summary>
        /// RAW source is before any content decoding steps like base64.
        /// <param name="id">Message ID</param>
        /// <returns></returns>
        [HttpGet("{id}/raw")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = CACHE_DURATION)]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message does not exist")]
        public async Task<string> GetMessageSourceRaw(Guid id)
        {
            ApiModel.Message message = await GetMessage(id);
            var encoding = message.MimeMessage?.Body?.ContentType.CharsetEncoding ?? ApiModel.Message.GetSessionEncodingOrAssumed(message);
            return encoding.GetString(message.Data);
        }

        /// <summary>
        /// Returns the source text of the specified message.
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <returns></returns>
        [HttpGet("{id}/source")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message does not exist")]
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
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message or part does not exist")]
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

        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the message or part does not exist")]
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

        /// <summary>
        /// Deletes the specified message.
        /// </summary>
        /// <param name="id">Message ID</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "")]
        public async Task Delete(Guid id)
        {
            await messagesRepository.DeleteMessage(id);
        }


        /// <summary>
        /// Deletes all messages.
        /// </summary>
        /// <returns></returns>
        [HttpDelete("*")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "")]
        public async Task DeleteAll(string mailboxName = MailboxOptions.DEFAULTNAME)
        {
            await messagesRepository.DeleteAllMessages(mailboxName);
        }
    }
}