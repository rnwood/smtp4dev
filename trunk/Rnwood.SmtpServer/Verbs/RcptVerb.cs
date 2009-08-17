using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer
{
    public class RcptVerb : Verb
    {
        public RcptVerb()
        {
            SubVerbMap = new VerbMap();
            SubVerbMap.SetVerbProcessor("TO", new RcptToVerb());
        }

        public VerbMap SubVerbMap
        {
            get;
            private set;
        }

        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            SmtpRequest subrequest = new SmtpRequest(request.ArgumentsText);
            Verb verbProcessor = SubVerbMap.GetVerbProcessor(subrequest.Verb);

            if (verbProcessor != null)
            {
                verbProcessor.Process(connectionProcessor, subrequest);
            }
            else
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.CommandParameterNotImplemented, "Subcommand {0} not implemented", subrequest.Verb));
            }
        }
    }
}
