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

                    while (_activeThreads.Count > 0)
                    {
                        try
                        {
                            Thread.Sleep(Timeout.Infinite);
                        } catch (ThreadInterruptedException)
                        {
                            //cool
                        }
                    }

                    if (IsRunning)
                    {
                        Thread thread = new Thread(ConnectionThreadWork);
                        _activeThreads.Add(thread);
                        thread.Start(tcpClient);
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
        /// Stops the running server. Any existing connections are continued.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">if the server is not running.</exception>
        public void Stop()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Not running");
            }

            IsRunning = false;
            _listener.Stop();

            _coreThread.Join();
        }

        private IList _activeThreads = ArrayList.Synchronized(new List<Thread>());

        private void ConnectionThreadWork(object tcpClient)
        {
            Connection connection = new Connection(this, (TcpClient) tcpClient);
            connection.Start();
            _activeThreads.Remove(Thread.CurrentThread);
            _coreThread.Interrupt();
        }
    }
}