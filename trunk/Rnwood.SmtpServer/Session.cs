using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class Session
    {
        public Session()
        {
            Messages = new List<Message>();
        }

        public DateTime StartDate
        {
            get;
            internal set;
        }

        public DateTime? EndDate
        {
            get;
            internal set;
        }

        public IPAddress ClientAddress
        {
            get;
            internal set;
        }

        public string ClientName
        {
            get;
            internal set;
        }

        public bool SecureConnection
        {
            get;
            internal set;
        }

        public string Log
        {
            get
            {
                return _log.ToString();
            }
        }
        private StringBuilder _log = new StringBuilder();

        internal void AppendToLog(string text)
        {
            _log.AppendLine(text);
            //Console.WriteLine(text);
        }

        public List<Message> Messages
        {
            get;
            private set;
        }

        public bool SessionCompleted
        {
            get;
            internal set;
        }

        public bool Authenticated { get; internal set; }
    }
}