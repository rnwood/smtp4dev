// <copyright file="XOAuth2Mechanism.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="XOAuth2Mechanism" /> which implements the XOAUTH2 auth mechanism.
/// </summary>
public class XOAuth2Mechanism : IAuthMechanism
{
    /// <inheritdoc />
    public string Identifier => "XOAUTH2";

    /// <inheritdoc />
    public bool IsPlainText => true;

    /// <inheritdoc />
    public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection) =>
        new XOauth2MechanismProcessor(connection);

    /// <inheritdoc />
    public override bool Equals(object obj) =>
        obj is XOAuth2Mechanism mechanism &&
        Identifier == mechanism.Identifier;

    /// <inheritdoc />
    public override int GetHashCode() => Identifier.GetHashCode();
}
