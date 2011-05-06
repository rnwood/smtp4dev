using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class SessionEventArgsTests
    {
        [Test]
        public void Session()
        {
            Mocks mocks = new Mocks();

            SessionEventArgs s = new SessionEventArgs(mocks.Session.Object);
            
            Assert.AreEqual(s.Session, mocks.Session.Object);
        }
    }
}
