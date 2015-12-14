#region

using System.Collections.Generic;
using System.Linq;
using Rnwood.SmtpServer.Verbs;

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