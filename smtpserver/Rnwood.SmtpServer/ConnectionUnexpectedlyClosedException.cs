// <copyright file="ConnectionUnexpectedlyClosedException.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="ConnectionUnexpectedlyClosedException" />.
/// </summary>
public class ConnectionUnexpectedlyClosedException : IOException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionUnexpectedlyClosedException" /> class.
    /// </summary>
    public ConnectionUnexpectedlyClosedException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionUnexpectedlyClosedException" /> class.
    /// </summary>
    /// <param name="message">The message<see cref="string" />.</param>
    public ConnectionUnexpectedlyClosedException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionUnexpectedlyClosedException" /> class.
    /// </summary>
    /// <param name="message">The message<see cref="string" />.</param>
    /// <param name="innerException">The innerException<see cref="Exception" />.</param>
    public ConnectionUnexpectedlyClosedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
