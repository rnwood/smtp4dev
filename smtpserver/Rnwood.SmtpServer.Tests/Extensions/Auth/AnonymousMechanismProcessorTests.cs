// <copyright file="AnonymousMechanismProcessorTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth;

/// <summary>
///     Defines the <see cref="AnomymousMechanismProcessorTests" />
/// </summary>
public class AnomymousMechanismProcessorTests
{
    /// <summary>
    ///     The ProcessResponse_Failure
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessResponse_Failure() =>
        await ProcessResponseAsync(AuthenticationResult.Failure, AuthMechanismProcessorStatus.Failed)
            ;

    /// <summary>
    ///     The ProcessResponse_Success
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessResponse_Success() =>
        await ProcessResponseAsync(AuthenticationResult.Success, AuthMechanismProcessorStatus.Success)
            ;

    /// <summary>
    ///     The ProcessResponse_TemporarilyFailure
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessResponse_TemporarilyFailure() =>
        await ProcessResponseAsync(AuthenticationResult.TemporaryFailure, AuthMechanismProcessorStatus.Failed)
            ;

    /// <summary>
    /// </summary>
    /// <param name="authenticationResult">The authenticationResult<see cref="AuthenticationResult" /></param>
    /// <param name="authMechanismProcessorStatus">
    ///     The authMechanismProcessorStatus<see cref="AuthMechanismProcessorStatus" />
    /// </param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    private async Task ProcessResponseAsync(AuthenticationResult authenticationResult,
        AuthMechanismProcessorStatus authMechanismProcessorStatus)
    {
        TestMocks mocks = new TestMocks();
        mocks.ServerOptions.Setup(
                b =>
                    b.ValidateAuthenticationCredentials(mocks.Connection.Object,
                        It.IsAny<AnonymousAuthenticationCredentials>()))
            .ReturnsAsync(authenticationResult);

        AnonymousMechanismProcessor anonymousMechanismProcessor =
            new AnonymousMechanismProcessor(mocks.Connection.Object);
        AuthMechanismProcessorStatus result =
            await anonymousMechanismProcessor.ProcessResponse(null);

        Assert.Equal(authMechanismProcessorStatus, result);

        if (authenticationResult == AuthenticationResult.Success)
        {
            Assert.IsType<AnonymousAuthenticationCredentials>(anonymousMechanismProcessor.Credentials);
        }
    }
}
