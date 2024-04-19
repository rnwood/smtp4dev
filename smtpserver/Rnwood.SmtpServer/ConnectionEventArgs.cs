// <copyright file="ConnectionEventArgs.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="ConnectionEventArgs" />.
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionEventArgs" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    public ConnectionEventArgs(IConnection connection) => Connection = connection;

    /// <summary>
    ///     Gets the Connection.
    /// </summary>
    public IConnection Connection { get; private set; }
}
