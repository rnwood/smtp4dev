using MimeKit;
using Rnwood.Smtp4dev.Model;
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
        private ISmtp4devMessage _message;

        public Message(ISmtp4devMessage message)
        {
            _message = message;

            using (Stream messageData = message.GetData())
            {
                try
                {
                    MimeMessage mimeMessage = MimeMessage.Load(messageData);
                    subject = mimeMessage.Subject;
                }
                catch (FormatException)
                {
                    subject = "";
                }
            }
        }

        public DateTime receivedDate { get { return _message.ReceivedDate; } }

        public string from { get { return _message.From; } }

        public string[] to { get { return _message.To; } }

        public string subject { get; private set; }

        public Guid id { get { return _message.Id; } }
    }
}