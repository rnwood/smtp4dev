﻿// <copyright file="CramMd5MechanismProcessorTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    using System;
    using System.Threading.Tasks;
    using Moq;
    using Rnwood.SmtpServer.Extensions.Auth;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="CramMd5MechanismProcessorTests" />
    /// </summary>
    public class CramMd5MechanismProcessorTests : AuthMechanismTest
    {
        /// <summary>
        /// Defines the FAKEDATETIME
        /// </summary>
        private const int FAKEDATETIME = 10000;

        /// <summary>
        /// Defines the FAKEDOMAIN
        /// </summary>
        private const string FAKEDOMAIN = "mockdomain";

        /// <summary>
        /// Defines the FAKERANDOM
        /// </summary>
        private const int FAKERANDOM = 1234;

        /// <summary>
        /// The ProcessRepsonse_ChallengeReponse_BadFormat
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task ProcessRepsonse_ChallengeReponse_BadFormat()
        {
            await Assert.ThrowsAsync<SmtpServerException>(async () =>
            {
                TestMocks mocks = new TestMocks();

                string challenge = string.Format("{0}.{1}@{2}", FAKERANDOM, FAKEDATETIME, FAKEDOMAIN);

                CramMd5MechanismProcessor cramMd5MechanismProcessor = this.Setup(mocks, challenge);
                AuthMechanismProcessorStatus result = await cramMd5MechanismProcessor.ProcessResponse("BLAH").ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// The ProcessRepsonse_GetChallenge
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task ProcessRepsonse_GetChallenge()
        {
            TestMocks mocks = new TestMocks();

            CramMd5MechanismProcessor cramMd5MechanismProcessor = this.Setup(mocks);
            AuthMechanismProcessorStatus result = await cramMd5MechanismProcessor.ProcessResponse(null).ConfigureAwait(false);

            string expectedResponse = string.Format("{0}.{1}@{2}", FAKERANDOM, FAKEDATETIME, FAKEDOMAIN);

            Assert.Equal(AuthMechanismProcessorStatus.Continue, result);
            mocks.Connection.Verify(
                    c => c.WriteResponse(
                        It.Is<SmtpResponse>(r =>
                            r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue &&
                            VerifyBase64Response(r.Message, expectedResponse)
                        )
                    )
                );
        }

        /// <summary>
        /// The ProcessResponse_Response_BadBase64
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task ProcessResponse_Response_BadBase64()
        {
            await Assert.ThrowsAsync<BadBase64Exception>(async () =>
            {
                TestMocks mocks = new TestMocks();

                CramMd5MechanismProcessor cramMd5MechanismProcessor = this.Setup(mocks);
                await cramMd5MechanismProcessor.ProcessResponse(null).ConfigureAwait(false);
                await cramMd5MechanismProcessor.ProcessResponse("rob blah").ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mocks">The mocks<see cref="TestMocks"/></param>
        /// <param name="challenge">The challenge<see cref="string"/></param>
        /// <returns>The <see cref="CramMd5MechanismProcessor"/></returns>
        private CramMd5MechanismProcessor Setup(TestMocks mocks, string challenge = null)
        {
            Mock<IRandomIntegerGenerator> randomMock = new Mock<IRandomIntegerGenerator>();
            randomMock.Setup(r => r.GenerateRandomInteger(It.IsAny<int>(), It.IsAny<int>())).Returns(FAKERANDOM);

            Mock<ICurrentDateTimeProvider> dateMock = new Mock<ICurrentDateTimeProvider>();
            dateMock.Setup(d => d.GetCurrentDateTime()).Returns(new DateTime(FAKEDATETIME));

            mocks.ServerBehaviour.SetupGet(b => b.DomainName).Returns(FAKEDOMAIN);

            return new CramMd5MechanismProcessor(mocks.Connection.Object, randomMock.Object, dateMock.Object, challenge);
        }
    }
}
