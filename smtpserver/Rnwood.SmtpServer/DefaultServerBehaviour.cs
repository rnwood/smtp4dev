﻿// <copyright file="DefaultServerBehaviour.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer;

/// <summary>
///     Implements a default <see cref="IServerBehaviour" /> which is suitable for many basic uses.
/// </summary>
/// <seealso cref="Rnwood.SmtpServer.IServerBehaviour" />
public class DefaultServerBehaviour : IServerBehaviour
{
    private readonly bool allowRemoteConnections;

    private readonly X509Certificate implcitTlsCertificate;
    private readonly X509Certificate startTlsCertificate;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultServerBehaviour" /> class.
    /// </summary>
    /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
    public DefaultServerBehaviour(bool allowRemoteConnections)
        : this(allowRemoteConnections, 25, null, null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultServerBehaviour" /> class.
    /// </summary>
    /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
    /// <param name="portNumber">The port number.</param>
    public DefaultServerBehaviour(bool allowRemoteConnections, int portNumber)
        : this(allowRemoteConnections, portNumber, null, null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultServerBehaviour" /> class.
    /// </summary>
    /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
    /// <param name="portNumber">The port number.</param>
    /// <param name="implicitTlsCertificate">The TLS certificate to use for implicit TLS.</param>
    public DefaultServerBehaviour(bool allowRemoteConnections, int portNumber, X509Certificate implicitTlsCertificate)
        : this(allowRemoteConnections, portNumber, implicitTlsCertificate, null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultServerBehaviour" /> class.
    /// </summary>
    /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
    /// <param name="portNumber">The port number.</param>
    /// <param name="implicitTlsCertificate">The TLS certificate to use for implicit TLS.</param>
    /// <param name="startTlsCertificate">The TLS certificate to use for STARTTLS.</param>
    public DefaultServerBehaviour(
        bool allowRemoteConnections,
        int portNumber,
        X509Certificate implicitTlsCertificate,
        X509Certificate startTlsCertificate)
        : this(allowRemoteConnections, Dns.GetHostName(), portNumber, implicitTlsCertificate, startTlsCertificate)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultServerBehaviour" /> class.
    /// </summary>
    /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
    /// <param name="domainName">The domain name the server will send in greeting.</param>
    /// <param name="portNumber">The port number.</param>
    /// <param name="implcitTlsCertificate">The TLS certificate to use for implicit TLS.</param>
    /// <param name="startTlsCertificate">The TLS certificate to use for STARTTLS.</param>
    public DefaultServerBehaviour(
        bool allowRemoteConnections,
        string domainName,
        int portNumber,
        X509Certificate implcitTlsCertificate,
        X509Certificate startTlsCertificate)
    {
        DomainName = domainName;
        PortNumber = portNumber;
        this.implcitTlsCertificate = implcitTlsCertificate;
        this.startTlsCertificate = startTlsCertificate;
        this.allowRemoteConnections = allowRemoteConnections;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultServerBehaviour" /> class.
    /// </summary>
    /// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
    /// <param name="implcitTlsCertificate">The TLS certificate to use for implicit TLS.</param>
    public DefaultServerBehaviour(bool allowRemoteConnections, X509Certificate implcitTlsCertificate)
        : this(allowRemoteConnections, 587, implcitTlsCertificate, null)
    {
    }

    /// <summary>
    ///     Gets or sets a List of active Auth Mechanism Identifiers.
    /// </summary>
    public HashSet<IAuthMechanism> EnabledAuthMechanisms { get; set; } =
        new(AuthMechanisms.All());

    /// <inheritdoc />
    public virtual string DomainName { get; }

    /// <inheritdoc />
    public virtual IPAddress IpAddress => allowRemoteConnections ? IPAddress.Any : IPAddress.Loopback;

    /// <inheritdoc />
    public int MaximumNumberOfSequentialBadCommands => 10;

    /// <inheritdoc />
    public virtual int PortNumber { get; }

    /// <inheritdoc />
    public virtual Encoding FallbackEncoding => Encoding.GetEncoding("iso-8859-1");

    /// <inheritdoc />
    public virtual Task<IEnumerable<IExtension>> GetExtensions(IConnectionChannel connectionChannel)
    {
        List<IExtension> extensions = new List<IExtension>(new IExtension[]
        {
            new EightBitMimeExtension(), new SizeExtension(), new SmtpUtfEightExtension()
        });

        if (startTlsCertificate != null)
        {
            extensions.Add(new StartTlsExtension());
        }

        if (AuthenticationCredentialsValidationRequiredEventHandler != null)
        {
            extensions.Add(new AuthExtension());
        }

        return Task.FromResult<IEnumerable<IExtension>>(extensions);
    }

    /// <inheritdoc />
    public virtual Task<long?> GetMaximumMessageSize(IConnection connection) => Task.FromResult<long?>(null);

    /// <inheritdoc />
    public virtual Task<TimeSpan> GetReceiveTimeout(IConnectionChannel connectionChannel) =>
        Task.FromResult(new TimeSpan(0, 0, 30));

    /// <inheritdoc />
    public virtual Task<TimeSpan> GetSendTimeout(IConnectionChannel connectionChannel) =>
        Task.FromResult(new TimeSpan(0, 0, 30));

    /// <inheritdoc />
    public virtual Task<X509Certificate> GetSSLCertificate(IConnection connection) =>
        Task.FromResult(implcitTlsCertificate ?? startTlsCertificate);

    /// <inheritdoc />
    public virtual Task<bool> IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism) =>
        Task.FromResult(
            EnabledAuthMechanisms.Contains(authMechanism));

    /// <inheritdoc />
    public Task<bool> IsSessionLoggingEnabled(IConnection connection) => Task.FromResult(false);

    /// <inheritdoc />
    public Task<bool> IsSSLEnabled(IConnection connection) => Task.FromResult(implcitTlsCertificate != null);

    /// <inheritdoc />
    public virtual Task OnCommandReceived(IConnection connection, SmtpCommand command) =>
        CommandReceivedEventHandler?.Invoke(this, new CommandEventArgs(command)) ?? Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task<IMessageBuilder> OnCreateNewMessage(IConnection connection) =>
        Task.FromResult<IMessageBuilder>(new MemoryMessageBuilder());

    /// <inheritdoc />
    public virtual Task<IEditableSession> OnCreateNewSession(IConnectionChannel connectionChannel) =>
        Task.FromResult<IEditableSession>(new MemorySession(connectionChannel.ClientIPAddress, DateTime.Now));

    /// <inheritdoc />
    public virtual Task OnMessageCompleted(IConnection connection) =>
        MessageCompletedEventHandler?.Invoke(this, new ConnectionEventArgs(connection)) ?? Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnMessageReceived(IConnection connection, IMessage message) =>
        MessageReceivedEventHandler?.Invoke(this, new MessageEventArgs(message)) ?? Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnMessageRecipientAdding(IConnection connection, IMessageBuilder message, string recipient) =>
        MessageRecipientAddingEventHandler?.Invoke(this, new RecipientAddingEventArgs(message, recipient)) ??
        Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnMessageStart(IConnection connection, string from) =>
        MessageStartEventHandler?.Invoke(this, new MessageStartEventArgs(connection.Session, from)) ??
        Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnSessionCompleted(IConnection connection, ISession session) =>
        SessionCompletedEventHandler?.Invoke(this, new SessionEventArgs(session)) ?? Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnSessionStarted(IConnection connection, ISession session) =>
        SessionStartedEventHandler?.Invoke(this, new SessionEventArgs(session)) ?? Task.CompletedTask;

    /// <inheritdoc />
    public virtual async Task<AuthenticationResult> ValidateAuthenticationCredentials(
        IConnection connection,
        IAuthenticationCredentials authenticationRequest)
    {
        AsyncEventHandler<AuthenticationCredentialsValidationEventArgs> handlers =
            AuthenticationCredentialsValidationRequiredEventHandler;

        if (handlers != null)
        {
            var tasks = handlers.GetInvocationList()
                .Cast<AsyncEventHandler<AuthenticationCredentialsValidationEventArgs>>()
                .Select(h =>
                {
                    AuthenticationCredentialsValidationEventArgs args =
                        new AuthenticationCredentialsValidationEventArgs(connection.Session, authenticationRequest);
                    return new { Args = args, Task = h(this, args) };
                });

            await Task.WhenAll(tasks.Select(t => t.Task).ToArray()).ConfigureAwait(false);

            AuthenticationResult? failureResult = tasks
                .Select(t => t.Args.AuthenticationResult)
                .FirstOrDefault(r => r != AuthenticationResult.Success);

            return failureResult.GetValueOrDefault(AuthenticationResult.Success);
        }

        return AuthenticationResult.Failure;
    }

    /// <summary>
    ///     Occurs when authentication credential provided by the client need to be validated.
    /// </summary>
    public event AsyncEventHandler<AuthenticationCredentialsValidationEventArgs>
        AuthenticationCredentialsValidationRequiredEventHandler;

    /// <summary>
    ///     Occurs when a command is received from a client.
    /// </summary>
    public event AsyncEventHandler<CommandEventArgs> CommandReceivedEventHandler;

    /// <summary>
    ///     Occurs when a message has been requested for a message.
    /// </summary>
    public event AsyncEventHandler<RecipientAddingEventArgs> MessageRecipientAddingEventHandler;

    /// <summary>
    ///     Occurs when a message is received but not yet committed.
    /// </summary>
    public event AsyncEventHandler<ConnectionEventArgs> MessageCompletedEventHandler;

    /// <summary>
    ///     Occurs when a message is received and committed.
    /// </summary>
    public event AsyncEventHandler<MessageEventArgs> MessageReceivedEventHandler;

    /// <summary>
    ///     Occurs when a client session is closed.
    /// </summary>
    public event AsyncEventHandler<SessionEventArgs> SessionCompletedEventHandler;

    /// <summary>
    ///     Occurs when a new session is created, when a client connects to the server.
    /// </summary>
    public event AsyncEventHandler<SessionEventArgs> SessionStartedEventHandler;

    /// <summary>
    ///     Occurs when a new message is started.
    /// </summary>
    public event AsyncEventHandler<MessageStartEventArgs> MessageStartEventHandler;
}
