// <copyright file="SmtpServer.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Logging;
	using Rnwood.SmtpServer.Verbs;

#pragma warning disable CA1724 // Type names should not match namespaces
	/// <summary>
	/// Defines the <see cref="SmtpServer" />.
	/// </summary>
	public class SmtpServer : ISmtpServer
#pragma warning restore CA1724 // Type names should not match namespaces
	{
		/// <summary>
		/// Defines the activeConnections.
		/// </summary>
		private readonly IList activeConnections = ArrayList.Synchronized(new List<Connection>());

		/// <summary>
		/// Defines the logger.
		/// </summary>
		private readonly ILogger logger = Logging.Factory.CreateLogger<SmtpServer>();

		/// <summary>
		/// Defines the nextConnectionEvent.
		/// </summary>
		private readonly AutoResetEvent nextConnectionEvent = new AutoResetEvent(false);

		/// <summary>
		/// Defines the coreTask.
		/// </summary>
		private Task coreTask;

		/// <summary>
		/// Defines the disposedValue.
		/// </summary>
		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Defines the isRunning.
		/// </summary>
		private bool isRunning;

		/// <summary>
		/// Defines the listener.
		/// </summary>
		private TcpListener listener;

		/// <summary>
		/// Initializes a new instance of the <see cref="SmtpServer"/> class.
		/// </summary>
		/// <param name="behaviour">The behaviour<see cref="IServerBehaviour"/>.</param>
		public SmtpServer(IServerBehaviour behaviour)
		{
			this.Behaviour = behaviour;
		}

		/// <summary>
		/// Defines the IsRunningChanged
		/// </summary>
		public event EventHandler IsRunningChanged;

		/// <summary>
		/// Gets the ActiveConnections.
		/// </summary>
		/// <remarks>Note: this is not thread-safe for enumeration.</remarks>
		public IEnumerable<IConnection> ActiveConnections => this.activeConnections.Cast<IConnection>();

		/// <summary>
		/// Gets the Behaviour.
		/// </summary>
		public IServerBehaviour Behaviour { get; private set; }

		/// <summary>
		/// Gets a value indicating whether IsRunning
		/// Gets or sets a value indicating whether the server is currently running.
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return this.isRunning;
			}

			private set
			{
				this.isRunning = value;
				this.IsRunningChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Gets the PortNumber.
		/// </summary>
		public int PortNumber => ((IPEndPoint)this.listener.LocalEndpoint).Port;

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Kills all client connections.
		/// </summary>
		public void KillConnections()
		{
			this.logger.LogDebug("Killing client connections");

			List<Task> killTasks = new List<Task>();
			lock (this.activeConnections.SyncRoot)
			{
				foreach (Connection connection in this.activeConnections.Cast<Connection>().ToArray())
				{
					this.logger.LogDebug("Killing connection {0}", connection);
					killTasks.Add(connection.CloseConnection());
				}
			}

			Task.WaitAll(killTasks.ToArray());
		}

		/// <summary>
		/// Runs the server asynchronously. This method returns once the server has been started.
		/// To stop the server call the <see cref="Stop()"/> method.
		/// </summary>
		public void Start()
		{
			if (this.IsRunning)
			{
				throw new InvalidOperationException("Already running");
			}

			this.logger.LogDebug("Starting server on {0}:{1}", this.Behaviour.IpAddress, this.Behaviour.PortNumber);

			this.listener = new TcpListener(this.Behaviour.IpAddress, this.Behaviour.PortNumber);
			this.listener.Start();

			this.IsRunning = true;

			this.logger.LogDebug("Listener active. Starting core task");

			this.coreTask = Task.Run(() => this.Core().Wait());
		}

		/// <summary>
		/// Stops the running server. Any existing connections are terminated.
		/// </summary>
		public void Stop()
		{
			this.Stop(true);
		}

		/// <summary>
		/// Stops the running server.
		/// This method blocks until all connections have terminated, either by normal completion or timeout,
		/// or if <paramref name="killConnections"/> has been specified then once all of the threads
		/// have been killed.
		/// </summary>
		/// <param name="killConnections">True if existing connections should be terminated.</param>
		public void Stop(bool killConnections)
		{
			if (!this.IsRunning)
			{
				return;
			}

			this.logger.LogDebug("Stopping server");

			this.IsRunning = false;
			this.listener.Stop();

			this.logger.LogDebug("Listener stopped. Waiting for core task to exit");
			this.coreTask.Wait();

			if (killConnections)
			{
				this.KillConnections();

				this.logger.LogDebug("Server is stopped");
			}
			else
			{
				this.logger.LogDebug("Server is stopped but existing connections may still be active");
			}
		}

		/// <summary>
		/// Waits for the next client to connect and blocks until then.
		/// </summary>
		public void WaitForNextConnection()
		{
			this.nextConnectionEvent.WaitOne();
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing">The disposing<see cref="bool" />.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.Stop();
					this.nextConnectionEvent.Close();
				}

				this.disposedValue = true;
			}
		}

		/// <summary>
		/// Creates the verb map which represent the commands implemented by the server.
		/// </summary>
		/// <returns>The <see cref="IVerbMap"/> with registered verbs for the commands.</returns>
		protected virtual IVerbMap CreateVerbMap()
		{
			VerbMap verbMap = new VerbMap();
			verbMap.SetVerbProcessor("HELO", new HeloVerb());
			verbMap.SetVerbProcessor("EHLO", new EhloVerb());
			verbMap.SetVerbProcessor("QUIT", new QuitVerb());
			verbMap.SetVerbProcessor("MAIL", new MailVerb());
			verbMap.SetVerbProcessor("RCPT", new RcptVerb());
			verbMap.SetVerbProcessor("DATA", new DataVerb());
			verbMap.SetVerbProcessor("RSET", new RsetVerb());
			verbMap.SetVerbProcessor("NOOP", new NoopVerb());

			return verbMap;
		}

		private async Task AcceptNextClient()
		{
			TcpClient tcpClient = null;
			try
			{
				tcpClient = await this.listener.AcceptTcpClientAsync().ConfigureAwait(false);
			}
			catch (InvalidOperationException)
			{
				if (this.IsRunning)
				{
					throw;
				}

				this.logger.LogDebug("Got InvalidOperationException on listener, shutting down");

				// normal - caused by _listener.Stop();
			}

			if (this.IsRunning)
			{
				this.logger.LogDebug("New connection from {0}", tcpClient.Client.RemoteEndPoint);

				TcpClientConnectionChannel connectionChannel = new TcpClientConnectionChannel(tcpClient, this.Behaviour.FallbackEncoding);
				connectionChannel.ReceiveTimeout = await this.Behaviour.GetReceiveTimeout(connectionChannel).ConfigureAwait(false);
				connectionChannel.SendTimeout = await this.Behaviour.GetSendTimeout(connectionChannel).ConfigureAwait(false);

				Connection connection = await Connection.Create(this, connectionChannel, this.CreateVerbMap()).ConfigureAwait(false);
				this.activeConnections.Add(connection);
				connection.ConnectionClosedEventHandler += (s, ea) =>
				{
					this.logger.LogDebug("Connection {0} handling completed removing from active connections", connection);
					this.activeConnections.Remove(connection);
					return Task.CompletedTask;
				};
#pragma warning disable 4014
				connection.ProcessAsync();
#pragma warning restore 4014
			}
		}

		private async Task Core()
		{
			this.logger.LogDebug("Core task running");

			while (this.IsRunning)
			{
				this.logger.LogDebug("Waiting for new client");

				await this.AcceptNextClient().ConfigureAwait(false);

				this.nextConnectionEvent.Set();
			}
		}
	}
}