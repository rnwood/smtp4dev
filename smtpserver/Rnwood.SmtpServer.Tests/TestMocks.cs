// <copyright file="TestMocks.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
	using System;
	using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Moq;
    using Rnwood.SmtpServer.Extensions;
	using Rnwood.SmtpServer.Extensions.Auth;
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
            this.Connection = new Mock<IConnection>(MockBehavior.Strict);
            this.ConnectionChannel = new Mock<IConnectionChannel>(MockBehavior.Strict);
            this.Session = new Mock<MemorySession>(IPAddress.Loopback, DateTime.Now) { CallBase = true };
            this.Server = new Mock<ISmtpServer>(MockBehavior.Strict);
            this.ServerBehaviour = new Mock<IServerBehaviour>(MockBehavior.Strict);
            this.MessageBuilder = new Mock<MemoryMessageBuilder>() { CallBase = true };
            this.VerbMap = new Mock<VerbMap>() { CallBase = true };

            this.ServerBehaviour.Setup(
                sb => sb.OnCreateNewSession(It.IsAny<IConnectionChannel>())).
                ReturnsAsync(this.Session.Object);
            this.ServerBehaviour.Setup(s => s.FallbackEncoding).Returns(Encoding.GetEncoding("iso-8859-1"));
            this.ServerBehaviour.Setup(sb => sb.OnCreateNewMessage(It.IsAny<IConnection>())).ReturnsAsync(this.MessageBuilder.Object);
			this.ServerBehaviour.Setup(sb => sb.GetExtensions(It.IsAny<IConnectionChannel>())).ReturnsAsync(new IExtension[0]);
			this.ServerBehaviour.Setup(sb => sb.OnSessionCompleted(It.IsAny<IConnection>(), It.IsAny<ISession>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.SetupGet(sb => sb.DomainName).Returns("tests");
			this.ServerBehaviour.Setup(sb => sb.IsSSLEnabled(It.IsAny<IConnection>())).Returns(Task.FromResult(false));
			this.ServerBehaviour.Setup(sb => sb.OnSessionStarted(It.IsAny<IConnection>(), It.IsAny<ISession>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.Setup(sb => sb.OnMessageRecipientAdding(It.IsAny<IConnection>(), It.IsAny<IMessageBuilder>(), It.IsAny<string>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.Setup(sb => sb.OnMessageStart(It.IsAny<IConnection>(), It.IsAny<string>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.Setup(sb => sb.OnMessageReceived(It.IsAny<IConnection>(), It.IsAny<IMessage>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.Setup(sb => sb.OnMessageCompleted(It.IsAny<IConnection>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.Setup(sb => sb.OnCommandReceived(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>())).Returns(Task.CompletedTask);
			this.ServerBehaviour.SetupGet(sb => sb.MaximumNumberOfSequentialBadCommands).Returns(0);
			this.ServerBehaviour.Setup(sb => sb.ValidateAuthenticationCredentials(It.IsAny<IConnection>(), It.IsAny<IAuthenticationCredentials>())).Returns(Task.FromResult(AuthenticationResult.Failure));

			this.Connection.SetupAllProperties();
            this.Connection.SetupGet(c => c.Session).Returns(this.Session.Object);
            this.Connection.SetupGet(c => c.Server).Returns(this.Server.Object);
            this.Connection.Setup(s => s.CloseConnection()).Returns(() => this.ConnectionChannel.Object.Close());
            this.Connection.SetupGet(s => s.ExtensionProcessors).Returns(new IExtensionProcessor[0]);
            this.Connection.SetupGet(c => c.VerbMap).Returns(this.VerbMap.Object);
			this.Connection.Setup(c => c.WriteResponse(It.IsAny<SmtpResponse>())).Returns(Task.CompletedTask);
			this.Connection.Setup(c => c.CommitMessage()).Returns(Task.CompletedTask);

            this.Server.SetupGet(s => s.Behaviour).Returns(this.ServerBehaviour.Object);

            bool isConnected = true;
            this.ConnectionChannel.Setup(s => s.IsConnected).Returns(() => isConnected);
            this.ConnectionChannel.Setup(s => s.Close()).Returns(() => Task.Run(() => isConnected = false));
            this.ConnectionChannel.Setup(s => s.ClientIPAddress).Returns(IPAddress.Loopback);
			this.ConnectionChannel.Setup(s => s.WriteLine(It.IsAny<string>())).Returns(Task.CompletedTask);
			this.ConnectionChannel.Setup(s => s.Flush()).Returns(Task.CompletedTask);

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
        public Mock<MemoryMessageBuilder> MessageBuilder { get; private set; }

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
        public Mock<MemorySession> Session { get; private set; }

        /// <summary>
        /// Gets the VerbMap
        /// </summary>
        public Mock<VerbMap> VerbMap { get; private set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="responseCode">The responseCode<see cref="StandardSmtpResponseCode"/></param>
        public void VerifyWriteResponse(StandardSmtpResponseCode responseCode, Times times)
        {
            this.Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)responseCode)), times);
        }

		public void VerifyWriteResponse(StandardSmtpResponseCode responseCode)
		{
			VerifyWriteResponse(responseCode, Times.Once());
		}

	}
}
