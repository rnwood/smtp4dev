namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class LoginAuthenticationCredentials : UsernameAndPasswordAuthenticationCredentials
    {
        public LoginAuthenticationCredentials(string username, string password) : base(username, password)
        {
        }
    }
}