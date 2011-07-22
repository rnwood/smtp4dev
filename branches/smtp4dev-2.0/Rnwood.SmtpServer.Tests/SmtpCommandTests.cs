using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class SmtpCommandTests
    {
        [Test]
        public void Parsing_SingleToken()
        {
            SmtpCommand command = new SmtpCommand("DATA");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual("DATA", command.Verb);
            Assert.AreEqual("", command.ArgumentsText);
            Assert.AreEqual(0, command.Arguments.Length);
        }

        [Test]
        public void Parsing_MailFrom_WithDisplayName()
        {
            SmtpCommand command = new SmtpCommand("MAIL FROM:<Robert Wood<rob@rnwood.co.uk>> ARG1 ARG2");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual("MAIL", command.Verb);
            Assert.AreEqual("FROM:<Robert Wood<rob@rnwood.co.uk>> ARG1 ARG2", command.ArgumentsText);
            Assert.AreEqual("FROM", command.Arguments[0]);
            Assert.AreEqual("<Robert Wood<rob@rnwood.co.uk>>", command.Arguments[1]);
            Assert.AreEqual("ARG1", command.Arguments[2]);
            Assert.AreEqual("ARG2", command.Arguments[3]);
        }

        [Test]
        public void Parsing_MailFrom_EmailOnly()
        {
            SmtpCommand command = new SmtpCommand("MAIL FROM:<rob@rnwood.co.uk> ARG1 ARG2");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual("MAIL", command.Verb);
            Assert.AreEqual("FROM:<rob@rnwood.co.uk> ARG1 ARG2", command.ArgumentsText);
            Assert.AreEqual("FROM", command.Arguments[0]);
            Assert.AreEqual("<rob@rnwood.co.uk>", command.Arguments[1]);
            Assert.AreEqual("ARG1", command.Arguments[2]);
            Assert.AreEqual("ARG2", command.Arguments[3]);
        }
    }
}
