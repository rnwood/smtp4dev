using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class CommandEventArgsTests
    {
        [Test]
        public void Command()
        {
            SmtpCommand command = new SmtpCommand("BLAH");
            CommandEventArgs args = new CommandEventArgs(command);

            Assert.AreSame(command, args.Command);
        }
    }
}
