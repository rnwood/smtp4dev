// <copyright file="IAuthenticationCredentials.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
/// Represents auth credentials that have a username and which can be validated by the server using the clear text password.
/// </summary>
public interface IAuthenticationCredentialsCanValidateWithPassword : IAuthenticationCredentials
{
    /// <summary>
    /// The username
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Checks the response using clear text password.
    /// </summary>
    /// <param name="password"></param>
    /// <returns>True if response matches the password.</returns>
    bool ValidateResponse(string password);
}
