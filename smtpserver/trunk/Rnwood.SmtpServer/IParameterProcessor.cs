namespace Rnwood.SmtpServer
{
    public interface IParameterProcessor
    {
        void SetParameter(IConnection connection, string key, string value);
    }
}