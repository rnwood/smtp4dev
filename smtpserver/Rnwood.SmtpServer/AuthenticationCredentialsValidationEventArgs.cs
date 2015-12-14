using Rnwood.SmtpServer.Extensions.Auth;
using System;

namespace Rnwood.SmtpServer
{
    public class AuthenticationCredentialsValidationEventArgs : EventArgs
    {
        public AuthenticationCredentialsValidationEventArgs(IAuthenticationCredentials credentials)
        {
            Credentials = credentials;
        }

        public IAuthenticationCredentials Credentials { get; private set; }

        public AuthenticationResult AuthenticationResult { get; set; }
    }
}