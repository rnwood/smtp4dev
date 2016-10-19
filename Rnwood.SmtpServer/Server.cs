#region

using Microsoft.Extensions.Logging;
using Rnwood.SmtpServer.Verbs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class Server : IServer
    {
        private ILogger _logger = Logging.Factory.CreateLogger<Server>();
        private TcpListener _listener;

        public Server(IServerBehaviour behaviour)
        {
            Behaviour = behaviour;
        }

        public IServerBehaviour Behaviour { get; private set; }

        private bool _isRunning;

        /// <summary>
        /// Gets or sets a value indicating whether the server is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            private set
            {
                _isRunning = value;
                IsRunningChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler IsRunningChanged;

        public int PortNumber
        {
            get
            {
                return ((IPEndPoint)_listener.LocalEndpoint).Port;
            }
        }

        private IVerbMap GetVerbMap()
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

        private async void Core()
        {
            _logger.LogDebug("Core task running");

            try
            {
                while (IsRunning)
                {
                    _logger.LogDebug("Waiting for new client");

                    await _currentAcceptTask;
                    _currentAcceptTask = AcceptNextClient();
                }
            }
            finally
            {
                _currentAcceptTask = null;
            }
        }

        public async Task WaitForNextConnectionAsync()
        {
            await _currentAcceptTask;
        }

        private async Task AcceptNextClient()
        {
            TcpClient tcpClient = null;
            try
            {
                tcpClient = await _listener.AcceptTcpClientAsync();
            }
            catch (InvalidOperationException)
            {
                if (IsRunning)
                {
                    throw;
                }

                _logger.LogDebug("Got InvalidOperationException on listener, shutting down");
                //normal - caused by _listener.Stop();
            }

            if (IsRunning)
            {
                _logger.LogDebug("New connection from {0}", tcpClient.Client.RemoteEndPoint);

                Connection connection = new Connection(this, new TcpClientConnectionChannel(tcpClient), GetVerbMap());
                _activeConnections.Add(connection);
                connection.ConnectionClosed += (s, ea) =>
                {
                    _logger.LogDebug("Connection {0} handling completed removing from active connections", connection);
                    _activeConnections.Remove(connection);
                };
                connection.ProcessAsync();
            }
        }

        /// <summary>
        /// Runs the server asynchronously. This method returns once the server has been started.
        /// To stop the server call the <see cref="Stop()"/> method.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">if the server is already running.</exception>
        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Already running");

            _logger.LogDebug("Starting server on {0}:{1}", Behaviour.IpAddress, Behaviour.PortNumber);

            _listener = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            _listener.Start();

            IsRunning = true;

            _logger.LogDebug("Listener active. Starting core task");

            _currentAcceptTask = AcceptNextClient();
            _coreTask = Task.Run(() => Core());
        }

        /// <summary>
        /// Stops the running server. Any existing connections are terminated.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">if the server is not running.</exception>
        public void Stop()
        {
            Stop(true);
        }

        /// <summary>
        /// Stops the running server.
        /// This method blocks until all connections have terminated, either by normal completion or timeout,
        /// or if <paramref name="killConnections"/> has been specified then once all of the threads
        /// have been killed.
        /// </summary>
        /// <param name="killConnections">True if existing connections should be terminated.</param>
        /// <exception cref="System.InvalidOperationException">if the server is not running.</exception>
        public void Stop(bool killConnections)
        {
            if (!IsRunning)
            {
                return;
            }

            _logger.LogDebug("Stopping server");

            IsRunning = false;
            _listener.Stop();

            _logger.LogDebug("Listener stopped. Waiting for core task to exit");
            _coreTask.Wait();

            if (killConnections)
            {
                KillConnections();

                _logger.LogDebug("Server is stopped");
            }
            else
            {
                _logger.LogDebug("Server is stopped but existing connections may still be active");
            }
        }

        public void KillConnections()
        {
            _logger.LogDebug("Killing client connections");

            List<Task> killTasks = new List<Task>();
            foreach (Connection connection in _activeConnections.Cast<Connection>().ToArray())
            {
                _logger.LogDebug("Killing connection {0}", connection);
                killTasks.Add(connection.CloseConnectionAsync());
            }
            Task.WaitAll(killTasks.ToArray());
        }

        private readonly IList _activeConnections = ArrayList.Synchronized(new List<Connection>());

        public IEnumerable<IConnection> ActiveConnections
        {
            get
            {
                return _activeConnections.Cast<IConnection>();
            }
        }

        private Task _coreTask;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private Task _currentAcceptTask;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                disposedValue = true;
            }
        }

        // ~Server() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}