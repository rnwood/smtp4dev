// <copyright file="IAuthMechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="IAuthMechanismProcessor" /> which implements the state machine for a particular auth
///     mechnism for a single client connection.
/// </summary>
public interface IAuthMechanismProcessor
{
    /// <summary>
    ///     Gets the Credentials supplied during this authentication.
    /// </summary>
    IAuthenticationCredentials Credentials { get; }

    /// <summary>
    ///     Processes a response from the client and returns the result of the auth operation.
    /// </summary>
    /// <param name="data">The data<see cref="string" />.</param>
    /// <returns>
    ///     A <see cref="Task{T}" /> representing the async operation.
    /// </returns>
    Task<AuthMechanismProcessorStatus> ProcessResponse(string data);
}
