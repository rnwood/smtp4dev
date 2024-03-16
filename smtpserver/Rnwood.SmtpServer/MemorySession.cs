// <copyright file="MemorySession.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="MemorySession" />.
/// </summary>
public class MemorySession : AbstractSession
{
    private readonly SmtpStreamWriter log;

    private readonly MemoryStream logStream = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemorySession" /> class.
    /// </summary>
    /// <param name="clientAddress">The clientAddress<see cref="IPAddress" />.</param>
    /// <param name="startDate">The startDate<see cref="DateTime" />.</param>
    public MemorySession(IPAddress clientAddress, DateTime startDate)
        : base(clientAddress, startDate) =>
        log = new SmtpStreamWriter(logStream, false);

    /// <inheritdoc />
    public override Task AppendLineToSessionLog(string text)
    {
        log.WriteLine(text);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<TextReader> GetLog()
    {
        log.Flush();
        return Task.FromResult<TextReader>(new StreamReader(new MemoryStream(logStream.ToArray(), false),
            new UTF8Encoding(false, true), false));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        logStream.Dispose();
        log.Dispose();
    }
}
