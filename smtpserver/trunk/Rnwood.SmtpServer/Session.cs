#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

#endregion

namespace Rnwood.SmtpServer
{
    public class Session
    {
        private readonly StringBuilder _log = new StringBuilder();

        public Session()
        {
            Messages = new List<Message>();
        }

        public DateTime StartDate { get; internal set; }

        public DateTime? EndDate { get; internal set; }

        public IPAddress ClientAddress { get; internal set; }

        public string ClientName { get; internal set; }

        public bool SecureConnection { get; internal set; }

        public string Log
        {
            get { return _log.ToString(); }
        }

        public List<Message> Messages { get; private set; }

        public bool SessionCompleted { get; internal set; }

        public bool Authenticated { get; internal set; }

        public string SessionError { get; internal set; }

        internal void AppendToLog(string text)
        {
            _log.AppendLine(text);
        }
    }
}