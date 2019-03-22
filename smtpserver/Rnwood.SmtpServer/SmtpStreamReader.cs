// <copyright file="SmtpStreamReader.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>A stream writer which uses the correct \r\n line ending required for SMTP protocol.</summary>
    public class SmtpStreamReader : IDisposable
    {
        private readonly List<byte> lineBytes = new List<byte>(32 * 1024);
        private readonly byte[] buffer = new byte[64 * 1024];
        private readonly Stream stream;
        private readonly bool leaveOpen;
        private readonly Encoding fallbackEncoding;
        private readonly UTF8Encoding utf8Encoding = new UTF8Encoding(false, true);

        private bool disposedValue = false;
        private int bufferPos = 0;
        private int bufferLen = 0;

        /// <summary>Initializes a new instance of the <see cref="SmtpStreamReader"/> class.</summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="fallbackEncoding">The character encoding to use to fallback to should a line not be decodable as UTF8</param>
        /// <param name="leaveOpen">True if stream should be left open when the reader is disposed.</param>
        public SmtpStreamReader(Stream stream, Encoding fallbackEncoding, bool leaveOpen)
        {
            this.stream = stream;
            this.leaveOpen = leaveOpen;
            this.fallbackEncoding = Encoding.GetEncoding(fallbackEncoding.CodePage, new EncoderExceptionFallback(), new DecoderExceptionFallback());
        }

        /// <summary>Reads the a line from the stream which is terminated with a \n. The string will be decoded using UTF8 and falling back to the provided encoding if decoding fails.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            this.lineBytes.Clear();

            while (true)
            {
                while (this.bufferPos < this.bufferLen)
                {
                    byte bufferByte = this.buffer[this.bufferPos++];

                    if (bufferByte == '\n')
                    {
                        return this.Decode(this.lineBytes.ToArray());
                    }

                    if (bufferByte != '\r')
                    {
                        this.lineBytes.Add(bufferByte);
                    }
                }

                this.bufferPos = 0;
                this.bufferLen = await this.stream.ReadAsync(this.buffer, 0, this.buffer.Length, cancellationToken).ConfigureAwait(false);

                if (this.bufferLen < 1)
                {
                    return null;
                }
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (!this.leaveOpen)
                    {
                        this.stream.Close();
                    }
                }

                this.disposedValue = true;
            }
        }

        private string Decode(byte[] lineBytes)
        {
            try
            {
                return this.utf8Encoding.GetString(lineBytes);
            }
            catch (DecoderFallbackException)
            {
                return this.fallbackEncoding.GetString(lineBytes);
            }
        }
    }
}
