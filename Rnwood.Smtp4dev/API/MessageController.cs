using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using MimeKit;
using Rnwood.Smtp4dev.API;
using Rnwood.Smtp4dev.API.DTO;
using Rnwood.Smtp4dev.Controllers.API.DTO;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Rnwood.Smtp4dev.API
{
    [Route("api/message")]
    public class MessagesController : Controller
    {
        private IMessageStore _messageStore;

        public MessagesController(IMessageStore messageStore)
        {
            _messageStore = messageStore;
        }

        [HttpGet("{searchTerm?}")]
        public IEnumerable<Message> Get(string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                return _messageStore.SearchMessages(searchTerm).Select(m => new Message(m));
            }
            else {
                return _messageStore.Messages.Select(m => new Message(m));
            }
        }

        [HttpDelete("{id?}")]
        public IActionResult Delete(Guid? id)
        {
            if (id.HasValue)
            {
                ISmtp4devMessage message = _messageStore.Messages.FirstOrDefault(m => m.Id == id);

                if (message != null)
                {
                    _messageStore.DeleteMessage(message);
                }
            }
            else
            {
                _messageStore.DeleteAllMessages();
            }

            return new NoContentResult();
        }

        [HttpGet("events")]
        public async Task Events()
        {
            HttpContext.Response.ContentType = "text/event-stream";

            AutoResetEvent messagesChangedEvent = new AutoResetEvent(false);

            _messageStore.MessageAdded += (s, ea) =>
            {
                messagesChangedEvent.Set();
            };

            _messageStore.MessageDeleted += (s, ea) =>
            {
                messagesChangedEvent.Set();
            };

            while (true)
            {
                await messagesChangedEvent.WaitOneAsync();
                await HttpContext.Response.WriteAsync("event: messageschanged\ndata: messages changed!\n\n");
            }
        }
    }
}