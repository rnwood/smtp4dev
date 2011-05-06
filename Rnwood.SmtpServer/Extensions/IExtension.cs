namespace Rnwood.SmtpServer.Extensions
{
    public interface IExtension
    {
        IExtensionProcessor CreateExtensionProcessor(IConnection connection);
    }
}