// <copyright file="IConnection.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="IConnection" />.
/// </summary>
public interface IConnection
{
    /// <summary>
    ///     Gets the current message which has been started by the MAIL FROM command but not yet completed with
    ///     a valid response from the server after the DATA command.
    /// </summary>
    IMessageBuilder CurrentMessage { get; }

    /// <summary>
    ///     Gets a list of extensions which are available for this connection.
    /// </summary>
    IReadOnlyCollection<IExtensionProcessor> ExtensionProcessors { get; }

    /// <summary>
    ///     Gets the MailVerb.
    /// </summary>
    MailVerb MailVerb { get; }

    /// <summary>
    ///     Gets the Server.
    /// </summary>
    ISmtpServer Server { get; }

    /// <summary>
    ///     Gets the Session.
    /// </summary>
    IEditableSession Session { get; }

    /// <summary>
    ///     Gets the VerbMap.
    /// </summary>
    IVerbMap VerbMap { get; }

    /// <summary>
    ///     Occurs when connection is closed.
    /// </summary>
    event AsyncEventHandler<ConnectionEventArgs> ConnectionClosedEventHandler;

    /// <summary>
    ///     Aborts the current message started by the MAIL FROM command.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task AbortMessage();

    /// <summary>
    ///     Applies a filter to the stream replacing the stream that this connection is reading/writing to with a new one. This
    ///     method is used to implement TLS etc.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task ApplyStreamFilter(Func<Stream, Task<Stream>> filter);

    /// <summary>
    ///     Closes the connection.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task CloseConnection();

    /// <summary>
    ///     Commits the current message.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task CommitMessage();

    /// <summary>
    ///     Creates and returns a new message and sets it as the current message.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<IMessageBuilder> NewMessage();

    /// <summary>
    ///     Reads the next line from the client and returns it.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<string> ReadLine();

    /// <summary>
    ///     Writes an <see cref="SmtpResponse" /> to the client.
    /// </summary>
    /// <param name="response">The response<see cref="SmtpResponse" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task WriteResponse(SmtpResponse response);

    /// <summary>
    ///     Reads bytes until CRLF and returns them
    /// </summary>
    /// <returns></returns>
    Task<byte[]> ReadLineBytes();
}
