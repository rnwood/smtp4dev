// <copyright file="MailFromVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests.Verbs
{
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="MailFromVerbTests" />
    /// </summary>
    public class MailFromVerbTests
    {
        /// <summary>
        /// The Process_Address_Bracketed
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_Address_Bracketed()
        {
            await this.Process_AddressAsync("<rob@rnwood.co.uk>", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK).ConfigureAwait(false);
        }

        /// <summary>
        /// The Process_Address_BracketedWithName
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_Address_BracketedWithName()
        {
            await this.Process_AddressAsync("<Robert Wood <rob@rnwood.co.uk>>", "Robert Wood <rob@rnwood.co.uk>", StandardSmtpResponseCode.OK).ConfigureAwait(false);
        }

        /// <summary>
        /// The Process_Address_Plain
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_Address_Plain()
        {
            await this.Process_AddressAsync("rob@rnwood.co.uk", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK).ConfigureAwait(false);
        }

        /// <summary>
        /// The Process_AlreadyGivenFrom_ErrorResponse
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_AlreadyGivenFrom_ErrorResponse()
        {
            TestMocks mocks = new TestMocks();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(new Mock<IMessageBuilder>().Object);

            MailFromVerb mailFromVerb = new MailFromVerb();
            await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM <foo@bar.com>")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        /// <summary>
        /// The Process_MissingAddress_ErrorResponse
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Process_MissingAddress_ErrorResponse()
        {
            TestMocks mocks = new TestMocks();

            MailFromVerb mailFromVerb = new MailFromVerb();
            await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
        }

        /// <summary>
        /// The Process_AddressAsync
        /// </summary>
        /// <param name="address">The address<see cref="string"/></param>
        /// <param name="expectedParsedAddress">The expectedParsedAddress<see cref="string"/></param>
        /// <param name="expectedResponse">The expectedResponse<see cref="StandardSmtpResponseCode"/></param>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        private async Task Process_AddressAsync(string address, string expectedParsedAddress, StandardSmtpResponseCode expectedResponse)
        {
            TestMocks mocks = new TestMocks();
            Mock<IMessageBuilder> message = new Mock<IMessageBuilder>();
            IMessageBuilder currentMessage = null;
            mocks.Connection.Setup(c => c.NewMessage()).ReturnsAsync(() =>
            {
                currentMessage = message.Object;
                return currentMessage;
            });
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(() => currentMessage);

            MailFromVerb mailFromVerb = new MailFromVerb();
            await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM " + address)).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(expectedResponse);
            message.VerifySet(m => m.From = expectedParsedAddress);
        }
    }
}
