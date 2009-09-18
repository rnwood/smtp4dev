#region

using System;
using System.IO;
using Rnwood.SmtpServer;

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
            get { return Message.Contents.Header.Subject; }
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