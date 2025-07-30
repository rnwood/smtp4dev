// <copyright file="XOAuth2MechanismProcessorTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>


using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class XOAuth2MechanismProcessorTests : AuthMechanismTest

    {

        [Fact]
        public async Task ProcessResponse_BadBase64()
        {
            var mocks = new TestMocks();
            XOauth2MechanismProcessor processor = Setup(mocks);

            var exception = await Assert.ThrowsAsync<BadBase64Exception>(async () =>
            {
                await processor.ProcessResponse(null);
                await processor.ProcessResponse("not base64");
            });

            Assert.Equal((int)StandardSmtpResponseCode.AuthenticationFailure, exception.SmtpResponse.Code);
        }

        [Fact]
        public async Task ProcessResponse_InvalidFormat()
        {
            var mocks = new TestMocks();
            XOauth2MechanismProcessor processor = Setup(mocks);

            var exception = await Assert.ThrowsAsync<SmtpServerException>(async () =>
            {
                await processor.ProcessResponse(null);
                await processor.ProcessResponse(EncodeBase64("an invalid format string"));
            });

            Assert.Equal((int)StandardSmtpResponseCode.AuthenticationFailure, exception.SmtpResponse.Code);
        }

        [Fact]
        public async Task ProcessResponse_EmptyUser()
        {
            var mocks = new TestMocks();
            XOauth2MechanismProcessor processor = Setup(mocks);

            var exception = await Assert.ThrowsAsync<SmtpServerException>(async () =>
            {
                await processor.ProcessResponse(null);
                await processor.ProcessResponse(EncodeBase64("user=\u0001auth=Bearer a_token\u0001\u0001"));
            });

            Assert.Equal((int)StandardSmtpResponseCode.AuthenticationFailure, exception.SmtpResponse.Code);
        }

        [Fact]
        public async Task ProcessResponse_EmptyToken()
        {
            var mocks = new TestMocks();
            XOauth2MechanismProcessor processor = Setup(mocks);

            var exception = await Assert.ThrowsAsync<SmtpServerException>(async () =>
            {
                await processor.ProcessResponse(null);
                await processor.ProcessResponse(EncodeBase64("user=an_user\u0001auth=\u0001\u0001"));
            });

            Assert.Equal((int)StandardSmtpResponseCode.AuthenticationFailure, exception.SmtpResponse.Code);
        }

        [Fact]
        public async Task ProcessResponse_WellFormed()
        {
            var mocks = new TestMocks();
            XOauth2MechanismProcessor processor = Setup(mocks);
            mocks.ServerOptions
                .Setup(sb =>
                    sb.ValidateAuthenticationCredentials(It.IsAny<IConnection>(),
                        It.IsAny<IAuthenticationCredentials>())).Returns(Task.FromResult(AuthenticationResult.Success));

            await processor.ProcessResponse(null);
            var result =
                await processor.ProcessResponse(EncodeBase64("user=an_user\u0001auth=Bearer a_token\u0001\u0001"));

            Assert.Equal(AuthMechanismProcessorStatus.Success, result);
        }






        /// <summary>
        /// </summary>
        /// <param name="mocks">The mocks<see cref="TestMocks" /></param>
        /// <returns>The <see cref="XOauth2MechanismProcessor" /></returns>
        private XOauth2MechanismProcessor Setup(TestMocks mocks)
        {
            return new XOauth2MechanismProcessor(mocks.Connection.Object);
        }

    }
}
