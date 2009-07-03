using System;
using System.IO;
using System.Linq;
using System.Text;
using anmar.SharpMimeTools;
using Rnwood.SmtpServer;

namespace smtp4dev
{
    public class Email
    {
        private DateTime _recieved;
        private bool _viewed;

        public Email(Message message)
        {
            _recieved = DateTime.Now;
            Message = message;
            MessageDetails = new SharpMessage(Message.Data);

            Subject = new SharpMessage(message.Data).Subject;
        }

        public Message Message { get; private set; }
        public SharpMessage MessageDetails { get; private set; }

        public string FromAddress
        {
            get
            {
                return MessageDetails.FromAddress;
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
                return MessageDetails.To.Cast<SharpMimeAddress>().Select(adr => adr.ToString()).ToArray();
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