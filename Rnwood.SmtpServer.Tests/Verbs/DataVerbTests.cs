using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using System.Text;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class DataVerbTests
    {
        [TestMethod]
        public void Data_DoubleDots_Unescaped()
        {
            //Check escaping of end of message character ".." is decoded to "."
            //but the .. after B should be left alone
            TestGoodData(new string[] { "A", "..", "B..", "." }, "A\r\n.\r\nB..", true);
        }

        [TestMethod]
        public void Data_EmptyMessage_Accepted()
        {
            TestGoodData(new string[] { "." }, "", true);
        }

        [TestMethod]
        public void Data_8BitData_TruncatedTo7Bit()
        {
            TestGoodData(new string[] { ((char)(0x41 + 128)).ToString(), "." }, "\u0041", false);
        }

        [TestMethod]
        public void Data_8BitData_PassedThrough()
        {
            string data = ((char)(0x41 + 128)).ToString();
            TestGoodData(new string[] { data, "." }, data, true);
        }

        private void TestGoodData(string[] messageData, string expectedData, bool eightBitClean)
        {
            Mocks mocks = new Mocks();

            if (eightBitClean)
            {
                mocks.Connection.SetupGet(c => c.ReaderEncoding).Returns(Encoding.UTF8);
            }

            MemoryMessage message = new MemoryMessage(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns((long?)null);

            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => messageData[messageLine++]);

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);

            using (StreamReader dataReader = new StreamReader(message.GetData(), eightBitClean ? Encoding.UTF8 : Encoding.Default))
            {
                Assert.AreEqual(expectedData, dataReader.ReadToEnd());
            }
        }

        [TestMethod]
        public void Data_AboveSizeLimit_Rejected()
        {
            Mocks mocks = new Mocks();

            MemoryMessage message = new MemoryMessage(mocks.Session.Object);
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

        [TestMethod]
        public void Data_ExactlySizeLimit_Accepted()
        {
            Mocks mocks = new Mocks();

            MemoryMessage message = new MemoryMessage(mocks.Session.Object);
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

        [TestMethod]
        public void Data_WithinSizeLimit_Accepted()
        {
            Mocks mocks = new Mocks();

            MemoryMessage message = new MemoryMessage(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns(10);

            string[] messageData = new string[] { new string('x', 9), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => messageData[messageLine++]);

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }

        [TestMethod]
        public void Data_NoCurrentMessage_ReturnsError()
        {
            Mocks mocks = new Mocks();

            DataVerb verb = new DataVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
        }
    }
}