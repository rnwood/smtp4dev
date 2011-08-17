using System;
using System.Collections.Generic;
using System.Net;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer
{
    public interface IEditableSession : ISession
    {
        DateTime StartDate { get; set; }
        
        DateTime? EndDate { get; set; }

        IPAddress ClientAddress { get; set; }

        string ClientName { get; set; }

        bool SecureConnection { get; set; }

        void AddMessage(IMessage message);
        
        bool CompletedNormally { get; set; }

        bool Authenticated { get; set; }

        IAuthenticationCredentials AuthenticationCredentials { get; set; }

        Exception SessionError { get; set; }
        SessionErrorType SessionErrorType { get; set; }

        void AppendToLog(string text);
    }
}