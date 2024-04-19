// <copyright file="BadBase64Exception.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="BadBase64Exception" />.
/// </summary>
public class BadBase64Exception : SmtpServerException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BadBase64Exception" /> class.
    /// </summary>
    /// <param name="smtpResponse">The smtpResponse<see cref="SmtpResponse" />.</param>
    public BadBase64Exception(SmtpResponse smtpResponse)
        : base(smtpResponse)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BadBase64Exception" /> class.
    /// </summary>
    /// <param name="smtpResponse">The smtpResponse<see cref="SmtpResponse" />.</param>
    /// <param name="innerException">The innerException<see cref="Exception" />.</param>
    public BadBase64Exception(SmtpResponse smtpResponse, Exception innerException)
        : base(smtpResponse, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BadBase64Exception" /> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BadBase64Exception(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BadBase64Exception" /> class.
    /// </summary>
    public BadBase64Exception()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BadBase64Exception" /> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception, or a null reference (Nothing in
    ///     Visual Basic) if no inner exception is specified.
    /// </param>
    public BadBase64Exception(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
