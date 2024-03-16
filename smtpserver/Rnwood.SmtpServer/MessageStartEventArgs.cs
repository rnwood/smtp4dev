// <copyright file="MessageStartEventArgs.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="MessageStartEventArgs" />.
/// </summary>
public class MessageStartEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageStartEventArgs" /> class.
    /// </summary>
    /// <param name="session">The session<see cref="ISession" />.</param>
    /// <param name="from">The from address.</param>
    public MessageStartEventArgs(ISession session, string from)
    {
        Session = session;
        From = from;
    }

    /// <summary>
    ///     Gets the Session.
    /// </summary>
    public ISession Session { get; private set; }

    /// <summary>
    ///     Gets the from address.
    /// </summary>
    public string From { get; private set; }
}
