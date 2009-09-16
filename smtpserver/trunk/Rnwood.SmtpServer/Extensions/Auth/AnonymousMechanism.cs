using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AnonymousMechanism : IAuthMechanism
    {
        public string Identifier
        {
            get { return "ANONYMOUS"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor)
        {
            return new AnonymousMechanismProcessor();
        }

        public bool IsPlainText
        {
            get { return false; }
        }
    }

    public class AnonymousMechanismProcessor : IAuthMechanismProcessor
    {
        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            return AuthMechanismProcessorStatus.Success;
        }
    }
}
