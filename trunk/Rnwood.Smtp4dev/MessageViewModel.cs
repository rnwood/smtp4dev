using System;
using System.IO;
using System.Linq;
using System.Text;
using anmar.SharpMimeTools;
using Rnwood.SmtpServer;

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
            get
            {
                return Message.From;
            }
        }

        public void SaveToFile(FileInfo file)
        {
            HasBeenViewed = true;
            File.WriteAllBytes(file.FullName, Message.Data);
        }

        public string To
        {
            get
            {
                return string.Join(", ", Message.To);
            }
        }

        public DateTime ReceivedDate
        {
            get
            {
                return Message.ReceivedDate;
            }
        }

        public string Subject
        {
            get
            {
                return Message.Contents.Header.Subject;
            }
        }

        public bool HasBeenViewed
        {
            get;
            private set;
        }

        public void MarkAsViewed()
        {
            HasBeenViewed = true;
        }
    }
}