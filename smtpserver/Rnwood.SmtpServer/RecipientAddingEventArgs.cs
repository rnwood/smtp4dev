// <copyright file="RecipientEventArgs.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="RecipientAddingEventArgs" />.
/// </summary>
public class RecipientAddingEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RecipientAddingEventArgs" /> class.
    /// </summary>
    /// <param name="message">The message<see cref="IMessage" />.</param>
    /// <param name="recipient">The recipient being added.</param>
    public RecipientAddingEventArgs(IMessageBuilder message, string recipient, IConnection connection)
    {
        Message = message;
        Recipient = recipient;
        Connection = connection;
    }

    /// <summary>
    ///     Gets the Message.
    /// </summary>
    public IMessageBuilder Message { get; private set; }

    /// <summary>
    ///     Gets the Recipient.
    /// </summary>
    public string Recipient { get; private set; }
    public IConnection Connection { get; private set; }
}
