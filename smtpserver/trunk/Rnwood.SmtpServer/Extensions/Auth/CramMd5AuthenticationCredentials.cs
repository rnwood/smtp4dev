using System;
using System.Security.Cryptography;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5AuthenticationCredentials : IAuthenticationCredentials
    {
        public CramMd5AuthenticationCredentials(string username, string challenge, string challengeResponse)
        {
            Username = username;
            ChallengeResponse = challengeResponse;
            Challenge = challenge;
        }

        public string Username { get; private set; }

        public string ChallengeResponse { get; private set; }

        public string Challenge { get; private set; }

        public bool ValidateResponse(string password)
        {
            HMACMD5 hmacmd5 = new HMACMD5(ASCIIEncoding.ASCII.GetBytes(password));
            string expectedResponse = BitConverter.ToString(hmacmd5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(Challenge))).Replace("-", "");

            return string.Equals(expectedResponse, ChallengeResponse, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}