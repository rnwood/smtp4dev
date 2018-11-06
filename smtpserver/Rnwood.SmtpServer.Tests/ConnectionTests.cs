// <copyright file="ConnectionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Moq;
    using Rnwood.SmtpServer.Verbs;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="ConnectionTests" />
    /// </summary>
    public class ConnectionTests
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task AbortMessage()
        {
            TestMocks mocks = new TestMocks();

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.NewMessage().ConfigureAwait(false);

            await connection.AbortMessage().ConfigureAwait(false);
            Assert.Null(connection.CurrentMessage);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task CommitMessage()
        {
            TestMocks mocks = new TestMocks();

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            IMessageBuilder messageBuilder = await connection.NewMessage().ConfigureAwait(false);
            IMessage message = await messageBuilder.ToMessage().ConfigureAwait(false);

            await connection.CommitMessage().ConfigureAwait(false);
            mocks.Session.Verify(s => s.AddMessage(message));
            mocks.ServerBehaviour.Verify(b => b.OnMessageReceived(connection, message));
            Assert.Null(connection.CurrentMessage);
        }

        /// <summary>
        /// The Process_BadCommand_500Response
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_BadCommand_500Response()
        {
            TestMocks mocks = new TestMocks();
            mocks.ConnectionChannel.Setup(c => c.ReadLine()).ReturnsAsync("BADCOMMAND").Callback(() => mocks.Connection.Object.CloseConnection().Wait());

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.ProcessAsync().ConfigureAwait(false);

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("500 .*", RegexOptions.IgnoreCase)));
        }

        /// <summary>
        /// The Process_EmptyCommand_NoResponse
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_EmptyCommand_NoResponse()
        {
            TestMocks mocks = new TestMocks();

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).ReturnsAsync("").Callback(() => mocks.Connection.Object.CloseConnection().Wait());

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.ProcessAsync().ConfigureAwait(false);

            // Should only print service ready message
            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.Is<string>(s => !s.StartsWith("220 ", StringComparison.OrdinalIgnoreCase))), Times.Never());
        }

        /// <summary>
        /// The Process_GoodCommand_Processed
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_GoodCommand_Processed()
        {
            TestMocks mocks = new TestMocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Setup(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object).Callback(() => mocks.Connection.Object.CloseConnection().Wait());

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).ReturnsAsync("GOODCOMMAND");

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.ProcessAsync().ConfigureAwait(false);

            mockVerb.Verify(v => v.Process(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>()));
        }

        /// <summary>
        /// The Process_GreetingWritten
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_GreetingWritten()
        {
            TestMocks mocks = new TestMocks();
            mocks.ConnectionChannel.Setup(c => c.WriteLine(It.IsAny<string>())).Callback(() => mocks.Connection.Object.CloseConnection().Wait());

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.ProcessAsync().ConfigureAwait(false);

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("220 .*", RegexOptions.IgnoreCase)));
        }

        /// <summary>
        /// The Process_SmtpServerExceptionThrow_ResponseWritten
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_SmtpServerExceptionThrow_ResponseWritten()
        {
            TestMocks mocks = new TestMocks();
            Mock<IVerb> mockVerb = new Mock<IVerb>();
            mocks.VerbMap.Setup(v => v.GetVerbProcessor(It.IsAny<string>())).Returns(mockVerb.Object);
            mockVerb.Setup(v => v.Process(It.IsAny<IConnection>(), It.IsAny<SmtpCommand>())).Returns(Task.FromException(new SmtpServerException(new SmtpResponse(500, "error"))));

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).ReturnsAsync("GOODCOMMAND").Callback(() => mocks.Connection.Object.CloseConnection().Wait());

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.ProcessAsync().ConfigureAwait(false);

            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("500 error", RegexOptions.IgnoreCase)));
        }

        /// <summary>
        /// The Process_TooManyBadCommands_Disconnected
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_TooManyBadCommands_Disconnected()
        {
            TestMocks mocks = new TestMocks();
            mocks.ServerBehaviour.SetupGet(b => b.MaximumNumberOfSequentialBadCommands).Returns(2);

            mocks.ConnectionChannel.Setup(c => c.ReadLine()).ReturnsAsync("BADCOMMAND");

            Connection connection = await Connection.Create(mocks.Server.Object, mocks.ConnectionChannel.Object, mocks.VerbMap.Object).ConfigureAwait(false);
            await connection.ProcessAsync().ConfigureAwait(false);

            mocks.ConnectionChannel.Verify(c => c.ReadLine(), Times.Exactly(2));
            mocks.ConnectionChannel.Verify(cc => cc.WriteLine(It.IsRegex("221 .*", RegexOptions.IgnoreCase)));
        }
    }
}
