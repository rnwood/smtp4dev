using Moq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class DataVerbTests
    {
        [Fact]
        public async Task Data_DoubleDots_Unescaped()
        {
            //Check escaping of end of message character ".." is decoded to "."
            //but the .. after B should be left alone
            await TestGoodDataAsync(new string[] { "A", "..", "B..", "." }, "A\r\n.\r\nB..", true);
        }

        [Fact]
        public async Task Data_EmptyMessage_Accepted()
        {
            await TestGoodDataAsync(new string[] { "." }, "", true);
        }

        [Fact]
        public async Task Data_8BitData_TruncatedTo7Bit()
        {
            await TestGoodDataAsync(new string[] { ((char)(0x41 + 128)).ToString(), "." }, "\u0041", false);
        }

        [Fact]
        public async Task Data_8BitData_PassedThrough()
        {
            string data = ((char)(0x41 + 128)).ToString();
            await TestGoodDataAsync(new string[] { data, "." }, data, true);
        }

        private async Task TestGoodDataAsync(string[] messageData, string expectedData, bool eightBitClean)
        {
            Mocks mocks = new Mocks();

            if (eightBitClean)
            {
                mocks.Connection.SetupGet(c => c.ReaderEncoding).Returns(Encoding.UTF8);
            }

            MemoryMessage.Builder messageBuilder = new MemoryMessage.Builder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns((long?)null);

            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLineAsync()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("DATA"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);

            using (StreamReader dataReader = new StreamReader(messageBuilder.GetData(), eightBitClean ? Encoding.UTF8 : new ASCIISevenBitTruncatingEncoding()))
            {
                Assert.Equal(expectedData, dataReader.ReadToEnd());
            }
        }

        [Fact]
        public async Task Data_AboveSizeLimit_Rejected()
        {
            Mocks mocks = new Mocks();

            MemoryMessage.Builder messageBuilder = new MemoryMessage.Builder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns(10);

            string[] messageData = new string[] { new string('x', 11), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLineAsync()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("DATA"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.ExceededStorageAllocation);
        }

        [Fact]
        public async Task Data_ExactlySizeLimit_Accepted()
        {
            Mocks mocks = new Mocks();

            MemoryMessage.Builder messageBuilder = new MemoryMessage.Builder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns(10);

            string[] messageData = new string[] { new string('x', 10), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLineAsync()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("DATA"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
        }

        [Fact]
        public async Task Data_WithinSizeLimit_Accepted()
        {
            Mocks mocks = new Mocks();

            MemoryMessage.Builder messageBuilder = new MemoryMessage.Builder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).Returns(10);

            string[] messageData = new string[] { new string('x', 9), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLineAsync()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("DATA"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
        }

        [Fact]
        public async Task Data_NoCurrentMessage_ReturnsError()
        {
            Mocks mocks = new Mocks();

            DataVerb verb = new DataVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("DATA"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.BadSequenceOfCommands);
        }
    }
}