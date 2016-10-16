using Moq;
using Rnwood.SmtpServer.Verbs;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class VerbWithSubCommandsTests
    {
        [Fact]
        public async Task ProcessAsync_RegisteredSubCommand_Processed()
        {
            Mocks mocks = new Mocks();

            Mock<VerbWithSubCommands> verbWithSubCommands = new Mock<VerbWithSubCommands>() { CallBase = true };
            Mock<IVerb> verb = new Mock<IVerb>();
            verbWithSubCommands.Object.SubVerbMap.SetVerbProcessor("SUBCOMMAND1", verb.Object);

            await verbWithSubCommands.Object.ProcessAsync(mocks.Connection.Object, new SmtpCommand("VERB SUBCOMMAND1"));

            verb.Verify(v => v.ProcessAsync(mocks.Connection.Object, new SmtpCommand("SUBCOMMAND1")));
        }

        [Fact]
        public async Task ProcessAsync_UnregisteredSubCommand_ErrorResponse()
        {
            Mocks mocks = new Mocks();

            Mock<VerbWithSubCommands> verbWithSubCommands = new Mock<VerbWithSubCommands>() { CallBase = true };

            await verbWithSubCommands.Object.ProcessAsync(mocks.Connection.Object, new SmtpCommand("VERB SUBCOMMAND1"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.CommandParameterNotImplemented);
        }
    }
}