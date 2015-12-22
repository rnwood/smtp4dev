using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.Controllers.API.DTO;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Rnwood.Smtp4dev.Controllers.API
{
    [Route("api/message")]
    public class MessagesController : Controller
    {
        private IMessageStore _messageStore;

        public MessagesController(IMessageStore messageStore)
        {
            _messageStore = messageStore;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<Message> Get()
        {
            return _messageStore.Messages.Select(m => new Message(m));
        }
    }
}