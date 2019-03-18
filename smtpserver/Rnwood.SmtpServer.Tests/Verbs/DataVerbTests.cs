// <copyright file="DataVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests.Verbs
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="DataVerbTests" />
    /// </summary>
    public class DataVerbTests
    {
        /// <summary>
        /// The Data_8BitData_PassedThrough
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_8BitData_PassedThrough()
        {
            string data = ((char)(0x41 + 128)).ToString();
            await this.TestGoodDataAsync(new string[] { data, "." }, data, true).ConfigureAwait(false);
        }

        /// <summary>
        /// The Data_AboveSizeLimit_Rejected
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_AboveSizeLimit_Rejected()
        {
            TestMocks mocks = new TestMocks();

            MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);

            string[] messageData = new string[] { new string('x', 11), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.ExceededStorageAllocation);
        }

        /// <summary>
        /// The Data_DoubleDots_Unescaped
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_DoubleDots_Unescaped()
        {
            //Check escaping of end of message character ".." is decoded to "."
            //but the .. after B should be left alone
            await this.TestGoodDataAsync(new string[] { "A", "..", "B..", "." }, "A\r\n.\r\nB..", true).ConfigureAwait(false);
        }

        /// <summary>
        /// The Data_EmptyMessage_Accepted
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_EmptyMessage_Accepted()
        {
            await this.TestGoodDataAsync(new string[] { "." }, "", true).ConfigureAwait(false);
        }

        /// <summary>
        /// The Data_ExactlySizeLimit_Accepted
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_ExactlySizeLimit_Accepted()
        {
            TestMocks mocks = new TestMocks();

            MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);

            string[] messageData = new string[] { new string('x', 10), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
        }

        /// <summary>
        /// The Data_NoCurrentMessage_ReturnsError
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_NoCurrentMessage_ReturnsError()
        {
            TestMocks mocks = new TestMocks();

            DataVerb verb = new DataVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        /// <summary>
        /// The Data_WithinSizeLimit_Accepted
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Data_WithinSizeLimit_Accepted()
        {
            TestMocks mocks = new TestMocks();

            MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);

            string[] messageData = new string[] { new string('x', 9), "." };
            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="messageData">The messageData<see cref="string"/></param>
        /// <param name="expectedData">The expectedData<see cref="string"/></param>
        /// <param name="eightBitClean">The eightBitClean<see cref="bool"/></param>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        private async Task TestGoodDataAsync(string[] messageData, string expectedData, bool eightBitClean)
        {
            TestMocks mocks = new TestMocks();

            if (eightBitClean)
            {
                mocks.Connection.SetupGet(c => c.ReaderEncoding).Returns(Encoding.UTF8);
            }

            MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
            mocks.ServerBehaviour.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync((long?)null);

            int messageLine = 0;
            mocks.Connection.Setup(c => c.ReadLine()).Returns(() => Task.FromResult(messageData[messageLine++]));

            DataVerb verb = new DataVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.StartMailInputEndWithDot);
            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);

            using (StreamReader dataReader = new StreamReader(await messageBuilder.GetData().ConfigureAwait(false), eightBitClean ? Encoding.UTF8 : new ASCIISevenBitTruncatingEncoding()))
            {
                Assert.Equal(expectedData, dataReader.ReadToEnd());
            }
        }
    }
}
