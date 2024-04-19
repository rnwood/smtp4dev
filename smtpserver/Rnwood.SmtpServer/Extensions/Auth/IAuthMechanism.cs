// <copyright file="IAuthMechanism.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="IAuthMechanism" /> which implements a single authentication mechansim for the server.
/// </summary>
public interface IAuthMechanism
{
    /// <summary>
    ///     Gets the identifier for this AUTH mechanism as declared by the server in the EHELO response.
    /// </summary>
    string Identifier { get; }

    /// <summary>
    ///     Gets a value indicating whether credentials are sent using plain text.
    /// </summary>
    bool IsPlainText { get; }

    /// <summary>
    ///     Creates an authentication mechanism processor for the provided connection.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <returns>
    ///     The <see cref="IAuthMechanismProcessor" />.
    /// </returns>
    IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection);
}
