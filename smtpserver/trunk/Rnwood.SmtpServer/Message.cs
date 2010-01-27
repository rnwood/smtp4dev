#region

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Rnwood.SmtpServer
{
    public class Message
    {
        public Message(ISession session)
        {
            Session = session;
            ToList = new List<string>();
            ReceivedDate = DateTime.Now;
        }

        public DateTime ReceivedDate { get; internal set; }

        public ISession Session { get; private set; }

        public string From { get; internal set; }

        internal List<string> ToList { get; set; }

        public string[] To
        {
            get { return ToList.ToArray(); }
        }

        public byte[] Data { get; internal set; }
    }
}