#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class MailVerb : IVerb
    {
        public MailVerb()
        {
            SubVerbMap = new VerbMap();
            SubVerbMap.SetVerbProcessor("FROM", new MailFromVerb());
        }

        public VerbMap SubVerbMap { get; private set; }

        public MailFromVerb FromSubVerb
        {
            get { return (MailFromVerb) SubVerbMap.GetVerbProcessor("FROM"); }
        }


        public void Process(IConnection connection, SmtpCommand command)
        {
            SmtpCommand subrequest = new SmtpCommand(command.ArgumentsText);
            IVerb verbProcessor = SubVerbMap.GetVerbProcessor(subrequest.Verb);

            if (verbProcessor != null)
            {
                verbProcessor.Process(connection, subrequest);
            }
            else
            {
                connection.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.CommandParameterNotImplemented,
                                     "Subcommand {0} not implemented", subrequest.Verb));
            }
        }
    }
}