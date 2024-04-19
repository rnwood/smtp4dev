// <copyright file="ISession.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="ISession" />.
/// </summary>
public interface ISession : IDisposable
{
    /// <summary>
    ///     Gets a value indicating whether this the client provided authentication.
    /// </summary>
    bool Authenticated { get; }

    /// <summary>
    ///     Gets the AuthenticationCredentials.
    /// </summary>
    IAuthenticationCredentials AuthenticationCredentials { get; }

    /// <summary>
    ///     Gets the IP address of the client that established this session.
    /// </summary>
    IPAddress ClientAddress { get; }

    /// <summary>
    ///     Gets the ClientName
    ///     Gets or sets the name of the client as reported in its HELO/EHLO command
    ///     or null.
    /// </summary>
    string ClientName { get; }

    /// <summary>
    ///     Gets a value indicating whether this <see cref="AbstractSession" /> completed normally (by the client issuing a
    ///     QUIT command)
    ///     as opposed to abormal termination such as a connection timeout or unhandled errors in the server.
    /// </summary>
    bool CompletedNormally { get; }

    /// <summary>
    ///     Gets the date the session ended.
    /// </summary>
    DateTime? EndDate { get; }

    /// <summary>
    ///     Gets a value indicating whether the session is over a secure connection.
    /// </summary>
    bool SecureConnection { get; }

    /// <summary>
    ///     Gets the error that caused the session to terminate if it didn't complete normally.
    /// </summary>
    Exception SessionError { get; }

    /// <summary>
    ///     Gets a classification of the type of error which was experienced.
    /// </summary>
    SessionErrorType SessionErrorType { get; }

    /// <summary>
    ///     Gets the date the session started.
    /// </summary>
    DateTime StartDate { get; }

    /// <summary>
    ///     Indicates the current number of bad commands this client has sent in a row.
    /// </summary>
    int NumberOfBadCommandsInARow { get; }

    /// <summary>
    ///     Gets the session log (all communication between the client and server)
    ///     if session logging is enabled.
    /// </summary>
    /// <returns>A <see cref="TextReader" /> which will read from the session log.</returns>
    Task<TextReader> GetLog();

    /// <summary>
    ///     Gets list of messages received in this session.
    /// </summary>
    /// <returns>A read only list of messages.</returns>
    Task<IReadOnlyCollection<IMessage>> GetMessages();
}
