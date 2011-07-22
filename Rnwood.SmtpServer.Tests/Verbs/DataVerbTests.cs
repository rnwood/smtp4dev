using System;
using System.Collections.Generic;
using System.IO;
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
            //Check escaping of end of message character ".." is decoded to "."
            //but the .. after B should be left alone
            TestGoodData(new string[] { "A", "..", "B..", "." }, "A\r\n.\r\nB..");
        }

        [Test]
        public void Data_EmptyMessage()
        {
            TestGoodData(new string[] { "." }, "");
        }

        [Test]
        public void Data_7BitTruncation()
        {
            TestGoodData(new string[] { ((char) (0x41+128)).ToString(), "." }, "\u0041");
        }

        private void TestGoodData(string[] messageData, string expectedData)
        {
            Mocks mocks = new Mocks();

            Message message = new Message(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns((long?)null);

            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => messageData[messageLine++]);

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);

            using (StreamReader dataReader = new StreamReader(message.GetData(), Encoding.ASCII))
            {
                Assert.AreEqual(expectedData, dataReader.ReadToEnd());
            }
        }

        [Test]
        public void MessageAboveFixedSize()
        {
            Mocks mocks = new Mocks();

            Message message = new Message(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns(10);

            string[] messageData = new string[] { new string('x', 11), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => messageData[messageLine++]);

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.ExceededStorageAllocation);
        }

        [Test]
        public void MessageInsideFixedSize()
        {
            Mocks mocks = new Mocks();

            Message message = new Message(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns(10);

            string[] messageData = new string[] { new string('x', 10), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => messageData[messageLine++]);

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }

        [Test]
        public void Data_NoCurrentMessage_ReturnsError()
        {
            Mocks mocks = new Mocks();

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
        }
    }
}
