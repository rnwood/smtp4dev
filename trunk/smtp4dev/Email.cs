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
            Envelope = message;
            Contents = new SharpMessage(Envelope.Data);

        }

        public Message Envelope { get; private set; }
        public SharpMessage Contents { get; private set; }

        public string FromAddress
        {
            get
            {
                return Contents.FromAddress;
            }
        }

        public void SaveToFile(FileInfo file)
        {
            _viewed = true;
            File.WriteAllText(file.FullName, Envelope.Data, Encoding.ASCII);
        }

        public string[] ToAddresses
        {
            get
            {
                return Contents.To.Cast<SharpMimeAddress>().Select(adr => adr.ToString()).ToArray();
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
            get
            {
                return Contents.Subject;
            }
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