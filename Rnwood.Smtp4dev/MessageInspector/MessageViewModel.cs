using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using anmar.SharpMimeTools;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rnwood.Smtp4dev.MessageInspector
{
    public class MessageViewModel
    {
        public MessageViewModel(SharpMimeMessage message)
        {
            Message = message;
        }

        public SharpMimeMessage Message { get; private set; }

        public MessageViewModel[] Children
        {
            get
            {
                return Message.Cast<SharpMimeMessage>().Select(part => new MessageViewModel(part)).ToArray();
            }
        }

        public string Data
        {
            get
            {
                return Message.Header.RawHeaders + "\r\n\r\n" + Message.Body;
            }
        }

        public string Body
        {
            get
            {
                return Message.BodyDecoded;
            }
        }

        public string Type
        {
            get
            {
                return Message.Header.TopLevelMediaType + "/" + Message.Header.SubType;
            }
        }

        public long Size
        {
            get
            {
                return Message.Size;
            }
        }

        public string Disposition
        {
            get
            {
                return Message.Header.ContentDisposition;
            }
        }

        public string Encoding
        {
            get
            {
                return Message.Header.ContentTransferEncoding;
            }
        }

        public string Name
        {
            get
            {
                return Message.Name??"Unnamed" + ": " + Message.Header.TopLevelMediaType + "/" + Message.Header.SubType + " (" + Message.Size + " bytes)";
            }
        }

    }
}
