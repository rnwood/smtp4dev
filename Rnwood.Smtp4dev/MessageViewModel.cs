#region

using System;
using System.IO;
using Rnwood.SmtpServer;
using anmar.SharpMimeTools;

#endregion

namespace Rnwood.Smtp4dev
{
    public class MessageViewModel
    {
        public MessageViewModel(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }

        public string From
        {
            get { return Message.From; }
        }

        public string To
        {
            get { return string.Join(", ", Message.To); }
        }

        public DateTime ReceivedDate
        {
            get { return Message.ReceivedDate; }
        }

        public string Subject
        {
            get { return Parts.Header.Subject; }
        }

        private SharpMimeMessage _contents;
        public SharpMimeMessage Parts
        {
            get
            {
                if (_contents == null)
                {
                    _contents = new SharpMimeMessage(new MemoryStream(Message.Data));
                }

                return _contents;
            }
        }

        public bool HasBeenViewed { get; private set; }

        public void SaveToFile(FileInfo file)
        {
            HasBeenViewed = true;
            File.WriteAllBytes(file.FullName, Message.Data);
        }

        public void MarkAsViewed()
        {
            HasBeenViewed = true;
        }
    }
}