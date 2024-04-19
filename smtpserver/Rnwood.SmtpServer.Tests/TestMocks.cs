// <copyright file="TestMocks.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="TestMocks" />
/// </summary>
public class TestMocks
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMocks" /> class.
    /// </summary>
    public TestMocks()
    {
        Connection = new Mock<IConnection>(MockBehavior.Strict);
        ConnectionChannel = new Mock<IConnectionChannel>(MockBehavior.Strict);
        Session = new Mock<MemorySession>(IPAddress.Loopback, DateTime.Now) { CallBase = true };
        Server = new Mock<ISmtpServer>(MockBehavior.Strict);
        ServerOptions = new Mock<IServerOptions>(MockBehavior.Strict);
        MessageBuilder = new Mock<MemoryMessageBuilder> { CallBase = true };
        VerbMap = new Mock<VerbMap> { CallBase = true };

        ServerOptions.Setup(
            sb => sb.OnCreateNewSession(It.IsAny<IConnectionChannel>())).ReturnsAsync(Session.Object);
        ServerOptions.Setup(s => s.FallbackEncoding).Returns(Encoding.GetEncoding("iso-8859-1"));
        ServerOptions.Setup(sb => sb.OnCreateNewMessage(It.IsAny<IConnection>()))
            .ReturnsAsync(MessageBuilder.Object);
        ServerOptions.Setup(sb => sb.GetExtensions(It.IsAny<IConnectionChannel>()))
            .ReturnsAsync(new IExtension[0]);
        ServerOptions.Setup(sb => sb.OnSessionCompleted(It.IsAny<IConnection>(), It.IsAny<ISession>()))
            .Returns(Task.CompletedTask);
        ServerOptions.SetupGet(sb => sb.DomainName).Returns("tests");
        ServerOptions.Setup(sb => sb.IsSSLEnabled(It.IsAny<IConnection>())).Returns(Task.FromResult(false));
        ServerOptions.Setup(sb => sb.OnSessionStarted(It.IsAny<IConnection>(), It.IsAny<ISession>()))
            .Returns(Task.CompletedTask);
        ServerOptions
            .Setup(sb =>
                sb.OnMessageRecipientAdding(It.IsAny<IConnection>(), It.IsAny<IMessageBuilder>(),
                    It.IsAny<string>())).Returns(Task.CompletedTask);
        ServerOptions.Setup(sb => sb.OnMessageStart(It.IsAny<IConnection>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        ServerOptions.Setup(sb => sb.OnMessageReceived(It.IsAny<IConnection>(), It.IsAny<IMessage>()))
            .Returns(Task.CompletedTask);
        ServerOptions.Setup(sb => sb.OnMessageCompleted(It.IsAny<IConnection>())).Returns(Task.CompletedTask);
        ServerOptions.Setup(sb => sb.OnCommandReceived(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>()))
            .Returns(Task.CompletedTask);
        ServerOptions.SetupGet(sb => sb.MaximumNumberOfSequentialBadCommands).Returns(0);
        ServerOptions
            .Setup(sb =>
                sb.ValidateAuthenticationCredentials(It.IsAny<IConnection>(),
                    It.IsAny<IAuthenticationCredentials>())).Returns(Task.FromResult(AuthenticationResult.Failure));

        Connection.SetupAllProperties();
        Connection.SetupGet(c => c.Session).Returns(Session.Object);
        Connection.SetupGet(c => c.Server).Returns(Server.Object);
        Connection.Setup(s => s.CloseConnection()).Returns(() => ConnectionChannel.Object.Close());
        Connection.SetupGet(s => s.ExtensionProcessors).Returns(new IExtensionProcessor[0]);
        Connection.SetupGet(c => c.VerbMap).Returns(VerbMap.Object);
        Connection.Setup(c => c.WriteResponse(It.IsAny<SmtpResponse>())).Returns(Task.CompletedTask);
        Connection.Setup(c => c.CommitMessage()).Returns(Task.CompletedTask);
        Connection.Setup(c => c.AbortMessage()).Returns(Task.CompletedTask);

        Server.SetupGet(s => s.Options).Returns(ServerOptions.Object);

        bool isConnected = true;
        ConnectionChannel.Setup(s => s.IsConnected).Returns(() => isConnected);
        ConnectionChannel.Setup(s => s.Close()).Returns(() => Task.Run(() => isConnected = false));
        ConnectionChannel.Setup(s => s.ClientIPAddress).Returns(IPAddress.Loopback);
        ConnectionChannel.Setup(s => s.WriteLine(It.IsAny<string>())).Returns(Task.CompletedTask);
        ConnectionChannel.Setup(s => s.Flush()).Returns(Task.CompletedTask);
    }

    /// <summary>
    ///     Gets the Connection
    /// </summary>
    public Mock<IConnection> Connection { get; }

    /// <summary>
    ///     Gets the ConnectionChannel
    /// </summary>
    public Mock<IConnectionChannel> ConnectionChannel { get; }

    /// <summary>
    ///     Gets the MessageBuilder
    /// </summary>
    public Mock<MemoryMessageBuilder> MessageBuilder { get; }

    /// <summary>
    ///     Gets the Server
    /// </summary>
    public Mock<ISmtpServer> Server { get; }

    /// <summary>
    ///     Gets the ServerOptions
    /// </summary>
    public Mock<IServerOptions> ServerOptions { get; }

    /// <summary>
    ///     Gets the Session
    /// </summary>
    public Mock<MemorySession> Session { get; }

    /// <summary>
    ///     Gets the VerbMap
    /// </summary>
    public Mock<VerbMap> VerbMap { get; }

    /// <summary>
    /// </summary>
    /// <param name="responseCode">The responseCode<see cref="StandardSmtpResponseCode" /></param>
    public void VerifyWriteResponse(StandardSmtpResponseCode responseCode, Times times) =>
        Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r => r.Code == (int)responseCode)), times);

    public void VerifyWriteResponse(StandardSmtpResponseCode responseCode) =>
        VerifyWriteResponse(responseCode, Times.Once());
}
