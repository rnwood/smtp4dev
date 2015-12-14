#region

using Rnwood.SmtpServer.Extensions.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

#endregion

namespace Rnwood.SmtpServer
{
    public abstract class AbstractSession : IEditableSession
    {
        public AbstractSession(IPAddress clientAddress, DateTime startDate)
        {
            _messages = new List<IMessage>();
            ClientAddress = clientAddress;
            StartDate = startDate;
        }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public IPAddress ClientAddress { get; set; }

        public string ClientName { get; set; }

        public bool SecureConnection { get; set; }

        public abstract TextReader GetLog();

        public abstract void Dispose();

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

        public Exception SessionError { get; set; }

        public SessionErrorType SessionErrorType { get; set; }

        public abstract void AppendToLog(string text);
    }
}