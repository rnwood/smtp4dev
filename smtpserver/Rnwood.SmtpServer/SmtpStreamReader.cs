// <copyright file="SmtpStreamReader.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>A stream writer which uses the correct \r\n line ending required for SMTP protocol.</summary>
public class SmtpStreamReader : IDisposable
{
    private readonly byte[] buffer = new byte[64 * 1024];
    private readonly Encoding fallbackEncoding;
    private readonly bool leaveOpen;
    private readonly List<byte> lineBytes = new(32 * 1024);
    private readonly Stream stream;
    private readonly UTF8Encoding utf8Encoding = new(false, true);
    private int bufferLen;
    private int bufferPos;

    private bool disposedValue;

    /// <summary>Initializes a new instance of the <see cref="SmtpStreamReader" /> class.</summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="fallbackEncoding">The character encoding to use to fallback to should a line not be decodable as UTF8.</param>
    /// <param name="leaveOpen">True if stream should be left open when the reader is disposed.</param>
    public SmtpStreamReader(Stream stream, Encoding fallbackEncoding, bool leaveOpen)
    {
        this.stream = stream;
        this.leaveOpen = leaveOpen;
        this.fallbackEncoding = Encoding.GetEncoding(fallbackEncoding.CodePage, new EncoderExceptionFallback(),
            new DecoderExceptionFallback());
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task<byte[]> ReadLineBytesAsync(CancellationToken cancellationToken)
    {
        lineBytes.Clear();

        while (true)
        {
            while (bufferPos < bufferLen)
            {
                byte bufferByte = buffer[bufferPos++];

                if (bufferByte == '\n')
                {
                    return lineBytes.ToArray();
                }

                if (bufferByte != '\r')
                {
                    lineBytes.Add(bufferByte);
                }
            }

            bufferPos = 0;
            bufferLen = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (bufferLen < 1)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Reads the a line from the stream which is terminated with a \n. The string will be decoded using UTF8 and
    ///     falling back to the provided encoding if decoding fails.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task<string> ReadLineAsync(CancellationToken cancellationToken) =>
        Decode(await ReadLineBytesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing && !leaveOpen)
            {
                stream.Close();
            }

            disposedValue = true;
        }
    }

    private string Decode(byte[] lineBytes)
    {
        if (lineBytes == null)
        {
            return null;
        }

        try
        {
            return utf8Encoding.GetString(lineBytes);
        }
        catch (DecoderFallbackException)
        {
            return fallbackEncoding.GetString(lineBytes);
        }
    }
}
