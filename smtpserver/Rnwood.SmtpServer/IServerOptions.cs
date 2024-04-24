// <copyright file="IServerOptions.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="IServerOptions" />.
/// </summary>
public interface IServerOptions
{
    /// <summary>
    ///     Gets the DomainName
    ///     Gets domain name reported by the server to clients.
    /// </summary>
    string DomainName { get; }

    /// <summary>
    ///     Gets the IP address on which to listen for connections.
    /// </summary>
    IPAddress IpAddress { get; }

    /// <summary>
    ///     Gets the max number of sequential bad commands before the client will be disconnected.
    /// </summary>
    int MaximumNumberOfSequentialBadCommands { get; }

    /// <summary>
    ///     Gets the TCP port number on which to listen for connections.
    /// </summary>
    int PortNumber { get; }

    /// <summary>
    ///     Gets an encoding which will be used if bytes received from the client cannot be decoded as ASCII/UTF-8.
    /// </summary>
    Encoding FallbackEncoding { get; }

    event AsyncEventHandler<AuthenticationCredentialsValidationEventArgs> AuthenticationCredentialsValidationRequiredEventHandler;
    event AsyncEventHandler<ConnectionEventArgs> MessageCompletedEventHandler;
    event AsyncEventHandler<MessageEventArgs> MessageReceivedEventHandler;
    event AsyncEventHandler<SessionEventArgs> SessionCompletedEventHandler;
    event AsyncEventHandler<SessionEventArgs> SessionStartedEventHandler;

    /// <summary>
    ///     Gets the extensions that should be enabled for the specified connection.
    /// </summary>
    /// <param name="connectionChannel">The connectionChannel<see cref="IConnectionChannel" />.</param>
    /// <returns>A <see cref="Task{T}" /> resulting in a sequence of <see cref="IExtension" /> for the extensions.</returns>
    Task<IEnumerable<IExtension>> GetExtensions(IConnectionChannel connectionChannel);

    /// <summary>
    ///     Gets the maximum allowed size of the message for the specified connection.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<long?> GetMaximumMessageSize(IConnection connection);

    /// <summary>
    ///     Gets the receive timeout that should be used for the specified connection.
    /// </summary>
    /// <param name="connectionChannel">The connection channel.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<TimeSpan> GetReceiveTimeout(IConnectionChannel connectionChannel);

    /// <summary>
    ///     Gets the send timeout that should be used for the specified connection.
    /// </summary>
    /// <param name="connectionChannel">The connectionChannel<see cref="IConnectionChannel" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<TimeSpan> GetSendTimeout(IConnectionChannel connectionChannel);

    /// <summary>
    ///     Gets the SSL certificate that should be used for the specified connection.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<X509Certificate> GetSSLCertificate(IConnection connection);

    /// <summary>
    ///     Determines whether the specified auth mechanism should be enabled for the specified connection.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="authMechanism">The auth mechanism.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<bool> IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism);

    /// <summary>
    ///     Gets a value indicating whether session logging should be enabled for the given connection.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<bool> IsSessionLoggingEnabled(IConnection connection);

    /// <summary>
    ///     Gets a value indicating whether to run in SSL mode.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task<bool> IsSSLEnabled(IConnection connection);

    /// <summary>
    ///     Called when a command received in the specified SMTP session.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="command">The command.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task OnCommandReceived(IConnection connection, SmtpCommand command);

    /// <summary>
    ///     Called when a new message is started using the MAIL FROM command and must returns the instance of
    ///     <see cref="IMessageBuilder" /> which will be used to record the message.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<IMessageBuilder> OnCreateNewMessage(IConnection connection);

    /// <summary>
    ///     Called when a new session is started and must return an object which is used to record details about that session.
    /// </summary>
    /// <param name="connectionChannel">The connectionChannel<see cref="IConnectionChannel" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task<IEditableSession> OnCreateNewSession(IConnectionChannel connectionChannel);

    /// <summary>
    ///     Called when a message is received but not committed.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task OnMessageCompleted(IConnection connection);

    /// <summary>
    ///     Called when a new message is received by the server.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="message">The message<see cref="IMessage" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task OnMessageReceived(IConnection connection, IMessage message);

    /// <summary>
    ///     Called when a new recipient is requested for a message using the MAIL FROM command.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="message">The message<see cref="IMessageBuilder" />.</param>
    /// <param name="recipient">The recipient<see cref="string" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    Task OnMessageRecipientAdding(IConnection connection, IMessageBuilder message, string recipient);

    /// <summary>
    ///     Called when a new message is started in the specified session.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="from">From.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task OnMessageStart(IConnection connection, string from);

    /// <summary>
    ///     Called when a SMTP session is completed.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="session">The session.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task OnSessionCompleted(IConnection connection, ISession session);

    /// <summary>
    ///     Called when a new SMTP session is started.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="session">The session.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task OnSessionStarted(IConnection connection, ISession session);

    /// <summary>
    ///     Validates the authentication request to determine if the supplied details
    ///     are correct.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="authenticationRequest">The authentication request.</param>
    /// <returns>A <see cref="Task" /> representing the async operation.</returns>
    Task<AuthenticationResult> ValidateAuthenticationCredentials(
        IConnection connection,
        IAuthenticationCredentials authenticationRequest);
}
