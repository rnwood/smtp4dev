// <copyright file="CloseNotifyingMemoryStream.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="CloseNotifyingMemoryStream" /> which is a memory stream that fires an event when disposed.
/// </summary>
internal class CloseNotifyingMemoryStream : MemoryStream
{
    /// <summary>
    ///     Occurs when the stream is disposed.
    /// </summary>
    public event EventHandler Closing;

    /// <summary>
    ///     Releases the unmanaged resources used by the <see cref="System.IO.MemoryStream"></see> class and optionally
    ///     releases the managed resources.
    /// </summary>
    /// <param name="disposing">The disposing<see cref="bool" />.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }

        base.Dispose(disposing);
    }
}
