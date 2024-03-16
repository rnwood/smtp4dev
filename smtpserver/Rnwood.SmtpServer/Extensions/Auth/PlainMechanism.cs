// <copyright file="PlainMechanism.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="PlainMechanism" /> which implements the PLAIN auth mechanism.
/// </summary>
public class PlainMechanism : IAuthMechanism
{
    /// <inheritdoc />
    public string Identifier => "PLAIN";

    /// <inheritdoc />
    public bool IsPlainText => true;

    /// <inheritdoc />
    public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection) =>
        new PlainMechanismProcessor(connection);

    /// <inheritdoc />
    public override bool Equals(object obj) =>
        obj is PlainMechanism mechanism &&
        Identifier == mechanism.Identifier;

    /// <inheritdoc />
    public override int GetHashCode() => Identifier.GetHashCode();
}
