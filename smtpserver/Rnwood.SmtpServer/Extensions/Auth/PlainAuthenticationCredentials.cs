namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class PlainAuthenticationCredentials : UsernameAndPasswordAuthenticationCredentials
    {
        public PlainAuthenticationCredentials(string username, string password) : base(username, password)
        {
        }
    }
}