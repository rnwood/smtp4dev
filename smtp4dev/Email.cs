using System;
using System.IO;
using System.Linq;
using System.Text;
using anmar.SharpMimeTools;
using EricDaugherty.CSES.SmtpServer;

namespace smtp4dev
{
    public class Email
    {
        private DateTime _recieved;
        private bool _viewed;

        public Email(SMTPMessage message)
        {
            _recieved = DateTime.Now;
            Message = message;

            Subject = new SharpMessage(message.Data).Subject;
        }

        public SMTPMessage Message { get; private set; }

        public string FromAddress
        {
            get
            {
                return Message.FromAddress.Address;
            }
        }

        public void SaveToFile(FileInfo file)
        {
            _viewed = true;
            File.WriteAllText(file.FullName, Message.Data, Encoding.ASCII);
        }

        public string[] ToAddresses
        {
            get
            {
                return Message.ToAddresses.Select(a => a.Address).ToArray();
            }
        }

        public DateTime Recieved
        {
            get
            {
                return _recieved;
            }
        }

        public string Subject
        {
            get;
            private set;
        }

        public bool HasBeenViewed
        {
            get
            {
                return _viewed;
            }
        }
    }
}