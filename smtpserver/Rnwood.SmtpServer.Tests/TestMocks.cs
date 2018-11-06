// <copyright file="TestMocks.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System.Net;
    using System.Threading.Tasks;
    using Moq;
    using Rnwood.SmtpServer.Extensions;
    using Rnwood.SmtpServer.Verbs;

    /// <summary>
    /// Defines the <see cref="TestMocks" />
    /// </summary>
    public class TestMocks
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestMocks"/> class.
        /// </summary>
        public TestMocks()
        {
            this.Connection = new Mock<IConnection>();
            this.ConnectionChannel = new Mock<IConnectionChannel>();
            this.Session = new Mock<IEditableSession>();
            this.Server = new Mock<ISmtpServer>();
            this.ServerBehaviour = new Mock<IServerBehaviour>();
            this.MessageBuilder = new Mock<IMessageBuilder>();
            this.VerbMap = new Mock<IVerbMap>();

            this.ServerBehaviour.Setup(
                sb => sb.OnCreateNewSession(It.IsAny<IConnectionChannel>())).
                ReturnsAsync(this.Session.Object);
            this.ServerBehaviour.Setup(sb => sb.OnCreateNewMessage(It.IsAny<IConnection>())).ReturnsAsync(this.MessageBuilder.Object);

            this.Connection.SetupGet(c => c.Session).Returns(this.Session.Object);
            this.Connection.SetupGet(c => c.Server).Returns(this.Server.Object);
            this.Connection.SetupGet(c => c.ReaderEncoding).Returns(new ASCIISevenBitTruncatingEncoding());
            this.Connection.Setup(s => s.CloseConnection()).Returns(() => this.ConnectionChannel.Object.Close());
            this.Connection.SetupGet(s => s.ExtensionProcessors).Returns(new IExtensionProcessor[0]);

            this.Server.SetupGet(s => s.Behaviour).Returns(this.ServerBehaviour.Object);

            bool isConnected = true;
            this.ConnectionChannel.Setup(s => s.IsConnected).Returns(() => isConnected);
            this.ConnectionChannel.Setup(s => s.Close()).Returns(() => Task.Run(() => isConnected = false));
            this.ConnectionChannel.Setup(s => s.ClientIPAddress).Returns(IPAddress.Loopback);
        }

        /// <summary>
        /// Gets the Connection
        /// </summary>
        public Mock<IConnection> Connection { get; private set; }

        /// <summary>
        /// Gets the ConnectionChannel
        /// </summary>
        public Mock<IConnectionChannel> ConnectionChannel { get; private set; }

        /// <summary>
        /// Gets the MessageBuilder
        /// </summary>
        public Mock<IMessageBuilder> MessageBuilder { get; private set; }

        /// <summary>
        /// Gets the Server
        /// </summary>
        public Mock<ISmtpServer> Server { get; private set; }

        /// <summary>
        /// Gets the ServerBehaviour
        /// </summary>
        public Mock<IServerBehaviour> ServerBehaviour { get; private set; }

        /// <summary>
        /// Gets the Session
        /// </summary>
        public Mock<IEditableSession> Session { get; private set; }

        /// <summary>
        /// Gets the VerbMap
        /// </summary>
        public Mock<IVerbMap> VerbMap { get; private set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="responseCode">The responseCode<see cref="StandardSmtpResponseCode"/></param>
        public void VerifyWriteResponseAsync(StandardSmtpResponseCode responseCode)
        {
            this.Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)responseCode)));
        }
    }
}
