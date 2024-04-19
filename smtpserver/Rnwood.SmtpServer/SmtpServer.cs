// <copyright file="SmtpServer.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

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

namespace Rnwood.SmtpServer;

#pragma warning disable CA1724 // Type names should not match namespaces
/// <summary>
///     Defines the <see cref="SmtpServer" />.
/// </summary>
public class SmtpServer : ISmtpServer
#pragma warning restore CA1724 // Type names should not match namespaces
{
    /// <summary>
    ///     Defines the activeConnections.
    /// </summary>
    private readonly IList activeConnections = ArrayList.Synchronized(new List<Connection>());

    /// <summary>
    ///     Defines the logger.
    /// </summary>
    private readonly ILogger logger = Logging.Factory.CreateLogger<SmtpServer>();

    /// <summary>
    ///     Defines the nextConnectionEvent.
    /// </summary>
    private readonly AutoResetEvent nextConnectionEvent = new(false);

    /// <summary>
    ///     Defines the coreTask.
    /// </summary>
    private Task coreTask;

    /// <summary>
    ///     Defines the disposedValue.
    /// </summary>
    private bool disposedValue; // To detect redundant calls

    /// <summary>
    ///     Defines the isRunning.
    /// </summary>
    private bool isRunning;

    /// <summary>
    ///     Defines the listener.
    /// </summary>
    private TcpListener[] listeners;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpServer" /> class.
    /// </summary>
    /// <param name="options">The Options<see cref="IServerOptions" />.</param>
    public SmtpServer(IServerOptions options)
    {
        this.Options = options;
    }

    /// <summary>
    ///     Gets the ActiveConnections.
    /// </summary>
    /// <remarks>Note: this is not thread-safe for enumeration.</remarks>
    public IEnumerable<IConnection> ActiveConnections => activeConnections.Cast<IConnection>();

    /// <summary>
    ///     Gets a value indicating whether IsRunning
    ///     Gets or sets a value indicating whether the server is currently running.
    /// </summary>
    public bool IsRunning
    {
        get => isRunning;

        private set
        {
            isRunning = value;
            IsRunningChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public IPEndPoint[] ListeningEndpoints => listeners.Select(l => (IPEndPoint)l.LocalEndpoint).ToArray();

    /// <summary>
    ///     Gets the Options.
    /// </summary>
    public IServerOptions Options { get; }


    /// <summary>
    ///     Occurs when authentication results need to be validated.
    /// </summary>
    public event AsyncEventHandler<AuthenticationCredentialsValidationEventArgs>
        AuthenticationCredentialsValidationRequiredEventHandler
    {
        add => Options.AuthenticationCredentialsValidationRequiredEventHandler += value;
        remove => Options.AuthenticationCredentialsValidationRequiredEventHandler -= value;
    }

    /// <summary>
    ///     Occurs when a message has been fully received but not yet acknowledged by the server.
    /// </summary>
    public event AsyncEventHandler<ConnectionEventArgs> MessageCompletedEventHandler
    {
        add => Options.MessageCompletedEventHandler += value;
        remove => Options.MessageCompletedEventHandler -= value;
    }

    /// <summary>
    ///     Occurs when a message has been received and acknowledged by the server.
    /// </summary>
    public event AsyncEventHandler<MessageEventArgs> MessageReceivedEventHandler
    {
        add => Options.MessageReceivedEventHandler += value;
        remove => Options.MessageReceivedEventHandler -= value;
    }

    /// <summary>
    ///     Occurs when a session is terminated.
    /// </summary>
    public event AsyncEventHandler<SessionEventArgs> SessionCompletedEventHandler
    {
        add => Options.SessionCompletedEventHandler += value;
        remove => Options.SessionCompletedEventHandler -= value;
    }

    /// <summary>
    ///     Occurs when a new session is started, when a new client connects to the server.
    /// </summary>
    public event AsyncEventHandler<SessionEventArgs> SessionStartedHandler
    {
        add => Options.SessionStartedEventHandler += value;
        remove => Options.SessionStartedEventHandler -= value;
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Defines the IsRunningChanged
    /// </summary>
    public event EventHandler IsRunningChanged;

    /// <summary>
    ///     Kills all client connections.
    /// </summary>
    public void KillConnections()
    {
        logger.LogDebug("Killing client connections");

        List<Task> killTasks = new List<Task>();
        lock (activeConnections.SyncRoot)
        {
            foreach (Connection connection in activeConnections.Cast<Connection>().ToArray())
            {
                logger.LogDebug("Killing connection {0}", connection);
                killTasks.Add(connection.CloseConnection());
            }
        }

        Task.WaitAll(killTasks.ToArray());
    }

    /// <summary>
    ///     Runs the server asynchronously. This method returns once the server has been started.
    ///     To stop the server call the <see cref="Stop()" /> method.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Already running");
        }

        logger.LogDebug("Starting server on {0}:{1}", Options.IpAddress, Options.PortNumber);

        if (Options.IpAddress == IPAddress.IPv6Loopback)
        {
            //https://stackoverflow.com/questions/37729475/create-dual-stack-socket-on-all-loopback-interfaces-on-windows
            listeners = new[]
            {
                new TcpListener(IPAddress.IPv6Loopback, Options.PortNumber),
                new TcpListener(IPAddress.Loopback, Options.PortNumber)
            };
        }
        else
        {

            listeners = new[] { new TcpListener(Options.IpAddress, Options.PortNumber) };
            if (Options.IpAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                listeners[0].Server.DualMode = true;
            }
        }

        try
        {
            foreach(var l in listeners)
            {
                l.Start();
            }
        } catch
        {
            foreach (var l in listeners)
            {
                l.Stop();
            }

            throw;
        }
        pendingAcceptTasks = new Task<TcpClient>[listeners.Length];

        IsRunning = true;

        logger.LogDebug("Listener active. Starting core task");

        coreTask = Task.Run(() => Core().Wait());
    }

    /// <summary>
    ///     Stops the running server. Any existing connections are terminated.
    /// </summary>
    public void Stop() => Stop(true);

    /// <summary>
    ///     Stops the running server.
    ///     This method blocks until all connections have terminated, either by normal completion or timeout,
    ///     or if <paramref name="killConnections" /> has been specified then once all of the threads
    ///     have been killed.
    /// </summary>
    /// <param name="killConnections">True if existing connections should be terminated.</param>
    public void Stop(bool killConnections)
    {
        if (!IsRunning)
        {
            return;
        }

        logger.LogDebug("Stopping server");

        IsRunning = false;
        foreach (var l in listeners)
        {
            l.Stop();
        }

        logger.LogDebug("Listener stopped. Waiting for core task to exit");
        coreTask.Wait();

        if (killConnections)
        {
            KillConnections();

            logger.LogDebug("Server is stopped");
        }
        else
        {
            logger.LogDebug("Server is stopped but existing connections may still be active");
        }
    }

    /// <summary>
    ///     Waits for the next client to connect and blocks until then.
    /// </summary>
    public void WaitForNextConnection() => nextConnectionEvent.WaitOne();

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">The disposing<see cref="bool" />.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Stop();
                nextConnectionEvent.Close();
            }

            disposedValue = true;
        }
    }

    /// <summary>
    ///     Creates the verb map which represent the commands implemented by the server.
    /// </summary>
    /// <returns>The <see cref="IVerbMap" /> with registered verbs for the commands.</returns>
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

    private Task<TcpClient>[] pendingAcceptTasks;

    private async Task AcceptNextClient()
    {
        TcpClient tcpClient = null;
        try
        {
            for(int i=0; i<listeners.Length; i++)
            {
                if (pendingAcceptTasks[i] == null)
                {
                    pendingAcceptTasks[i] = listeners[i].AcceptTcpClientAsync();
                }
            }

            var completedTaskIdx = Task.WaitAny(pendingAcceptTasks);
            var completedTask = pendingAcceptTasks[completedTaskIdx];
            pendingAcceptTasks[completedTaskIdx] = null;

            tcpClient = await completedTask;
        }
        catch (SocketException)
        {
            if (IsRunning)
            {
                throw;
            }

            logger.LogDebug("Got SocketException on listener, shutting down");
        }
        catch (InvalidOperationException)
        {
            if (IsRunning)
            {
                throw;
            }

            logger.LogDebug("Got InvalidOperationException on listener, shutting down");
        }

        if (IsRunning)
        {
            logger.LogDebug("New connection from {0}", tcpClient.Client.RemoteEndPoint);

            TcpClientConnectionChannel connectionChannel =
                new TcpClientConnectionChannel(tcpClient, Options.FallbackEncoding);
            connectionChannel.ReceiveTimeout =
                await Options.GetReceiveTimeout(connectionChannel).ConfigureAwait(false);
            connectionChannel.SendTimeout = await Options.GetSendTimeout(connectionChannel).ConfigureAwait(false);

            Connection connection =
                await Connection.Create(this, connectionChannel, CreateVerbMap()).ConfigureAwait(false);
            activeConnections.Add(connection);
            connection.ConnectionClosedEventHandler += (s, ea) =>
            {
                logger.LogDebug("Connection {0} handling completed removing from active connections", connection);
                activeConnections.Remove(connection);
                return Task.CompletedTask;
            };
#pragma warning disable 4014
            connection.ProcessAsync();
#pragma warning restore 4014
        }
    }

    private async Task Core()
    {
        logger.LogDebug("Core task running");

        while (IsRunning)
        {
            logger.LogDebug("Waiting for new client");

            await AcceptNextClient().ConfigureAwait(false);

            nextConnectionEvent.Set();
        }
    }
}
