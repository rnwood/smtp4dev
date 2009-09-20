namespace Rnwood.SmtpServer.Extensions.Auth
{
    public interface IAuthMechanism
    {
        string Identifier { get; }

        bool IsPlainText { get; }
        IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection);
    }
}