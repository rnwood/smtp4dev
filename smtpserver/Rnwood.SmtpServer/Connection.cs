// <copyright file="Connection.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Net.Security;
	using System.Reflection;
	using System.Runtime.Versioning;
	using System.Security.Authentication;
	using System.Text;
	using System.Threading.Tasks;
	using Rnwood.SmtpServer.Extensions;
	using Rnwood.SmtpServer.Verbs;

	/// <summary>
	/// Represents a single SMTP server from a client to the server.
	/// </summary>
	public class Connection : IConnection
	{
		private readonly string id;

		/// <summary>
		/// Initializes a new instance of the <see cref="Connection"/> class.
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="session">The session.</param>
		/// <param name="connectionChannel">The connection channel.</param>
		/// <param name="verbMap">The verb map.</param>
		/// <param name="extensionProcessors">The extension processors.</param>
		internal Connection(ISmtpServer server, IEditableSession session, IConnectionChannel connectionChannel, IVerbMap verbMap, Func<IConnection, IExtensionProcessor[]> extensionProcessors)
		{
			this.id = $"[RemoteIP={connectionChannel.ClientIPAddress}]";

			this.ConnectionChannel = connectionChannel;
			this.ConnectionChannel.ClosedEventHandler += this.OnConnectionChannelClosed;

			this.VerbMap = verbMap;
			this.Session = session;
			this.Server = server;
			this.ExtensionProcessors = extensionProcessors(this).ToArray();
		}

		/// <inheritdoc/>
		public event AsyncEventHandler<ConnectionEventArgs> ConnectionClosedEventHandler;

		/// <inheritdoc/>
		public IMessageBuilder CurrentMessage { get; private set; }

		/// <inheritdoc/>
		public MailVerb MailVerb => (MailVerb)this.VerbMap.GetVerbProcessor("MAIL");

		/// <inheritdoc/>
		public ISmtpServer Server { get; private set; }

		/// <inheritdoc/>
		public IEditableSession Session { get; private set; }

		/// <inheritdoc/>
		public IVerbMap VerbMap { get; private set; }

		/// <summary>
		/// Gets a list of extensions which are available for this connection.
		/// </summary>
		public IReadOnlyCollection<IExtensionProcessor> ExtensionProcessors { get; private set; }

		private IConnectionChannel ConnectionChannel { get; set; }

		/// <inheritdoc/>
		public Task AbortMessage()
		{
			this.CurrentMessage = null;
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task ApplyStreamFilter(Func<Stream, Task<Stream>> filter)
		{
			await this.ConnectionChannel.ApplyStreamFilter(filter).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task CloseConnection()
		{
			await this.ConnectionChannel.Close().ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task CommitMessage()
		{
			IMessage message = await this.CurrentMessage.ToMessage().ConfigureAwait(false);
			this.Session.AddMessage(message);
			this.CurrentMessage = null;

			await this.Server.Behaviour.OnMessageReceived(this, message).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<IMessageBuilder> NewMessage()
		{
			this.CurrentMessage = await this.Server.Behaviour.OnCreateNewMessage(this).ConfigureAwait(false);
			this.CurrentMessage.Session = this.Session;
			return this.CurrentMessage;
		}

		/// <inheritdoc/>
		public async Task<string> ReadLine()
		{
			string text = await this.ConnectionChannel.ReadLine().ConfigureAwait(false);
			await this.Session.AppendLineToSessionLog(text).ConfigureAwait(false);
			return text;
		}

		/// <summary>
		/// Returns a <see cref="string" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="string" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return this.id;
		}

		/// <inheritdoc/>
		public async Task WriteResponse(SmtpResponse response)
		{
			await this.WriteLineAndFlush(response.ToString().TrimEnd()).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates the a connection for the specified server and channel..
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="connectionChannel">The connection channel.</param>
		/// <param name="verbMap">The verb map.</param>
		/// <returns>An <see cref="Task{T}"/> representing the async operation.</returns>
		internal static async Task<Connection> Create(ISmtpServer server, IConnectionChannel connectionChannel, IVerbMap verbMap)
		{
			IEditableSession session = await server.Behaviour.OnCreateNewSession(connectionChannel).ConfigureAwait(false);
			var extensions = await server.Behaviour.GetExtensions(connectionChannel).ConfigureAwait(false);
			IExtensionProcessor[] CreateConnectionExtensions(IConnection c) => extensions.Select(e => e.CreateExtensionProcessor(c)).ToArray();
			Connection result = new Connection(server, session, connectionChannel, verbMap, CreateConnectionExtensions);
			return result;
		}

		internal async Task<Stream> StartImplicitTls(Stream s)
		{
			SslStream sslStream = new SslStream(s);

			SslProtocols sslProtos;

			string ver = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
			if (ver == null || !ver.StartsWith(".NETCoreApp,"))
			{
				sslProtos = SslProtocols.Tls12 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Ssl3 | SslProtocols.Ssl2;
			}
			else
			{
				sslProtos = SslProtocols.None;
			}

			System.Security.Cryptography.X509Certificates.X509Certificate cert = 
				await this.Server.Behaviour.GetSSLCertificate(this).ConfigureAwait(false);

			await sslStream.AuthenticateAsServerAsync(cert, false, sslProtos, false).ConfigureAwait(false);
			return sslStream;
		}

		/// <summary>
		/// Starts processing of this connection.
		/// </summary>
		/// <returns>A <see cref="Task{T}"/> representing the async operation.</returns>
		internal async Task ProcessAsync()
		{
			try
			{
				await this.Server.Behaviour.OnSessionStarted(this, this.Session).ConfigureAwait(false);

				if (await this.Server.Behaviour.IsSSLEnabled(this).ConfigureAwait(false))
				{

					await this.ConnectionChannel.ApplyStreamFilter(this.StartImplicitTls).ConfigureAwait(false);

					this.Session.SecureConnection = true;
				}

				await this.WriteResponse(new SmtpResponse(
					StandardSmtpResponseCode.ServiceReady,
					this.Server.Behaviour.DomainName + " smtp4dev ready")).ConfigureAwait(false);

				int numberOfInvalidCommands = 0;
				while (this.ConnectionChannel.IsConnected)
				{
					bool badCommand = false;
					SmtpCommand command = new SmtpCommand(await this.ReadLine().ConfigureAwait(false));
					await this.Server.Behaviour.OnCommandReceived(this, command).ConfigureAwait(false);

					if (command.IsValid)
					{
						IVerb verbProcessor = this.VerbMap.GetVerbProcessor(command.Verb);

						if (verbProcessor != null)
						{
							try
							{
								await verbProcessor.Process(this, command).ConfigureAwait(false);
							}
							catch (SmtpServerException exception)
							{
								await this.WriteResponse(exception.SmtpResponse).ConfigureAwait(false);
							}
						}
						else
						{
							badCommand = true;
						}
					}
					else if (command.IsEmpty)
					{
					}
					else
					{
						badCommand = true;
					}

					if (badCommand)
					{
						numberOfInvalidCommands++;

						if (this.Server.Behaviour.MaximumNumberOfSequentialBadCommands > 0 &&
						numberOfInvalidCommands >= this.Server.Behaviour.MaximumNumberOfSequentialBadCommands)
						{
							await this.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel, "Too many bad commands. Bye!")).ConfigureAwait(false);
							await this.CloseConnection().ConfigureAwait(false);
						}
						else
						{
							await this.WriteResponse(new SmtpResponse(
								StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
								"Command unrecognised")).ConfigureAwait(false);
						}
					}
				}
			}
			catch (IOException ioException)
			{
				this.Session.SessionError = ioException;
				this.Session.SessionErrorType = SessionErrorType.NetworkError;
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception exception)
			{
				this.Session.SessionError = exception;
				this.Session.SessionErrorType = SessionErrorType.UnexpectedException;
			}
#pragma warning restore CA1031 // Do not catch general exception types

			await this.CloseConnection().ConfigureAwait(false);

			this.Session.EndDate = DateTime.Now;
			await this.Server.Behaviour.OnSessionCompleted(this, this.Session).ConfigureAwait(false);
		}

		/// <summary>
		/// Writes a line of text to the client.
		/// </summary>
		/// <param name="text">The text<see cref="string" /> optionally containing placeholders into which <paramref name="args" /> are subtituted using <see cref="string.Format(string, object[])" />.</param>
		/// <param name="args">The arguments which are formatted into <paramref name="text"/>.</param>
		/// <returns>
		/// The <see cref="Task" />.
		/// </returns>
		protected async Task WriteLineAndFlush(string text, params object[] args)
		{
			string formattedText = string.Format(CultureInfo.InvariantCulture, text, args);
			await this.Session.AppendLineToSessionLog(formattedText).ConfigureAwait(false);
			await this.ConnectionChannel.WriteLine(formattedText).ConfigureAwait(false);
			await this.ConnectionChannel.Flush().ConfigureAwait(false);
		}

		private async Task OnConnectionChannelClosed(object sender, EventArgs eventArgs)
		{
			ConnectionEventArgs connEventArgs = new ConnectionEventArgs(this);

			foreach (Delegate handler
				in this.ConnectionClosedEventHandler?.GetInvocationList() ?? Enumerable.Empty<Delegate>())
			{
				await ((Task)handler.DynamicInvoke(this, connEventArgs)).ConfigureAwait(false);
			}
		}
	}
}
