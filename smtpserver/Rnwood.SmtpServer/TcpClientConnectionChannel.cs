// <copyright file="TcpClientConnectionChannel.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TcpClientConnectionChannel" />
    /// </summary>
    public class TcpClientConnectionChannel : IConnectionChannel
    {
        private readonly TcpClient tcpClient;

        private SmtpStreamReader reader;

        private Stream stream;

        private SmtpStreamWriter writer;

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpClientConnectionChannel"/> class.
        /// </summary>
        /// <param name="tcpClient">The tcpClient<see cref="TcpClient"/></param>
        public TcpClientConnectionChannel(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            this.stream = tcpClient.GetStream();
            this.IsConnected = true;
            this.writer = new SmtpStreamWriter(this.stream, false) { AutoFlush = true };
            this.SetReaderEncoding(new UTF8Encoding(false, true));
        }

        /// <summary>
        /// Defines the Closed
        /// </summary>
        public event AsyncEventHandler<EventArgs> ClosedEventHandler;

        /// <summary>
        /// Gets the ClientIPAddress
        /// </summary>
        public IPAddress ClientIPAddress => ((IPEndPoint)this.tcpClient.Client.RemoteEndPoint).Address;

        /// <summary>
        /// Gets a value indicating whether IsConnected
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets the ReaderEncoding
        /// </summary>
        public Encoding ReaderEncoding { get; private set; }

        /// <summary>
        /// Gets or sets the ReceiveTimeout
        /// </summary>
        public TimeSpan ReceiveTimeout
        {
            get { return TimeSpan.FromMilliseconds(this.tcpClient.ReceiveTimeout); }
            set { this.tcpClient.ReceiveTimeout = (int)Math.Min(int.MaxValue, value.TotalMilliseconds); }
        }

        /// <summary>
        /// Gets or sets the SendTimeout
        /// </summary>
        public TimeSpan SendTimeout
        {
            get { return TimeSpan.FromMilliseconds(this.tcpClient.SendTimeout); }
            set { this.tcpClient.SendTimeout = (int)Math.Min(int.MaxValue, value.TotalMilliseconds); }
        }

        /// <summary>
        /// Applies the a filter to the stream which is used to read data from the channel.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// A <see cref="Task{T}" /> representing the async operation
        /// </returns>
        public async Task ApplyStreamFilter(Func<Stream, Task<Stream>> filter)
        {
            this.stream = await filter(this.stream).ConfigureAwait(false);
            this.SetupReader();
        }

        /// <summary>
        /// Closes the channel and notifies users via the <see cref="ClosedEventHandler" /> event.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{T}" /> representing the async operation
        /// </returns>
        public Task Close()
        {
            if (this.IsConnected)
            {
                this.IsConnected = false;
                this.tcpClient.Dispose();

                foreach (Delegate handler in this.ClosedEventHandler?.GetInvocationList() ?? Enumerable.Empty<Delegate>())
                {
                    handler.DynamicInvoke(this, EventArgs.Empty);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Flushes outgoing data.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{T}" /> representing the async operation
        /// </returns>
        public async Task Flush()
        {
            await this.writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the next line from the channel using the currrent <see cref="ReaderEncoding" />
        /// </summary>
        /// <returns>
        /// A <see cref="Task{T}" /> representing the async operation
        /// </returns>
        /// <exception cref="System.IO.IOException">Reader returned null string</exception>
        /// <exception cref="ConnectionUnexpectedlyClosedException">Read failed</exception>
        public async Task<string> ReadLine()
        {
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    string text = await this.reader.ReadLineAsync(cts.Token).ConfigureAwait(false);

                    if (text == null)
                    {
                        throw new IOException("Reader returned null string");
                    }

                    return text;
                }
            }
            catch (IOException e)
            {
                await this.Close().ConfigureAwait(false);
                throw new ConnectionUnexpectedlyClosedException("Read failed", e);
            }
        }

        /// <inheritdoc/>
        public void SetReaderEncoding(Encoding encoding)
        {
            this.ReaderEncoding = encoding;
            this.SetupReader();
        }

        /// <inheritdoc/>
        public async Task WriteLine(string text)
        {
            try
            {
                await this.writer.WriteLineAsync(text).ConfigureAwait(false);
            }
            catch (IOException e)
            {
                await this.Close().ConfigureAwait(false);
                throw new ConnectionUnexpectedlyClosedException("Write failed", e);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.writer.Dispose();
                    this.reader.Dispose();
                    this.stream.Dispose();
                    this.tcpClient.Dispose();
                }

                this.disposedValue = true;
            }
        }

        private void SetupReader()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
            }

            this.reader = new SmtpStreamReader(this.stream, this.ReaderEncoding, true);
        }
    }
}
