// <copyright file="DefaultServerBehaviour.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security.Cryptography.X509Certificates;
	using System.Text;
	using System.Threading.Tasks;
	using Rnwood.SmtpServer.Extensions;
	using Rnwood.SmtpServer.Extensions.Auth;

	/// <summary>
	/// Implements a default <see cref="IServerBehaviour"/> which is suitable for many basic uses.
	/// </summary>
	/// <seealso cref="Rnwood.SmtpServer.IServerBehaviour" />
	public class DefaultServerBehaviour : IServerBehaviour
	{
		private readonly bool allowRemoteConnections;

		private readonly X509Certificate sslCertificate;

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultServerBehaviour"/> class.
		/// </summary>
		/// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
		public DefaultServerBehaviour(bool allowRemoteConnections)
			: this(allowRemoteConnections, 25, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultServerBehaviour"/> class.
		/// </summary>
		/// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
		/// <param name="portNumber">The port number.</param>
		public DefaultServerBehaviour(bool allowRemoteConnections, int portNumber)
			: this(allowRemoteConnections, portNumber, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultServerBehaviour"/> class.
		/// </summary>
		/// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
		/// <param name="portNumber">The port number.</param>
		/// <param name="sslCertificate">The SSL certificate.</param>
		public DefaultServerBehaviour(bool allowRemoteConnections, int portNumber, X509Certificate sslCertificate)
		{
			this.PortNumber = portNumber;
			this.sslCertificate = sslCertificate;
			this.allowRemoteConnections = allowRemoteConnections;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultServerBehaviour"/> class.
		/// </summary>
		/// <param name="allowRemoteConnections">if set to <c>true</c> remote connections to the server are allowed.</param>
		/// <param name="sslCertificate">The SSL certificate.</param>
		public DefaultServerBehaviour(bool allowRemoteConnections, X509Certificate sslCertificate)
			: this(allowRemoteConnections, 587, sslCertificate)
		{
		}

		/// <summary>
		/// Occurs when authentication credential provided by the client need to be validated.
		/// </summary>
		public event AsyncEventHandler<AuthenticationCredentialsValidationEventArgs> AuthenticationCredentialsValidationRequiredAsync;

		/// <summary>
		/// Occurs when a command is received from a client.
		/// </summary>
		public event AsyncEventHandler<CommandEventArgs> CommandReceivedEventHandler;

		/// <summary>
		/// Occurs when a message is received but not yet committed.
		/// </summary>
		public event AsyncEventHandler<ConnectionEventArgs> MessageCompletedEventHandler;

		/// <summary>
		/// Occurs when a message is received and committed.
		/// </summary>
		public event AsyncEventHandler<MessageEventArgs> MessageReceivedEventHandler;

		/// <summary>
		/// Occurs when a client session is closed.
		/// </summary>
		public event AsyncEventHandler<SessionEventArgs> SessionCompletedEventHandler;

		/// <summary>
		/// Occurs when a new session is created, when a client connects to the server.
		/// </summary>
		public event AsyncEventHandler<SessionEventArgs> SessionStartedEventHandler;

		/// <inheritdoc/>
		public virtual string DomainName => Environment.MachineName;

		/// <inheritdoc/>
		public virtual IPAddress IpAddress => this.allowRemoteConnections ? IPAddress.Any : IPAddress.Loopback;

		/// <inheritdoc/>
		public int MaximumNumberOfSequentialBadCommands => 10;

		/// <inheritdoc/>
		public virtual int PortNumber { get; private set; }

		/// <inheritdoc/>
		public virtual Encoding FallbackEncoding => Encoding.GetEncoding("iso-8859-1");

		/// <inheritdoc/>
		public virtual Task<IEnumerable<IExtension>> GetExtensions(IConnectionChannel connectionChannel)
		{
			List<IExtension> extensions = new List<IExtension>(new IExtension[] { new EightBitMimeExtension(), new SizeExtension(), new SmtpUtfEightExtension() });

			if (this.sslCertificate != null)
			{
				extensions.Add(new StartTlsExtension());
			}

			return Task.FromResult<IEnumerable<IExtension>>(extensions);
		}

		/// <inheritdoc/>
		public virtual Task<long?> GetMaximumMessageSize(IConnection connection)
		{
			return Task.FromResult<long?>(null);
		}

		/// <inheritdoc/>
		public virtual Task<TimeSpan> GetReceiveTimeout(IConnectionChannel connectionChannel)
		{
			return Task.FromResult(new TimeSpan(0, 0, 30));
		}

		/// <inheritdoc/>
		public virtual Task<TimeSpan> GetSendTimeout(IConnectionChannel connectionChannel)
		{
			return Task.FromResult(new TimeSpan(0, 0, 30));
		}

		/// <inheritdoc/>
		public virtual Task<X509Certificate> GetSSLCertificate(IConnection connection)
		{
			return Task.FromResult(this.sslCertificate);
		}

		/// <inheritdoc/>
		public virtual Task<bool> IsAuthMechanismEnabled(IConnection connection, IAuthMechanism authMechanism)
		{
			return Task.FromResult(false);
		}

		/// <inheritdoc/>
		public Task<bool> IsSessionLoggingEnabled(IConnection connection)
		{
			return Task.FromResult(false);
		}

		/// <inheritdoc/>
		public Task<bool> IsSSLEnabled(IConnection connection)
		{
			return Task.FromResult(this.sslCertificate != null);
		}

		/// <inheritdoc/>
		public virtual Task OnCommandReceived(IConnection connection, SmtpCommand command)
		{
			this.CommandReceivedEventHandler?.Invoke(this, new CommandEventArgs(command));

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual Task<IMessageBuilder> OnCreateNewMessage(IConnection connection)
		{
			return Task.FromResult<IMessageBuilder>(new MemoryMessageBuilder());
		}

		/// <inheritdoc/>
		public Task<IEditableSession> OnCreateNewSession(IConnectionChannel connection)
		{
			return Task.FromResult<IEditableSession>(new MemorySession(connection.ClientIPAddress, DateTime.Now));
		}

		/// <inheritdoc/>
		public virtual Task OnMessageCompleted(IConnection connection)
		{
			return this.MessageCompletedEventHandler?.Invoke(this, new ConnectionEventArgs(connection)) ?? Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual Task OnMessageReceived(IConnection connection, IMessage message)
		{
			return this.MessageReceivedEventHandler?.Invoke(this, new MessageEventArgs(message)) ?? Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual Task OnMessageRecipientAdding(IConnection connection, IMessageBuilder message, string recipient)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual Task OnMessageStart(IConnection connection, string from)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual Task OnSessionCompleted(IConnection connection, ISession session)
		{
			return this.SessionCompletedEventHandler?.Invoke(this, new SessionEventArgs(session)) ?? Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual Task OnSessionStarted(IConnection connection, ISession session)
		{
			return this.SessionStartedEventHandler?.Invoke(this, new SessionEventArgs(session)) ?? Task.CompletedTask;
		}

		/// <inheritdoc/>
		public virtual async Task<AuthenticationResult> ValidateAuthenticationCredentials(
			IConnection connection,
			IAuthenticationCredentials request)
		{
			var handlers = this.AuthenticationCredentialsValidationRequiredAsync;

			if (handlers != null)
			{
				var tasks = handlers.GetInvocationList()
					.Cast<AsyncEventHandler<AuthenticationCredentialsValidationEventArgs>>()
					.Select(h =>
					{
						AuthenticationCredentialsValidationEventArgs args = new AuthenticationCredentialsValidationEventArgs(request);
						return new { Args = args, Task = h(this, args) };
					});

				await Task.WhenAll(tasks.Select(t => t.Task).ToArray()).ConfigureAwait(false);

				AuthenticationResult? failureResult = tasks.Select(t => t.Args.AuthenticationResult)
					.Where(r => r != AuthenticationResult.Success)
					.FirstOrDefault();

				return failureResult ?? AuthenticationResult.Success;
			}

			return AuthenticationResult.Failure;
		}
	}
}
