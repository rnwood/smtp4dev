// <copyright file="RcptToVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests.Verbs
{
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="RcptToVerbTests" />
    /// </summary>
    public class RcptToVerbTests
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task EmailAddressOnly()
        {
            await this.TestGoodAddressAsync("<rob@rnwood.co.uk>", "rob@rnwood.co.uk").ConfigureAwait(false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task EmailAddressWithDisplayName()
        {
            //Should this format be accepted????
            await this.TestGoodAddressAsync("<Robert Wood<rob@rnwood.co.uk>>", "Robert Wood<rob@rnwood.co.uk>").ConfigureAwait(false);
        }

        /// <summary>
        /// The EmptyAddress_ReturnsError
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task EmptyAddress_ReturnsError()
        {
            await this.TestBadAddressAsync("<>").ConfigureAwait(false);
        }

        /// <summary>
        /// The MismatchedBraket_ReturnsError
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task MismatchedBraket_ReturnsError()
        {
            await this.TestBadAddressAsync("<rob@rnwood.co.uk").ConfigureAwait(false);
            await this.TestBadAddressAsync("<Robert Wood<rob@rnwood.co.uk>").ConfigureAwait(false);
        }

        /// <summary>
        /// The UnbraketedAddress_ReturnsError
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task UnbraketedAddress_ReturnsError()
        {
            await this.TestBadAddressAsync("rob@rnwood.co.uk").ConfigureAwait(false);
        }

        [Fact]
        public async Task NonAsciiAddress_ReturnsError()
        {
            await this.TestBadAddressAsync("<ظػؿقط <rob@rnwood.co.uk>>", asException: true).ConfigureAwait(false);
        }

        [Fact]
        public async Task NonAsciiAddress_SmtpUtf8_Accepted()
        {
            await this.TestGoodAddressAsync("<ظػؿقط <rob@rnwood.co.uk>>", "ظػؿقط <rob@rnwood.co.uk>", eightBit: true).ConfigureAwait(false);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="address">The address<see cref="string"/></param>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        private async Task TestBadAddressAsync(string address, bool asException=false)
        {
            TestMocks mocks = new TestMocks();
            MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

            RcptToVerb verb = new RcptToVerb();

            if (!asException)
            {
                await verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address)).ConfigureAwait(false);
                mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
            }
            else
            {
                SmtpServerException e = await Assert.ThrowsAsync<SmtpServerException>(() => verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address))).ConfigureAwait(false);
                Assert.Equal((int) StandardSmtpResponseCode.SyntaxErrorInCommandArguments, e.SmtpResponse.Code);
            }
            Assert.Equal(0, messageBuilder.Recipients.Count);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="address">The address<see cref="string"/></param>
        /// <param name="expectedAddress">The expectedAddress<see cref="string"/></param>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        private async Task TestGoodAddressAsync(string address, string expectedAddress, bool eightBit = false)
        {
            TestMocks mocks = new TestMocks();
            MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
            messageBuilder.EightBitTransport = eightBit;
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

            RcptToVerb verb = new RcptToVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address)).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
            Assert.Equal(expectedAddress, messageBuilder.Recipients.First());
        }
    }
}
