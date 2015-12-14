using System;
using System.Collections.Generic;
using System.Net;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer
{
    public interface IEditableSession : ISession
    {
        new DateTime StartDate { get; set; }

        new DateTime? EndDate { get; set; }

        new IPAddress ClientAddress { get; set; }

        new string ClientName { get; set; }

        new bool SecureConnection { get; set; }

        void AddMessage(IMessage message);

        new bool CompletedNormally { get; set; }

        new bool Authenticated { get; set; }

        new IAuthenticationCredentials AuthenticationCredentials { get; set; }

        new Exception SessionError { get; set; }
        new SessionErrorType SessionErrorType { get; set; }

        void AppendToLog(string text);
    }
}