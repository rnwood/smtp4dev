#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace Rnwood.SmtpServer
{
    public class Server : IServer
    {
        private Thread _coreThread;
        private TcpListener _listener;

        public Server(IServerBehaviour behaviour)
        {
            Behaviour = behaviour;
        }

        public IServerBehaviour Behaviour { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Runs the server synchronously. This method blocks until the server is stopped.
        /// </summary>
        public void Run()
        {
            if (IsRunning)
                throw new InvalidOperationException("Already running");

            StartListener();

            IsRunning = true;

            Core();
        }

        private void StartListener()
        {
            _listener = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            _listener.Start();

            PortNumber = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public int PortNumber
        {
            get;
            private set;
        }

        private readonly System.Threading.AutoResetEvent _connectionCompletedEvent = new AutoResetEvent(false);

        private void Core()
        {
            _coreThread = Thread.CurrentThread;

            try
            {
                while (IsRunning)
                {
                    TcpClient tcpClient = _listener.AcceptTcpClient();

                    if (IsRunning)
                    {
                        Thread thread = new Thread(ConnectionThreadWork);
                        thread.Start(tcpClient);
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                //normal - caused by Stop()
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.Interrupted)
                {
                    //normal - caused by _listener.Stop();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Runs the server asynchronously. This method returns once the server has been started.
        /// To stop the server call the <see cref="Stop()"/> method.
        /// </summary>
        /// <returns>The port number the server is listening on</returns>
        /// <exception cref="System.InvalidOperationException">if the server is already running.</exception>
        public int Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Already running");

            StartListener();

            IsRunning = true;

            new Thread(Core).Start();

            return PortNumber;
        }

        /// <summary>
        /// Stops the running server.
        /// </summary>
        /// <param name="stopBehaviour">Specifies what to do with any currently active connections</param>
        /// <exception cref="System.InvalidOperationException">if the server is not running.</exception>
        public void Stop(ServerStopBehaviour stopBehaviour)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Not running");
            }

            IsRunning = false;
            _listener.Stop();

            _coreThread.Interrupt();
            _coreThread.Join();

            switch (stopBehaviour)
            {
                case ServerStopBehaviour.KillExistingConnections:
                    KillActiveConnections();
                    break;

                case ServerStopBehaviour.WaitForExistingConnections:
                    WaitForActiveConnectionsToComplete();
                    break;
            }
        }

        private void WaitForActiveConnectionsToComplete()
        {
            while (GetFirstActiveConnection() != null)
            {
                _connectionCompletedEvent.WaitOne();
            }
        }

        private Connection GetFirstActiveConnection()
        {
            lock (_activeConnections)
            {
                if (_activeConnections.Count > 0)
                {
                    return _activeConnections[0];
                }
            }

            return null;
        }

        private void KillActiveConnections()
        {
            Connection firstActiveConnection = null;

            while ((firstActiveConnection = GetFirstActiveConnection()) != null)
            {
                firstActiveConnection.Kill();
            }
        }

        private readonly List<Connection> _activeConnections = new List<Connection>();

        private void ConnectionThreadWork(object tcpClient)
        {
            Connection connection = new Connection(this, c => new TcpClientConnectionChannel(c, (TcpClient)tcpClient, Behaviour.GetDefaultEncoding(c)));
            lock (_activeConnections)
            {
                _activeConnections.Add(connection);
            }

            try
            {
                connection.Process();
            }
            finally
            {
                lock (_activeConnections)
                {
                    _activeConnections.Remove(connection);
                }

                _connectionCompletedEvent.Set();
            }
        }

        /// <summary>
        /// Stops the running server and waits for any existing connections to complete.
        /// </summary>
        public void Stop()
        {
            Stop(ServerStopBehaviour.WaitForExistingConnections);
        }
    }
}