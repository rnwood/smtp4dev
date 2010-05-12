namespace Rnwood.SmtpServer.Extensions
{
    public interface IExtension
    {
        IExtensionProcessor CreateExtensionProcessor(IConnection connection);
    }

    public interface IExtensionProcessor
    {
        string[] EHLOKeywords
        {
            get;
        }
    }
}