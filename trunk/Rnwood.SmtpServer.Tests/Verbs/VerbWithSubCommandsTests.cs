using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class VerbWithSubCommandsTests
    {
        [TestMethod]
        public void Process_RegisteredSubCommand_Processed()
        {
            Mocks mocks = new Mocks();

            Mock<VerbWithSubCommands> verbWithSubCommands = new Mock<VerbWithSubCommands>() { CallBase = true };
            Mock<IVerb> verb = new Mock<IVerb>();
            verbWithSubCommands.Object.SubVerbMap.SetVerbProcessor("SUBCOMMAND1", verb.Object);

            verbWithSubCommands.Object.Process(mocks.Connection.Object, new SmtpCommand("VERB SUBCOMMAND1"));

            verb.Verify(v => v.Process(mocks.Connection.Object, new SmtpCommand("SUBCOMMAND1")));
        }

        [TestMethod]
        public void Process_UnregisteredSubCommand_ErrorResponse()
        {
            Mocks mocks = new Mocks();

            Mock<VerbWithSubCommands> verbWithSubCommands = new Mock<VerbWithSubCommands>() { CallBase = true };
            
            verbWithSubCommands.Object.Process(mocks.Connection.Object, new SmtpCommand("VERB SUBCOMMAND1"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.CommandParameterNotImplemented);
        }
    }
}
