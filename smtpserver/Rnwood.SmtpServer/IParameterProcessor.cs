// <copyright file="IParameterProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="IParameterProcessor" />.
/// </summary>
public interface IParameterProcessor
{
    /// <summary>
    ///     Processes the parameter which has the <paramref name="key" /> and <paramref name="value" /> specified.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="key">The key<see cref="string" />.</param>
    /// <param name="value">The value<see cref="string" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task SetParameter(IConnection connection, string key, string value);
}
