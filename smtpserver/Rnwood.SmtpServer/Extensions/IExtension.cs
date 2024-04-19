// <copyright file="IExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="IExtension" />.
/// </summary>
public interface IExtension
{
    /// <summary>
    ///     Creates the extension processor for a connection.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <returns>
    ///     The <see cref="IExtensionProcessor" />.
    /// </returns>
    IExtensionProcessor CreateExtensionProcessor(IConnection connection);
}
