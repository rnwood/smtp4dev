// <copyright file="SmtpServerException.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="SmtpServerException" />.
/// </summary>
public class SmtpServerException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpServerException" /> class.
    /// </summary>
    /// <param name="smtpResponse">The smtpResponse<see cref="SmtpResponse" />.</param>
    public SmtpServerException(SmtpResponse smtpResponse)
        : base(smtpResponse.Message) =>
        SmtpResponse = smtpResponse;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpServerException" /> class.
    /// </summary>
    /// <param name="smtpResponse">The smtpResponse<see cref="SmtpResponse" />.</param>
    /// <param name="innerException">The innerException<see cref="Exception" />.</param>
    public SmtpServerException(SmtpResponse smtpResponse, Exception innerException)
        : base(smtpResponse.Message, innerException) =>
        SmtpResponse = smtpResponse;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpServerException" /> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception, or a null reference (Nothing in
    ///     Visual Basic) if no inner exception is specified.
    /// </param>
    public SmtpServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpServerException" /> class.
    /// </summary>
    public SmtpServerException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpServerException" /> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SmtpServerException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Gets the SmtpResponse.
    /// </summary>
    public SmtpResponse SmtpResponse { get; private set; }
}
