using Moq;
using Rnwood.SmtpServer.Verbs;
using System;
using System.Net;

namespace Rnwood.SmtpServer.Tests
{
    public class Mocks
    {
        public Mocks()
        {
            Connection = new Mock<IConnection>();
            ConnectionChannel = new Mock<IConnectionChannel>();
            Session = new Mock<IEditableSession>();
            Server = new Mock<IServer>();
            ServerBehaviour = new Mock<IServerBehaviour>();
            Message = new Mock<IMessage>();
            VerbMap = new Mock<IVerbMap>();

            ServerBehaviour.Setup(
                sb => sb.OnCreateNewSession(It.IsAny<IConnection>(), It.IsAny<IPAddress>(), It.IsAny<DateTime>())).
                Returns(Session.Object);
            Connection.SetupGet(c => c.Session).Returns(Session.Object);
            Connection.SetupGet(c => c.Server).Returns(Server.Object);
            Connection.SetupGet(c => c.ReaderEncoding).Returns(new ASCIISevenBitTruncatingEncoding());
            Server.SetupGet(s => s.Behaviour).Returns(ServerBehaviour.Object);

            bool isConnected = true;
            ConnectionChannel.Setup(s => s.IsConnected).Returns(() => isConnected);
            ConnectionChannel.Setup(s => s.Close()).Callback(() => isConnected = false);
        }

        public Mock<IConnection> Connection { get; private set; }
        public Mock<IConnectionChannel> ConnectionChannel { get; private set; }
        public Mock<IEditableSession> Session { get; private set; }
        public Mock<IServer> Server { get; private set; }
        public Mock<IServerBehaviour> ServerBehaviour { get; private set; }
        public Mock<IMessage> Message { get; private set; }

        public Mock<IVerbMap> VerbMap { get; private set; }

        public void VerifyWriteResponse(StandardSmtpResponseCode responseCode)
        {
            Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)responseCode)));
        }
    }
}