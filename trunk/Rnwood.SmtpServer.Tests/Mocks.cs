using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;

namespace Rnwood.SmtpServer.Tests
{
    public class Mocks
    {
        public Mocks()
        {
            Connection = new Mock<IConnection>();
            Session = new Mock<ISession>();
            Server = new Mock<IServer>();
            ServerBehaviour = new Mock<IServerBehaviour>();

            Connection.SetupGet(c => c.Session).Returns(Session.Object);
            Connection.SetupGet(c => c.Server).Returns(Server.Object);
            Connection.SetupGet(c => c.ReaderEncoding).Returns(new ASCIISevenBitTruncatingEncoding());
            Server.SetupGet(s => s.Behaviour).Returns(ServerBehaviour.Object);
        }

        public Mock<IConnection> Connection { get; private set; }
        public Mock<ISession> Session { get; private set; }
        public Mock<IServer> Server { get; private set; }
        public Mock<IServerBehaviour> ServerBehaviour { get; private set; }

        public void VerifyWriteResponse(StandardSmtpResponseCode responseCode)
        {
            Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)responseCode)));
        }
    }
}
