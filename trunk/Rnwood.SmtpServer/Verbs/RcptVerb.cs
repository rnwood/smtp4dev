#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class RcptVerb : Verb
    {
        public RcptVerb()
        {
            SubVerbMap = new VerbMap();
            SubVerbMap.SetVerbProcessor("TO", new RcptToVerb());
        }

        public VerbMap SubVerbMap { get; private set; }

        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            SmtpCommand subrequest = new SmtpCommand(command.ArgumentsText);
            Verb verbProcessor = SubVerbMap.GetVerbProcessor(subrequest.Verb);

            if (verbProcessor != null)
            {
                verbProcessor.Process(connectionProcessor, subrequest);
            }
            else
            {
                connectionProcessor.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.CommandParameterNotImplemented,
                                     "Subcommand {0} not implemented", subrequest.Verb));
            }
        }
    }
}