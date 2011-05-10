namespace Rnwood.SmtpServer.Verbs
{
    public abstract class VerbWithSubCommands : IVerb
    {
        protected VerbWithSubCommands() : this(new VerbMap())
        {
        }

        protected VerbWithSubCommands(IVerbMap subVerbMap)
        {
            SubVerbMap = subVerbMap;
        }

        public IVerbMap SubVerbMap { get; private set; }

        public virtual void Process(IConnection connection, SmtpCommand command)
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