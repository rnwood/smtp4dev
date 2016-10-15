using System.Threading.Tasks;

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

        public async virtual Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            SmtpCommand subrequest = new SmtpCommand(command.ArgumentsText);
            IVerb verbProcessor = SubVerbMap.GetVerbProcessor(subrequest.Verb);

            if (verbProcessor != null)
            {
                await verbProcessor.ProcessAsync(connection, subrequest);
            }
            else
            {
                await connection.WriteResponseAsync(
                    new SmtpResponse(StandardSmtpResponseCode.CommandParameterNotImplemented,
                                     "Subcommand {0} not implemented", subrequest.Verb));
            }
        }
    }
}