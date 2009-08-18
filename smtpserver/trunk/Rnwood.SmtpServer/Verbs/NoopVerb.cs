using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Verbs
{
    public class NoopVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Sucessfully did nothing"));
        }
    }
}
