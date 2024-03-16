// <copyright file="IConnectionChannel.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Represents a channel connecting the client and server.
/// </summary>
/// <seealso cref="System.IDisposable" />
public interface IConnectionChannel : IDisposable
{
    /// <summary>
    ///     Gets the client ip address.
    /// </summary>
    /// <value>
    ///     The client ip address.
    /// </value>
    IPAddress ClientIPAddress { get; }

    /// <summary>
    ///     Gets a value indicating whether this instance is connected.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is connected; otherwise, <c>false</c>.
    /// </value>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets or sets the receive timeout after which if data is expected but not received, the connection will be
    ///     terminated.
    /// </summary>
    /// <value>
    ///     The receive timeout.
    /// </value>
    TimeSpan ReceiveTimeout { get; set; }

    /// <summary>
    ///     Gets or sets the send timeout after which is data is being attempted to be sent but not completed, the connection
    ///     will be terminated.
    /// </summary>
    /// <value>
    ///     The send timeout.
    /// </value>
    TimeSpan SendTimeout { get; set; }

    /// <summary>
    ///     Occurs when the channel is closed.
    /// </summary>
    event AsyncEventHandler<EventArgs> ClosedEventHandler;

    /// <summary>
    ///     Applies the a filter to the stream which is used to read data from the channel.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task ApplyStreamFilter(Func<Stream, Task<Stream>> filter);

    /// <summary>
    ///     Closes the channel and notifies users via the <see cref="ClosedEventHandler" /> event.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task Close();

    /// <summary>
    ///     Flushes outgoing data.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task Flush();

    /// <summary>
    ///     Reads the next line from the channel.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<string> ReadLine();

    /// <summary>
    ///     Writes a line of text to the client.
    /// </summary>
    /// <param name="text">The text<see cref="string" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task WriteLine(string text);

    /// <summary>
    ///     Reads bytes until CRLF and returns them
    /// </summary>
    /// <returns></returns>
    Task<byte[]> ReadLineBytes();
}
