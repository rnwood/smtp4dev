using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class HeloVerbTests
    {
        [Test]
        public void SayHelo()
        {
            Mock<IConnection> connection = new Mock<IConnection>();
            Mock<ISession> session = new Mock<ISession>();
            connection.SetupGet(c => c.Session).Returns(session.Object);

            HeloVerb verb = new HeloVerb();
            verb.Process(connection.Object, new SmtpCommand("HELO foo.blah"));

            connection.Verify(
                c =>
                c.WriteResponse(It.Is<SmtpResponse>(response => response.Code == (int)StandardSmtpResponseCode.OK)));
            session.VerifySet(s => s.ClientName, "foo.bar");
        }

        [Test]
        public void SayHeloTwice_ReturnsError()
        {
            Mock<IConnection> connection = new Mock<IConnection>();
            Mock<ISession> session = new Mock<ISession>();
            connection.SetupGet(c => c.Session).Returns(session.Object);
            session.SetupGet(s => s.ClientName).Returns("already.said.helo");
            session.SetupSet(s => s.ClientName).Never();

            HeloVerb verb = new HeloVerb();
            verb.Process(connection.Object, new SmtpCommand("HELO foo.blah"));

            connection.Verify(
                c =>
                c.WriteResponse(It.Is<SmtpResponse>(response => response.Code == (int)StandardSmtpResponseCode.BadSequenceOfCommands)));
        }
    }
}
