// <copyright file="LoginMechanismProcessorTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth;

/// <summary>
///     Defines the <see cref="LoginMechanismProcessorTests" />
/// </summary>
public class LoginMechanismProcessorTests : AuthMechanismTest
{
    /// <summary>
    ///     The ProcessRepsonse_NoUsername_GetUsernameChallenge
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessRepsonse_NoUsername_GetUsernameChallenge()
    {
        TestMocks mocks = new TestMocks();

        LoginMechanismProcessor processor = Setup(mocks);
        AuthMechanismProcessorStatus result = await processor.ProcessResponse(null).ConfigureAwait(false);

        Assert.Equal(AuthMechanismProcessorStatus.Continue, result);
        mocks.Connection.Verify(c =>
            c.WriteResponse(
                It.Is<SmtpResponse>(r =>
                    r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue &&
                    VerifyBase64Response(r.Message, "Username:")
                )
            )
        );
    }

    /// <summary>
    ///     The ProcessRepsonse_Username_GetPasswordChallenge
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessRepsonse_Username_GetPasswordChallenge()
    {
        TestMocks mocks = new TestMocks();

        LoginMechanismProcessor processor = Setup(mocks);
        AuthMechanismProcessorStatus
            result = await processor.ProcessResponse(EncodeBase64("rob")).ConfigureAwait(false);

        Assert.Equal(AuthMechanismProcessorStatus.Continue, result);

        mocks.Connection.Verify(c =>
            c.WriteResponse(
                It.Is<SmtpResponse>(r =>
                    VerifyBase64Response(r.Message, "Password:")
                    && r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue
                )
            )
        );
    }

    /// <summary>
    ///     The ProcessResponse_PasswordAcceptedAfterUserNameInInitialRequest
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessResponse_PasswordAcceptedAfterUserNameInInitialRequest()
    {
        TestMocks mocks = new TestMocks();
        mocks.ServerBehaviour
            .Setup(sb =>
                sb.ValidateAuthenticationCredentials(It.IsAny<IConnection>(),
                    It.IsAny<IAuthenticationCredentials>())).Returns(Task.FromResult(AuthenticationResult.Success));


        LoginMechanismProcessor processor = Setup(mocks);
        AuthMechanismProcessorStatus
            result = await processor.ProcessResponse(EncodeBase64("rob")).ConfigureAwait(false);

        Assert.Equal(AuthMechanismProcessorStatus.Continue, result);

        mocks.Connection.Verify(c =>
            c.WriteResponse(
                It.Is<SmtpResponse>(r =>
                    VerifyBase64Response(r.Message, "Password:")
                    && r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue
                )
            )
        );

        result = await processor.ProcessResponse(EncodeBase64("password")).ConfigureAwait(false);
        Assert.Equal(AuthMechanismProcessorStatus.Success, result);
    }

    /// <summary>
    ///     The ProcessResponse_Response_BadBase64
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessResponse_Response_BadBase64() =>
        await Assert.ThrowsAsync<BadBase64Exception>(async () =>
        {
            TestMocks mocks = new TestMocks();

            LoginMechanismProcessor processor = Setup(mocks);
            await processor.ProcessResponse(null).ConfigureAwait(false);
            await processor.ProcessResponse("rob blah").ConfigureAwait(false);
        }).ConfigureAwait(false);

    /// <summary>
    /// </summary>
    /// <param name="mocks">The mocks<see cref="TestMocks" /></param>
    /// <returns>The <see cref="LoginMechanismProcessor" /></returns>
    private LoginMechanismProcessor Setup(TestMocks mocks) => new(mocks.Connection.Object);
}
