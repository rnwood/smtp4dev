using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class Message
    {
        public Message(Session session)
        {
            Session = session;
            To = new List<string>();
        }

        public Session Session
        {
            get; private set;
        }

        public string From
        {
            get;
            internal set;
        }

        public List<string> To
        {
            get;
            private set;
        }

        public string Data { get; internal set; }
    }
}
