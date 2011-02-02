#region

using System;
using System.IO;
using Rnwood.SmtpServer;
using anmar.SharpMimeTools;
using System.Linq;

#endregion

namespace Rnwood.Smtp4dev
{
    public class MessageViewModel
    {
        public MessageViewModel(IMessage message)
        {
            Message = message;
        }

        public IMessage Message { get; private set; }

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
            get { return SharpMimeTools.rfc2047decode(Parts.Header.Subject); }
        }

        private SharpMimeMessage _contents;
        public SharpMimeMessage Parts
        {
            get
            {
                if (_contents == null)
                {
                    _contents = new SharpMimeMessage(Message.GetData());
                }

                return _contents;
            }
        }

        public bool HasBeenViewed { get; private set; }

        public void SaveToFile(FileInfo file)
        {
            HasBeenViewed = true;

            byte[] data = new byte[64 * 1024];
            int bytesRead;

            using (Stream dataStream = Message.GetData())
            {
                using (FileStream fileStream = file.OpenWrite())
                {
                    while ((bytesRead = dataStream.Read(data, 0, data.Length)) > 0)
                    {
                        fileStream.Write(data, 0, bytesRead);
                    }
                }
            }
        }

        public void MarkAsViewed()
        {
            HasBeenViewed = true;
        }

        public bool MatchesFilter(string text)
        {
            string[] searchText = new string[] { Subject, To, From };
            return searchText.Any(st => st.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1);
        }
    }
}