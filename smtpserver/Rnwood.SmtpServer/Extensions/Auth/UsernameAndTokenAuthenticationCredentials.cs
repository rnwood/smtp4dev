// <copyright file="UsernameAndTokenAuthenticationCredentials.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="UsernameAndTokenAuthenticationCredentials" />.
/// </summary>
public abstract class UsernameAndTokenAuthenticationCredentials : IAuthenticationCredentials
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UsernameAndTokenAuthenticationCredentials" /> class.
    /// </summary>
    /// <param name="username">The username<see cref="string" />.</param>
    /// <param name="accessToken">The access token<see cref="string" />.</param>
    protected UsernameAndTokenAuthenticationCredentials(string username, string accessToken)
    {
        Username = username;
        AccessToken = accessToken;
    }

    /// <summary>
    ///     Gets the Token.
    /// </summary>
    public string AccessToken { get; private set; }

    /// <summary>
    ///     Gets the Username.
    /// </summary>
    public string Username { get; private set; }

    /// <inheritdoc />
    public string Type { get => "USERNAME_ACESSTOKEN"; }
}
