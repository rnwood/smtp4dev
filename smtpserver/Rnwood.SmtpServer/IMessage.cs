// <copyright file="IMessage.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="IMessage" />.
/// </summary>
public interface IMessage : IDisposable
{
    /// <summary>
    ///     Gets the size of the message as declared by the client using the SIZE extension to the MAIL FROM command, or null
    ///     if not specified by the client.
    /// </summary>
    long? DeclaredMessageSize { get; }

    /// <summary>
    ///     Gets a value indicating whether the messaage was received over a 8-bit 'clean' connection using the 8BITMIME
    ///     extension.
    /// </summary>
    bool EightBitTransport { get; }

    /// <summary>
    ///     Gets the sender of the message as specified by the client when sending MAIL FROM command.
    /// </summary>
    string From { get; }

    /// <summary>
    ///     Gets the date the message was received by the server.
    /// </summary>
    DateTime ReceivedDate { get; }

    /// <summary>
    ///     Gets a value indicating whether if message was received over a secure connection.
    /// </summary>
    bool SecureConnection { get; }

    /// <summary>
    ///     Gets the Session message was received on.
    /// </summary>
    ISession Session { get; }

    /// <summary>
    ///     Gets the recipient of the message as specified by the client when sending RCPT TO command.
    /// </summary>
    IReadOnlyCollection<string> Recipients { get; }

    /// <summary>
    ///     Gets a stream which returns the message data.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<Stream> GetData();
}
