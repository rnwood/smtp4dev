// <copyright file="ExtensionProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="ExtensionProcessor" />.
/// </summary>
public abstract class ExtensionProcessor : IExtensionProcessor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExtensionProcessor" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    protected ExtensionProcessor(IConnection connection) => Connection = connection;

    /// <summary>
    ///     Gets the Connection.
    /// </summary>
    public IConnection Connection { get; private set; }

    /// <summary>
    ///     Returns the EHLO keywords which advertise this extension to the client.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    public abstract Task<string[]> GetEHLOKeywords();
}
