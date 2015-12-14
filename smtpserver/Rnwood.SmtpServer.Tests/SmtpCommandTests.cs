using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class SmtpCommandTests
    {
        [TestMethod]
        public void Parsing_SingleToken()
        {
            SmtpCommand command = new SmtpCommand("DATA");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual("DATA", command.Verb);
            Assert.AreEqual("", command.ArgumentsText);
        }

        [TestMethod]
        public void Parsing_ArgsSeparatedBySpace()
        {
            SmtpCommand command = new SmtpCommand("DATA ARGS");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual("DATA", command.Verb);
            Assert.AreEqual("ARGS", command.ArgumentsText);
        }

        [TestMethod]
        public void Parsing_ArgsSeparatedByColon()
        {
            SmtpCommand command = new SmtpCommand("DATA:ARGS");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual("DATA", command.Verb);
            Assert.AreEqual("ARGS", command.ArgumentsText);
        }
    }
}