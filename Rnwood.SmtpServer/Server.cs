#region

using System;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace Rnwood.SmtpServer
{
    public class Server
    {
        private Thread _coreThread;
        private TcpListener _listener;

        public Server() : this(new DefaultServerBehaviour())
        {
        }

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

        private void Core()
        {
            _coreThread = Thread.CurrentThread;

            try
            {
                while (true)
                {
                    TcpClient tcpClient = _listener.AcceptTcpClient();
                    new Thread(ConnectionThreadWork).Start(tcpClient);
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
        /// Starts this server on .
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Already running");

            _listener = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            _listener.Start();

            IsRunning = true;

            new Thread(Core).Start();
        }


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

        private void ConnectionThreadWork(object tcpClient)
        {
            ConnectionProcessor connectionProcessor = new ConnectionProcessor(this, (TcpClient) tcpClient);
            connectionProcessor.Start();
        }
    }
}