// <copyright file="AbstractSession.cs" company="Rnwood.SmtpServer project contributors">
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
///     Provides a base implementation for <see cref="IEditableSession" />.
/// </summary>
/// <seealso cref="Rnwood.SmtpServer.IEditableSession" />
public abstract class AbstractSession : IEditableSession
{
    private readonly List<IMessage> messages;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AbstractSession" /> class.
    /// </summary>
    /// <param name="clientAddress">The client address.</param>
    /// <param name="startDate">The start date.</param>
    protected AbstractSession(IPAddress clientAddress, DateTime startDate)
    {
        messages = new List<IMessage>();
        ClientAddress = clientAddress;
        StartDate = startDate;
    }

    /// <inheritdoc />
    public virtual bool Authenticated { get; set; }

    /// <inheritdoc />
    public virtual IAuthenticationCredentials AuthenticationCredentials { get; set; }

    /// <inheritdoc />
    public virtual IPAddress ClientAddress { get; set; }

    /// <inheritdoc />
    public virtual string ClientName { get; set; }

    /// <inheritdoc />
    public virtual bool CompletedNormally { get; set; }

    /// <inheritdoc />
    public virtual DateTime? EndDate { get; set; }

    /// <inheritdoc />
    public virtual bool SecureConnection { get; set; }

    /// <inheritdoc />
    public virtual Exception SessionError { get; set; }

    /// <inheritdoc />
    public virtual SessionErrorType SessionErrorType { get; set; }

    /// <inheritdoc />
    public virtual DateTime StartDate { get; set; }

    /// <inheritdoc />
    public virtual Task AddMessage(IMessage message)
    {
        messages.Add(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task AppendLineToSessionLog(string text);

    /// <inheritdoc />
    public virtual Task IncrementBadCommandCounter()
    {
        NumberOfBadCommandsInARow++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task ResetBadCommandCounter()
    {
        NumberOfBadCommandsInARow = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task<TextReader> GetLog();

    /// <inheritdoc />
    public virtual Task<IReadOnlyCollection<IMessage>> GetMessages() =>
        Task.FromResult<IReadOnlyCollection<IMessage>>(messages.AsReadOnly());

    /// <inheritdoc />
    public int NumberOfBadCommandsInARow { get; protected set; }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    protected abstract void Dispose(bool disposing);
}
