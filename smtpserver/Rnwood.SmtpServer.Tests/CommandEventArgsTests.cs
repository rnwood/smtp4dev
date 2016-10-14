using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    
    public class CommandEventArgsTests
    {
        [Fact]
        public void Command()
        {
            SmtpCommand command = new SmtpCommand("BLAH");
            CommandEventArgs args = new CommandEventArgs(command);

            Assert.Same(command, args.Command);
        }
    }
}