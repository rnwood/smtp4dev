// <copyright file="IExtensionProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="IExtensionProcessor" />.
/// </summary>
public interface IExtensionProcessor
{
    /// <summary>
    ///     Returns a sequence of EHLO keywords which are output to advertise the support for this extension to the client.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<string[]> GetEHLOKeywords();
}
