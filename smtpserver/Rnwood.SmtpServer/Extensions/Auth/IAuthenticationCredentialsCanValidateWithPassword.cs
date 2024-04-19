// <copyright file="IAuthenticationCredentials.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

public interface IAuthenticationCredentialsCanValidateWithPassword : IAuthenticationCredentials
{
    string Username { get; }

    bool ValidateResponse(string password);
}
