using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public interface IAuthMechanismProcessor
    {
        AuthMechanismProcessorStatus ProcessResponse(string data);
    }
}
