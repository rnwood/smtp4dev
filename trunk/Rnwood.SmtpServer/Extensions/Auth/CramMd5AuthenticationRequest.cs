namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5AuthenticationRequest : IAuthenticationRequest
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