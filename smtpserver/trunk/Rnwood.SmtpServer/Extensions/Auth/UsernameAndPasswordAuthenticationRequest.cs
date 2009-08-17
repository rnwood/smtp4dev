using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class UsernameAndPasswordAuthenticationRequest : AuthenticationRequest
    {
        public UsernameAndPasswordAuthenticationRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }
    }
}
