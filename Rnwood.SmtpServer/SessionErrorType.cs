using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public enum SessionErrorType
    {
        NetworkError,
        UnexpectedException,
        ServerShutdown
    }
}
