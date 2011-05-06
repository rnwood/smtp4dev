using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;
using MbUnit.Framework;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class ConnectionTests
    {

        [Test]
        public void Process_GreetingWritten()
        {
            Mocks mocks = new Mocks();
            mocks.ConnectionChannel.Setup(c => c.WriteLine(It.IsAny<string>())).Callback(
                mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("220 .*", RegexOptions.IgnoreCase)));
        }

        [Test]
        public void Process_SmtpServerExceptionThrow_ResponseWritten()
        {
            Mocks mocks = new Mocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Expect(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);
            mockVerb.Setup(v => v.Process(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>())).Throws(new SmtpServerException(new SmtpResponse(500, "error")));

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("GOODCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("500 error", RegexOptions.IgnoreCase)));
        }

        [Test]
        public void Process_EmptyCommand_NoResponse()
        {
            Mocks mocks = new Mocks();

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            //Should only print service ready message
            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.Is<string>(s => !s.StartsWith("220 "))), Times.Never());
        }

        [Test]
        public void Process_GoodCommand_Processed()
        {
            Mocks mocks = new Mocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Expect(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("GOODCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object, mocks.VerbMap.Object);
            connection.Process();

            mockVerb.Verify(v => v.Process(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>()));
        }

        [Test]
        public void Process_BadCommand_500Response()
        {
            Mocks mocks = new Mocks();
            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("BADCOMMAND").Callback(mocks.ConnectionChannel.Object.Close);

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("500 .*", RegexOptions.IgnoreCase)));
        }

        [Test]
        public void Process_TooManyBadCommands_Disconnected()
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.SetupGet(b => b.MaximumNumberOfSequentialBadCommands).Returns(2);

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("a");
            mocks.ConnectionChannel.Setup(c => c.ReadLine()).Returns("b");

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object);
            connection.Process();

            mocks.ConnectionChannel.Verify(c => c.ReadLine(), Times.Exactly(2));
            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("221 .*", RegexOptions.IgnoreCase)));
        }

        [Test]
        public void AbortMessage()
        {
            Mocks mocks = new Mocks();

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object);
            connection.NewMessage();

            connection.AbortMessage();
            Assert.IsNull(connection.CurrentMessage);
        }

        [Test]
        public void CommitMessage()
        {
            Mocks mocks = new Mocks();

            Connection connection = new Connection(mocks.Server.Object, c => mocks.ConnectionChannel.Object);
            IEditableMessage message = connection.NewMessage();

            connection.CommitMessage();
            mocks.Session.Verify(s => s.AddMessage(message));
            mocks.ServerBehaviour.Verify(b => b.OnMessageReceived(connection, message));
            Assert.IsNull(connection.CurrentMessage);
        }
    }
}
