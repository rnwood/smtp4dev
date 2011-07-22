namespace Rnwood.SmtpServer
{
    public interface IServer
    {
        IServerBehaviour Behaviour { get; }
    }
}