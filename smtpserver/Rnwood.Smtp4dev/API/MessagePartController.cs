using Microsoft.AspNet.Mvc;
using MimeKit;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

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
        public Smtp4dev.API.DTO.MessagePart[] Get(Guid messageId)
        {
            var message = _messageStore.Messages.FirstOrDefault(m => m.Id == messageId);

            if (message == null)
            {
                throw new HttpResponseException(System.Net.HttpStatusCode.NotFound);
            }

            try
            {
                MimeMessage mimeMessage = MimeMessage.Load(message.GetData());
                return mimeMessage.BodyParts.Concat(mimeMessage.Attachments).Select((p, i) => new DTO.MessagePart(i, p)).ToArray();
            }
            catch (FormatException e)
            {
                return new[] { new DTO.MessagePart(0, e.Message) };
            }
        }
    }
}