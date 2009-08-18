using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Verbs
{
    public abstract class Verb
    {
        public abstract void Process(IConnectionProcessor connectionProcessor, SmtpRequest request);
    }
}