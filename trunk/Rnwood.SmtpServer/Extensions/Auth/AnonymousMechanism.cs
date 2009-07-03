using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AnonymousMechanism : AuthMechanism
    {
        public override string Identifier
        {
            get { return "ANONYMOUS"; }
        }

        public override AuthMechanismProcessor CreateAuthMechanismProcessor(ConnectionProcessor connectionProcessor)
        {
            return new AnonymousMechanismProcessor();
        }
    }

    public class AnonymousMechanismProcessor : AuthMechanismProcessor
    {
        public override AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            return AuthMechanismProcessorStatus.Success;
        }
    }
}
