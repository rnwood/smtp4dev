using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public abstract class AuthMechanism
    {
        public abstract string Identifier
        { 
            get;
        }

        public abstract AuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor);
    }
}
