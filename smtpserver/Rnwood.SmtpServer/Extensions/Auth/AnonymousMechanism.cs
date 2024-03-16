// <copyright file="AnonymousMechanism.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="AnonymousMechanism" /> which implements ANONYMOUS authentication.
/// </summary>
public class AnonymousMechanism : IAuthMechanism
{
    /// <inheritdoc />
    public string Identifier => "ANONYMOUS";

    /// <inheritdoc />
    public bool IsPlainText => false;

    /// <inheritdoc />
    public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection) =>
        new AnonymousMechanismProcessor(connection);

    /// <inheritdoc />
    public override bool Equals(object obj) =>
        obj is AnonymousMechanism mechanism &&
        Identifier == mechanism.Identifier;

    /// <inheritdoc />
    public override int GetHashCode() => Identifier.GetHashCode();
}
