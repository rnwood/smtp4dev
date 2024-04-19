// <copyright file="MemoryMessage.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="MemoryMessage" />.
/// </summary>
public class MemoryMessage : IMessage
{
    private bool disposedValue; // To detect redundant calls

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemoryMessage" /> class.
    /// </summary>
    public MemoryMessage()
    {
    }

    /// <summary>
    ///     Gets or sets the message data.
    /// </summary>
    /// <value>
    ///     The data.
    /// </value>
    internal byte[] Data { get; set; }

    /// <summary>
    ///     Gets the recipients list.
    /// </summary>
    /// <value>
    ///     The recipients list.
    /// </value>
    internal List<string> RecipientsList { get; } = new();

    /// <summary>
    ///     Gets the DeclaredMessageSize.
    /// </summary>
    public long? DeclaredMessageSize { get; internal set; }

    /// <summary>
    ///     Gets a value indicating whether EightBitTransport.
    /// </summary>
    public bool EightBitTransport { get; internal set; }

    /// <summary>
    ///     Gets the From.
    /// </summary>
    public string From { get; internal set; }

    /// <summary>
    ///     Gets the ReceivedDate.
    /// </summary>
    public DateTime ReceivedDate { get; internal set; }

    /// <summary>
    ///     Gets a value indicating whether if message was received over a secure connection.
    /// </summary>
    public bool SecureConnection { get; internal set; }

    /// <summary>
    ///     Gets the Session message was received on.
    /// </summary>
    public ISession Session { get; internal set; }

    /// <summary>
    ///     Gets the recipient of the message as specified by the client when sending RCPT TO command.
    /// </summary>
    public IReadOnlyCollection<string> Recipients => RecipientsList.AsReadOnly();

    /// <summary>
    ///     Gets a stream which returns the message data.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    public Task<Stream> GetData() =>
        Task.FromResult<Stream>(
            new MemoryStream(
                Data ?? Array.Empty<byte>(),
                false));

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            disposedValue = true;
        }
    }
}
