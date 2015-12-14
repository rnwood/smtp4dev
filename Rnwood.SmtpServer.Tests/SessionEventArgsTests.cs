using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class SessionEventArgsTests
    {
        [TestMethod]
        public void Session()
        {
            Mocks mocks = new Mocks();

            SessionEventArgs s = new SessionEventArgs(mocks.Session.Object);

            Assert.AreEqual(s.Session, mocks.Session.Object);
        }
    }
}