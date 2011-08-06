using System;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer
{
    public class AuthenticationCredentialsValidationEventArgs : EventArgs
    {
        public AuthenticationCredentialsValidationEventArgs(IAuthenticationRequest credentials)
        {
            Credentials = credentials;
        }

        public IAuthenticationRequest Credentials { get; private set; }

        public AuthenticationResult AuthenticationResult { get; set; }
    }
}