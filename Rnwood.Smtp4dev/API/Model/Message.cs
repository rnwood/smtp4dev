using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Controllers.API.Model
{
    public class Message
    {
        private IMessage _message;

        public Message(IMessage message)
        {
            _message = message;
        }

        public string From { get { return _message.From; } }
    }
}