#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Rnwood.SmtpServer.Extensions.Auth;

#endregion

namespace Rnwood.SmtpServer
{
    public abstract class AbstractSession : IEditableSession
    {
        public AbstractSession()
        {
            _messages = new List<IMessage>();
        }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public IPAddress ClientAddress { get; set; }

        public string ClientName { get; set; }

        public bool SecureConnection { get; set; }

        public abstract TextReader GetLog();
        
        public IMessage[] GetMessages()
        {
            return _messages.ToArray();
        }

        public void AddMessage(IMessage message)
        {
            _messages.Add(message);
        }

        private List<IMessage> _messages;

        public bool CompletedNormally { get; set; }

        public bool Authenticated { get; set; }

        public IAuthenticationCredentials AuthenticationCredentials { get; set; }

        public string SessionError { get; set; }

        public abstract void AppendToLog(string text);

        
    }
}