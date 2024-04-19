// <copyright file="TcpClientConnectionChannel.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="TcpClientConnectionChannel" />.
/// </summary>
public class TcpClientConnectionChannel : IConnectionChannel
{
    private readonly Encoding fallbackEncoding;
    private readonly TcpClient tcpClient;

    private bool disposedValue; // To detect redundant calls

    private SmtpStreamReader reader;

    private Stream stream;

    private SmtpStreamWriter writer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TcpClientConnectionChannel" /> class.
    /// </summary>
    /// <param name="tcpClient">The tcpClient<see cref="TcpClient" />.</param>
    /// <param name="fallbackEncoding">The encoding to fallback to if bytes received cannot be decoded as UTF-8.</param>
    public TcpClientConnectionChannel(TcpClient tcpClient, Encoding fallbackEncoding)
    {
        this.tcpClient = tcpClient;
        stream = tcpClient.GetStream();
        IsConnected = true;
        this.fallbackEncoding = fallbackEncoding;
        SetupReaderAndWriter();
    }

    /// <summary>
    ///     Defines the Closed
    /// </summary>
    public event AsyncEventHandler<EventArgs> ClosedEventHandler;

    /// <summary>
    ///     Gets the ClientIPAddress.
    /// </summary>
    public IPAddress ClientIPAddress => ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;

    /// <summary>
    ///     Gets a value indicating whether IsConnected.
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    ///     Gets or sets the ReceiveTimeout.
    /// </summary>
    public TimeSpan ReceiveTimeout
    {
        get => TimeSpan.FromMilliseconds(tcpClient.ReceiveTimeout);
        set => tcpClient.ReceiveTimeout = (int)Math.Min(int.MaxValue, value.TotalMilliseconds);
    }

    /// <summary>
    ///     Gets or sets the SendTimeout.
    /// </summary>
    public TimeSpan SendTimeout
    {
        get => TimeSpan.FromMilliseconds(tcpClient.SendTimeout);
        set => tcpClient.SendTimeout = (int)Math.Min(int.MaxValue, value.TotalMilliseconds);
    }

    /// <summary>
    ///     Applies the a filter to the stream which is used to read data from the channel.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    public async Task ApplyStreamFilter(Func<Stream, Task<Stream>> filter)
    {
        stream = await filter(stream).ConfigureAwait(false);
        SetupReaderAndWriter();
    }

    /// <summary>
    ///     Closes the channel and notifies users via the <see cref="ClosedEventHandler" /> event.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    public Task Close()
    {
        if (IsConnected)
        {
            IsConnected = false;
            tcpClient.Dispose();

            foreach (Delegate handler in ClosedEventHandler?.GetInvocationList() ?? Enumerable.Empty<Delegate>())
            {
                handler.DynamicInvoke(this, EventArgs.Empty);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Flushes outgoing data.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    public async Task Flush() => await writer.FlushAsync().ConfigureAwait(false);

    /// <summary>
    ///     Reads the next line from the channel.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    /// <exception cref="System.IO.IOException">Reader returned null string.</exception>
    /// <exception cref="ConnectionUnexpectedlyClosedException">Read failed.</exception>
    public async Task<string> ReadLine()
    {
        try
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                string text = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false);

                if (text == null)
                {
                    throw new IOException("Reader returned null string");
                }

                return text;
            }
        }
        catch (IOException e)
        {
            await Close().ConfigureAwait(false);
            throw new ConnectionUnexpectedlyClosedException("Read failed", e);
        }
    }

    /// <inheritdoc />
    public async Task WriteLine(string text)
    {
        try
        {
            await writer.WriteLineAsync(text).ConfigureAwait(false);
        }
        catch (IOException e)
        {
            await Close().ConfigureAwait(false);
            throw new ConnectionUnexpectedlyClosedException("Write failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadLineBytes()
    {
        try
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                byte[] data = await reader.ReadLineBytesAsync(cts.Token).ConfigureAwait(false);

                if (data == null)
                {
                    throw new IOException("Reader returned null bytes");
                }

                return data;
            }
        }
        catch (IOException e)
        {
            await Close().ConfigureAwait(false);
            throw new ConnectionUnexpectedlyClosedException("Read failed", e);
        }
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
            if (disposing)
            {
                writer.Dispose();
                reader.Dispose();
                stream.Dispose();
                tcpClient.Dispose();
            }

            disposedValue = true;
        }
    }

    private void SetupReaderAndWriter()
    {
        if (reader != null)
        {
            reader.Dispose();
        }

        reader = new SmtpStreamReader(stream, fallbackEncoding, true);

        if (writer != null)
        {
            writer.Dispose();
        }

        writer = new SmtpStreamWriter(stream, true) { AutoFlush = true };
    }
}
