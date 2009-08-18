using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Verbs
{
    public class RsetVerb : Verb
    {
        public RsetVerb()
        {
        }

        public override void Process(IConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            connectionProcessor.AbortMessage();
        }
    }
}
