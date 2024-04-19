// <copyright file="IEditableSession.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="IEditableSession" />.
/// </summary>
public interface IEditableSession : ISession
{
    /// <summary>
    ///     Gets or sets a value indicating whether Authenticated.
    /// </summary>
    new bool Authenticated { get; set; }

    /// <summary>
    ///     Gets or sets the credentials used during authentication.
    /// </summary>
    new IAuthenticationCredentials AuthenticationCredentials { get; set; }

    /// <summary>
    ///     Gets or sets the client IP address.
    /// </summary>
    new IPAddress ClientAddress { get; set; }

    /// <summary>
    ///     Gets or sets the client name recevied in HELO or EHLO request.
    /// </summary>
    new string ClientName { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the session completed without an error.
    /// </summary>
    new bool CompletedNormally { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the session ended.
    /// </summary>
    new DateTime? EndDate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether a secure SSL/TLS channel was established.
    /// </summary>
    new bool SecureConnection { get; set; }

    /// <summary>
    ///     Gets or sets.
    /// </summary>
    new Exception SessionError { get; set; }

    /// <summary>
    ///     Gets or sets the SessionErrorType.
    /// </summary>
    new SessionErrorType SessionErrorType { get; set; }

    /// <summary>
    ///     Gets or sets the StartDate.
    /// </summary>
    new DateTime StartDate { get; set; }

    /// <summary>
    ///     Adds a message to this session.
    /// </summary>
    /// <param name="message">The message<see cref="IMessage" />.</param>
    Task AddMessage(IMessage message);

    /// <summary>Appends a line of text to the session log.</summary>
    /// <param name="text">The text<see cref="string" />.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task AppendLineToSessionLog(string text);

    /// <summary>
    ///     Increments the bad command counter.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task IncrementBadCommandCounter();

    /// <summary>
    ///     Resets the bad command counter.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task ResetBadCommandCounter();
}
