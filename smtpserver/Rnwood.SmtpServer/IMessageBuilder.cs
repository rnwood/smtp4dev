// <copyright file="IMessageBuilder.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="IMessageBuilder" />.
/// </summary>
public interface IMessageBuilder : IDisposable
{
    /// <summary>
    ///     Gets or sets the message size declared by the client using the SIZE extension.
    /// </summary>
    long? DeclaredMessageSize { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message was received over an 8-bit clean channel.
    /// </summary>
    bool EightBitTransport { get; set; }

    /// <summary>
    ///     Gets or sets the From.
    /// </summary>
    string From { get; set; }

    /// <summary>
    ///     Gets or sets the date the message was received.
    /// </summary>
    DateTime ReceivedDate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message is being received over a secure connection.
    /// </summary>
    bool SecureConnection { get; set; }

    /// <summary>
    ///     Gets or sets the Session this message is being received in.
    /// </summary>
    ISession Session { get; set; }

    /// <summary>
    ///     Gets the recipients of the message as specified in the RCPT TO command.
    /// </summary>
    /// <value>
    ///     The recipients.
    /// </value>
    ICollection<string> Recipients { get; }

    /// <summary>
    ///     Gets a read only stream containing the message data.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    Task<Stream> GetData();

    /// <summary>
    ///     Turns the editable messge into a read only message.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<IMessage> ToMessage();

    /// <summary>
    ///     Returns a stream which can be used to write to the message data.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<Stream> WriteData();
}
