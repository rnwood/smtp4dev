// <copyright file="SessionEventArgs.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="SessionEventArgs" />.
/// </summary>
public class SessionEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SessionEventArgs" /> class.
    /// </summary>
    /// <param name="session">The session<see cref="ISession" />.</param>
    public SessionEventArgs(ISession session) => Session = session;

    /// <summary>
    ///     Gets the Session.
    /// </summary>
    public ISession Session { get; private set; }
}
