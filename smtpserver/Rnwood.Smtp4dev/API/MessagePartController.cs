using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Rnwood.Smtp4dev.API
{
    [Route("api/message")]
    public class MessagePartController : Controller
    {
        private IMessageStore _messageStore;

        public MessagePartController(IMessageStore messageStore)
        {
            _messageStore = messageStore;
        }

        [HttpGet("{messageid}/part")]
        public IActionResult Get(Guid messageId)
        {
            var message = _messageStore.Messages.FirstOrDefault(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            MimeMessage mimeMessage = MimeMessage.Load(message.GetData());
            return Ok(mimeMessage.BodyParts.Concat(mimeMessage.Attachments).Select((p, i) => new DTO.MessagePart(i, p)).ToArray());
        }
    }
}