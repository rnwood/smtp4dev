using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rnwood.SmtpServer.Verbs;
using System.Text.RegularExpressions;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class ConnectionTests
    {
        [TestMethod]
        public void Process_GreetingWritten()
        {
            Mocks mocks = new Mocks();
            mocks.ConnectionChannel.Setup(c => c.WriteLine(It.IsAny<string>())).Callback(
                mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("220 .*", RegexOptions.IgnoreCase)));
        }

        [TestMethod]
        public void Process_SmtpServerExceptionThrow_ResponseWritten()
        {
            Mocks mocks = new Mocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Setup(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);
            mockVerb.Setup(v => v.Process(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>())).Throws(new SmtpServerException(new SmtpResponse(500, "error")));

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("GOODCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("500 error", RegexOptions.IgnoreCase)));
        }

        [TestMethod]
        public void Process_EmptyCommand_NoResponse()
        {
            Mocks mocks = new Mocks();

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            //Should only print service ready message
            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.Is<string>(s => !s.StartsWith("220 "))), Times.Never());
        }

        [TestMethod]
        public void Process_GoodCommand_Processed()
        {
            Mocks mocks = new Mocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Setup(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("GOODCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mockVerb.Verify(v => v.Process(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>()));
        }

        [TestMethod]
        public void Process_BadCommand_500Response()
        {
            Mocks mocks = new Mocks();
            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("BADCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("500 .*", RegexOptions.IgnoreCase)));
        }

        [TestMethod]
        public void Process_TooManyBadCommands_Disconnected()
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.SetupGet(b => b.MaximumNumberOfSequentialBadCommands).Returns(2);

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("BADCOMMAND");

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(c => c.ReadLine(), Times.Exactly(2));
            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("221 .*", RegexOptions.IgnoreCase)));
        }

        [TestMethod]
        public void AbortMessage()
        {
            Mocks mocks = new Mocks();

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.NewMessage();

            connection.AbortMessage();
            Assert.IsNull(connection.CurrentMessage);
        }

        [TestMethod]
        public void CommitMessage()
        {
            Mocks mocks = new Mocks();

            Connection connection = new Connection(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            IMessageBuilder messageBuilder = connection.NewMessage();
            IMessage message = messageBuilder.ToMessage();

            connection.CommitMessage();
            mocks.Session.Verify(s => s.AddMessage(message));
            mocks.ServerBehaviour.Verify(b => b.OnMessageReceived(connection, message));
            Assert.IsNull(connection.CurrentMessage);
        }
    }
}