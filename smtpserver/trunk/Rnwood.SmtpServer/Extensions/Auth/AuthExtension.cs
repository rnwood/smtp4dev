#region



#endregion

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthExtension : IExtension
    {
        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new AuthExtensionProcessor(connection);
        }
    }
}