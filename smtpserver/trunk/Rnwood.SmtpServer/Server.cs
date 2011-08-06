#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

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

            _listener = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            _listener.Start();

            IsRunning = true;

            Core();
        }

        public int PortNumber
        {
            get
            {
                return ((IPEndPoint)_listener.LocalEndpoint).Port;
            }
        }


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
                        Connection connection = new Connection(this, (TcpClient)tcpClient);
                        _activeConnections.Add(connection);
                        thread.Start(connection);
                    }
                }
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
        /// <exception cref="System.InvalidOperationException">if the server is already running.</exception>
        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Already running");

            _listener = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            _listener.Start();

            IsRunning = true;

            new Thread(Core).Start();
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
                throw new InvalidOperationException("Not running");
            }

            IsRunning = false;
            _listener.Stop();
            _coreThread.Join();

            if (killConnections)
            {
                foreach (Connection connection in _activeConnections.Cast<Connection>().ToArray())
                {
                    connection.CloseConnection();
                    connection.Thread.Join();
                }
            }
        }

        private readonly IList _activeConnections = ArrayList.Synchronized(new List<Connection>());

        private void ConnectionThreadWork(object connectionObj)
        {
            Connection connection = (Connection) connectionObj;
            connection.Start();
            _activeConnections.Remove(connection);
        }
    }
}