using Moq;
using Rnwood.SmtpServer.Verbs;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public async Task Process_GreetingWritten()
        {
            Mocks mocks = new Mocks();
            mocks.ConnectionChannel.Setup(c => c.WriteLineAsync(It.IsAny<string>())).Callback(
                mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            await connection.ProcessAsync();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLineAsync(It.IsRegex("220 .*", RegexOptions.IgnoreCase)));
        }

        [Fact]
        public async Task Process_SmtpServerExceptionThrow_ResponseWritten()
        {
            Mocks mocks = new Mocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Setup(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);
            mockVerb.Setup(v => v.ProcessAsync(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>())).Returns(Task.FromException(new SmtpServerException(new SmtpResponse(500, "error"))));

            mocks.ConnectionChannel.Setup(c => c.ReadLineAsync()).ReturnsAsync("GOODCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            await connection.ProcessAsync();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLineAsync(It.IsRegex("500 error", RegexOptions.IgnoreCase)));
        }

        [Fact]
        public async Task Process_EmptyCommand_NoResponse()
        {
            Mocks mocks = new Mocks();

            mocks.ConnectionChannel.Setup(c => c.ReadLineAsync()).ReturnsAsync("").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            await connection.ProcessAsync();

            //Should only print service ready message
            mocks.ConnectionChannel.Verify(cc => cc.WriteLineAsync(It.Is<string>(s => !s.StartsWith("220 "))), Times.Never());
        }

        [Fact]
        public async Task Process_GoodCommand_Processed()
        {
            Mocks mocks = new Mocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Setup(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);

            mocks.ConnectionChannel.Setup(c => c.ReadLineAsync()).ReturnsAsync("GOODCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            await connection.ProcessAsync();

            mockVerb.Verify(v => v.ProcessAsync(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>()));
        }

        [Fact]
        public async Task Process_BadCommand_500Response()
        {
            Mocks mocks = new Mocks();
            mocks.ConnectionChannel.Setup(c => c.ReadLineAsync()).ReturnsAsync("BADCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            await connection.ProcessAsync();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLineAsync(It.IsRegex("500 .*", RegexOptions.IgnoreCase)));
        }

        [Fact]
        public async Task Process_TooManyBadCommands_Disconnected()
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.SetupGet(b => b.MaximumNumberOfSequentialBadCommands).Returns(2);

            mocks.ConnectionChannel.Setup(c => c.ReadLineAsync()).ReturnsAsync("BADCOMMAND");

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            await connection.ProcessAsync();

            mocks.ConnectionChannel.Verify(c => c.ReadLineAsync(), Times.Exactly(2));
            mocks.ConnectionChannel.Verify(cc => cc.WriteLineAsync(It.IsRegex("221 .*", RegexOptions.IgnoreCase)));
        }

        [Fact]
        public void AbortMessage()
        {
            Mocks mocks = new Mocks();

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.NewMessage();

            connection.AbortMessage();
            Assert.Null(connection.CurrentMessage);
        }

        [Fact]
        public void CommitMessage()
        {
            Mocks mocks = new Mocks();

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            IMessageBuilder messageBuilder = connection.NewMessage();
            IMessage message = messageBuilder.ToMessage();

            connection.CommitMessage();
            mocks.Session.Verify(s => s.AddMessage(message));
            mocks.ServerBehaviour.Verify(b => b.OnMessageReceived(connection, message));
            Assert.Null(connection.CurrentMessage);
        }
    }
}