using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests
{
    public class Mocks
    {
        public Mocks()
        {
            Connection = new Mock<IConnection>();
            Session = new Mock<IEditableSession>();
            Server = new Mock<IServer>();
            ServerBehaviour = new Mock<IServerBehaviour>();
            ConnectionChannel = new Mock<IConnectionChannel>();
            VerbMap = new Mock<IVerbMap>();
            Message = new Mock<IEditableMessage>();

            Session.SetupProperty(s => s.ClientName);

            Connection.SetupGet(c => c.Session).Returns(Session.Object);
            Connection.SetupGet(c => c.Server).Returns(Server.Object);
            Connection.SetupGet(c => c.Channel).Returns(ConnectionChannel.Object);

            ConnectionChannel.SetupGet(c => c.ReaderEncoding).Returns(new ASCIISevenBitTruncatingEncoding());
            bool connectionOpen = true;
            ConnectionChannel.SetupGet(c => c.IsConnected).Returns(() => connectionOpen);
            ConnectionChannel.Setup(c => c.Close()).Callback(() => connectionOpen = false);

            Server.SetupGet(s => s.Behaviour).Returns(ServerBehaviour.Object);

            ServerBehaviour.SetupGet(s => s.MaximumNumberOfSequentialBadCommands).Returns(int.MaxValue);
            ServerBehaviour.Setup(
                s => s.OnCreateNewSession(It.IsAny<IConnection>(), It.IsAny<IPAddress>(), It.IsAny<DateTime>())).Returns(Session.Object);
            ServerBehaviour.Setup(s => s.OnCreateNewMessage(It.IsAny<IConnection>())).Returns(Message.Object);
        }

        public Mock<IConnection> Connection { get; private set; }
        public Mock<IEditableSession> Session { get; private set; }
        public Mock<IServer> Server { get; private set; }
        public Mock<IServerBehaviour> ServerBehaviour { get; private set; }
        public Mock<IConnectionChannel> ConnectionChannel { get; private set; }
        public Mock<IVerbMap> VerbMap { get; private set; }
        public Mock<IEditableMessage> Message { get; private set; }

        public void VerifyWriteResponse(StandardSmtpResponseCode responseCode)
        {
            Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)responseCode)));
        }
    }
}
