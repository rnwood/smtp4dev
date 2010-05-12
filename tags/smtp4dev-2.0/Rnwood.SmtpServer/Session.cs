#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Rnwood.SmtpServer.Extensions.Auth;

#endregion

namespace Rnwood.SmtpServer
{
    public class Session : ISession, ICompletedSession
    {
        private readonly StringBuilder _log = new StringBuilder();

        internal Session()
        {
            Messages = new List<Message>();
        }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public IPAddress ClientAddress { get; set; }

        public string ClientName { get; set; }

        public bool SecureConnection { get; set; }

        public string Log
        {
            get { return _log.ToString(); }
        }

        public List<Message> Messages { get; set; }

        public bool CompletedNormally { get; set; }

        public bool Authenticated { get; set; }

        public IAuthenticationRequest AuthenticationCredentials { get; set; }

        public string SessionError { get; set; }

        public void AppendToLog(string text)
        {
            _log.AppendLine(text);
        }
    }
}