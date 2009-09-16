using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public interface IAuthMechanism
    {
        string Identifier
        {
            get;
        }

        IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor);

        bool IsPlainText { get; }
    }
}
