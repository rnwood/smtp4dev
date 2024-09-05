// <copyright file="UsernameAndPasswordAuthenticationCredentials.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="UsernameAndPasswordAuthenticationCredentials" />.
/// </summary>
public abstract class UsernameAndPasswordAuthenticationCredentials : IAuthenticationCredentials, IAuthenticationCredentialsCanValidateWithPassword
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UsernameAndPasswordAuthenticationCredentials" /> class.
    /// </summary>
    /// <param name="username">The username<see cref="string" />.</param>
    /// <param name="password">The password<see cref="string" />.</param>
    protected UsernameAndPasswordAuthenticationCredentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    /// <summary>
    ///     Gets the Password.
    /// </summary>
    public string Password { get; private set; }

    /// <summary>
    ///     Gets the Username.
    /// </summary>
    public string Username { get; private set; }

    /// <inheritdoc />
    public string Type { get => "USERNAME_PASSWORD"; }

    /// <inheritdoc/>
    public bool ValidateResponse(string password) {
        return string.Equals(Password, password);
    }
}
