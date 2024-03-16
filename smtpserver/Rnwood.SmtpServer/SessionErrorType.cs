// <copyright file="SessionErrorType.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer;

/// <summary>
///     A high level classification of common session termination errors.
/// </summary>
public enum SessionErrorType
{
    /// <summary>
    ///     Indicates that there was no error.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Indicates a network/IO error such as connection timeout or aborted connection.
    /// </summary>
    NetworkError,

    /// <summary>
    ///     Indicates an unhandled exception in the server or an extension which caused the connection to be terminated.
    /// </summary>
    UnexpectedException,

    /// <summary>
    ///     Indicates the connection was terminated because the server was shut down.
    /// </summary>
    ServerShutdown
}
