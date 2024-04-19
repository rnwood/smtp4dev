// <copyright file="Logging.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Rnwood.SmtpServer;

/// <summary>
///     Helper class implementing logging.
/// </summary>
internal static class Logging
{
    /// <summary>
    ///     Gets the logging factory.
    /// </summary>
    /// <value>
    ///     The factory.
    /// </value>
    public static ILoggerFactory Factory { get; } = new LoggerFactory();
}
