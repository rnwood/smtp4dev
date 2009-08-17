using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5AuthenticationRequest : AuthenticationRequest
    {
        public CramMd5AuthenticationRequest(string username, string challenge, string challengeResponse)
        {
            Username = username;
            ChallengeResponse = challengeResponse;
            Challenge = challenge;
        }

        public string Username { get; private set; }

        public string ChallengeResponse { get; private set; }

        public string Challenge { get; private set; }
    }
}
