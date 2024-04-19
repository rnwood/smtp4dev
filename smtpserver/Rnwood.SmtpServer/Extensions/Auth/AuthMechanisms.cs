// <copyright file="AuthMechanisms.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Authentication Mechanisms.
/// </summary>
public static class AuthMechanisms
{
    /// <summary>
    ///     Return enumerable of all valid Auth Mechanisms.
    /// </summary>
    /// <returns>Enumerable collection of AuthMechanisms.</returns>
    public static IEnumerable<IAuthMechanism> All()
    {
        yield return new CramMd5Mechanism();
        yield return new PlainMechanism();
        yield return new LoginMechanism();
    }
}
