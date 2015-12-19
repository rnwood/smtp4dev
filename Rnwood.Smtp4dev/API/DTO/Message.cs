using MimeKit;
using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Controllers.API.DTO
{
    public class Message
    {
        private IMessage _message;

        public Message(IMessage message)
        {
            _message = message;

            using (Stream messageData = message.GetData())
            {
                try
                {
                    MimeMessage mimeMessage = MimeMessage.Load(messageData);
                    Subject = mimeMessage.Subject;
                }
                catch (FormatException e)
                {
                    Subject = "";
                }
            }
        }

        public DateTime ReceivedDate { get { return _message.ReceivedDate; } }

        public string From { get { return _message.From; } }

        public string[] To { get { return _message.To; } }

        public string Subject { get; private set; }
    }
}