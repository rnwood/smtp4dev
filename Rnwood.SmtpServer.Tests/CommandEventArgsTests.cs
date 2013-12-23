using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class CommandEventArgsTests
    {
        [TestMethod]
        public void Command()
        {
            SmtpCommand command = new SmtpCommand("BLAH");
            CommandEventArgs args = new CommandEventArgs(command);

            Assert.AreSame(command, args.Command);
        }
    }
}
