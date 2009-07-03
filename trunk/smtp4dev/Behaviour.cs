using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Rnwood.SmtpServer;

namespace smtp4dev
{
    public class Behaviour : ServerBehaviour
    {
        public override void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public override string DomainName
        {
            get { return "localhost"; }
        }

        public override IPAddress IpAddress
        {
            get { return IPAddress.Any; }
        }

        public override int PortNumber
        {
            get { return Properties.Settings.Default.PortNumber; }
        }

        public override bool RunOverSSL
        {
            get { return false; }
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }

    }
}
