using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.Controllers.API.Model;
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
        private ISmtp4devServer _messageStore;

        public MessagesController(ISmtp4devServer messgeStore)
        {
            _messageStore = messgeStore;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<Message> Get()
        {
            return _messageStore.Messages.Select(m => new Message(m));
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(Guid id)
        {
        }
    }
}