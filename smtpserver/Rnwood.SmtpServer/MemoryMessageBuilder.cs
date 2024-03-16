// <copyright file="MemoryMessageBuilder.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="MemoryMessageBuilder" />.
/// </summary>
public class MemoryMessageBuilder : IMessageBuilder
{
    private readonly MemoryMessage message;

    private bool disposedValue; // To detect redundant calls

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemoryMessageBuilder" /> class.
    /// </summary>
    public MemoryMessageBuilder()
        : this(new MemoryMessage())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemoryMessageBuilder" /> class.
    /// </summary>
    /// <param name="message">The message<see cref="MemoryMessage" />.</param>
    protected MemoryMessageBuilder(MemoryMessage message) => this.message = message;

    /// <inheritdoc />
    public long? DeclaredMessageSize
    {
        get => message.DeclaredMessageSize;

        set => message.DeclaredMessageSize = value;
    }

    /// <inheritdoc />
    public bool EightBitTransport
    {
        get => message.EightBitTransport;

        set => message.EightBitTransport = value;
    }

    /// <inheritdoc />
    public string From
    {
        get => message.From;

        set => message.From = value;
    }

    /// <inheritdoc />
    public DateTime ReceivedDate
    {
        get => message.ReceivedDate;

        set => message.ReceivedDate = value;
    }

    /// <inheritdoc />
    public bool SecureConnection
    {
        get => message.SecureConnection;

        set => message.SecureConnection = value;
    }

    /// <inheritdoc />
    public ISession Session
    {
        get => message.Session;

        set => message.Session = value;
    }

    /// <inheritdoc />
    public ICollection<string> Recipients => message.RecipientsList;

    /// <inheritdoc />
    public async Task<Stream> GetData() => await message.GetData().ConfigureAwait(false);

    /// <inheritdoc />
    public virtual Task<IMessage> ToMessage() => Task.FromResult<IMessage>(message);

    /// <inheritdoc />
    public Task<Stream> WriteData()
    {
        CloseNotifyingMemoryStream stream = new CloseNotifyingMemoryStream();
        stream.Closing += (s, ea) => { message.Data = stream.ToArray(); };

        return Task.FromResult<Stream>(stream);
    }

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
