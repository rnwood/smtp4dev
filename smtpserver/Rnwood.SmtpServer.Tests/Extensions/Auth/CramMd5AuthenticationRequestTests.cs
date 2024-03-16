// <copyright file="CramMd5AuthenticationRequestTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth;

/// <summary>
///     Defines the <see cref="CramMd5AuthenticationRequestTests" />
/// </summary>
public class CramMd5AuthenticationRequestTests
{
    /// <summary>
    ///     The ValidateResponse_Invalid
    /// </summary>
    [Fact]
    public void ValidateResponse_Invalid()
    {
        CramMd5AuthenticationCredentials authenticationCredentials =
            new CramMd5AuthenticationCredentials("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
        Assert.False(authenticationCredentials.ValidateResponse("password2"));
    }

    /// <summary>
    ///     The ValidateResponse_Valid
    /// </summary>
    [Fact]
    public void ValidateResponse_Valid()
    {
        CramMd5AuthenticationCredentials authenticationCredentials =
            new CramMd5AuthenticationCredentials("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
        Assert.True(authenticationCredentials.ValidateResponse("password"));
    }
}
