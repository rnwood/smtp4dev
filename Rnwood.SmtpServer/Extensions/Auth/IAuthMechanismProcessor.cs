namespace Rnwood.SmtpServer.Extensions.Auth
{
    public interface IAuthMechanismProcessor
    {
        AuthMechanismProcessorStatus ProcessResponse(string data);

        IAuthenticationRequest Credentials
        {
            get;
        }
    }
}