using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using anmar.SharpMimeTools;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class Message
    {
        public Message(Session session)
        {
            Session = session;
            ToList = new List<string>();
            ReceivedDate = DateTime.Now;
        }

        public DateTime ReceivedDate
        {
            get;
            internal set;
        }
        
        public Session Session
        {
            get;
            private set;
        }

        public string From
        {
            get;
            internal set;
        }

        internal List<string> ToList
        {
            get;
            set;
        }

        public string[] To
        {
            get
            {
                return ToList.ToArray();
            }
        }

        public byte[] Data { get; internal set; }

        private SharpMimeMessage _contents;
        public SharpMimeMessage Contents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = new SharpMimeMessage(new MemoryStream(Data));
                }

                return _contents;
            }
        }
    }
}
