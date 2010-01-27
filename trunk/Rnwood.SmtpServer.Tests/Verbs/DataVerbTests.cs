using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class DataVerbTests
    {
        [Test]
        public void Data()
        {
            Mock<IConnection> connection = new Mock<IConnection>();
            Mock<ISession> session = new Mock<ISession>();
            connection.SetupGet(c => c.Session).Returns(session.Object);
            connection.SetupGet(c => c.CurrentMessage).Returns(new Message(session.Object));
            connection.Setup(c => c.Server.Behaviour.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns((long?)null);

            string[] message = new string[] { "A", "B", "." };
            int messageLine = 0;

            connection.Setup(c => c.ReadLine()).Returns(() => message[messageLine++]);

            DataVerb verb = new DataVerb();
            verb.Process(connection.Object, new SmtpCommand("DATA"));

            connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)StandardSmtpResponseCode.StartMailInputEndWithDot)));
            connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)StandardSmtpResponseCode.OK)));

            
        }

        [Test]
        public void Data_NoCurrentMessage_ReturnsError()
        {
            Mock<IConnection> connection = new Mock<IConnection>();
            Mock<ISession> session = new Mock<ISession>();
            connection.SetupGet(c => c.Session).Returns(session.Object);

            DataVerb verb = new DataVerb();
            verb.Process(connection.Object, new SmtpCommand("DATA"));

            connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)StandardSmtpResponseCode.BadSequenceOfCommands)));
        }
    }
}
