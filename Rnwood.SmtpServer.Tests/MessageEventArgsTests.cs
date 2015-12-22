using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class MessageEventArgsTests
    {
        [TestMethod]
        public void Message()
        {
            IMessage message = new MemoryMessage();
            MessageEventArgs messageEventArgs = new MessageEventArgs(message);

            Assert.AreSame(message, messageEventArgs.Message);
        }
    }
}