// <copyright file="IVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Verbs;

/// <summary>
///     Defines the <see cref="IVerb" />.
/// </summary>
public interface IVerb
{
    /// <summary>
    ///     Processes a command which math.
    /// </summary>
    /// <param name="connection">The connection<see cref="Rnwood.SmtpServer.IConnection" />.</param>
    /// <param name="command">The command<see cref="Rnwood.SmtpServer.SmtpCommand" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task Process(IConnection connection, SmtpCommand command);
}
