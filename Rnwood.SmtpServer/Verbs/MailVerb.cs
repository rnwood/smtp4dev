using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class MailVerb : Verb
    {
        public MailVerb()
        {
            SubVerbMap = new VerbMap();
            SubVerbMap.SetVerbProcessor("FROM", new MailFromVerb());
        }

        public VerbMap SubVerbMap
        {
            get;
            private set;
        }

        public MailFromVerb FromSubVerb
        {
            get
            {
                return (MailFromVerb) SubVerbMap.GetVerbProcessor("FROM");
            }
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
