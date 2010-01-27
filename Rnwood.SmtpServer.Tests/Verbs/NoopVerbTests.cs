using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class NoopVerbTests
    {
        [Test]
        public void Noop()
        {
            Mock<IConnection> connection = new Mock<IConnection>();
            Mock<ISession> session = new Mock<ISession>();
            connection.SetupGet(c => c.Session).Returns(session.Object);

            NoopVerb verb = new NoopVerb();
            verb.Process(connection.Object, new SmtpCommand("NOOP"));

            connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)StandardSmtpResponseCode.OK)));
        }
    }
}
