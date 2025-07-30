// <copyright file="XOAuth2AuthenticationCredentials.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="XOAuth2AuthenticationCredentials" />.
/// </summary>
public class XOAuth2AuthenticationCredentials : UsernameAndTokenAuthenticationCredentials
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PlainAuthenticationCredentials" /> class.
    /// </summary>
    /// <param name="username">The username<see cref="string" />.</param>
    /// <param name="accessToken">The access token<see cref="string" />.</param>
    public XOAuth2AuthenticationCredentials(string username, string accessToken) : base(username, accessToken)
    {
    }
}
