using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public abstract class AuthMechanismProcessor
    {
        public abstract AuthMechanismProcessorStatus ProcessResponse(string data);
    }
}
