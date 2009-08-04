#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Extensions.Auth;

#endregion

namespace Rnwood.SmtpServer
{
    public class Server
    {
        public Server(ServerBehaviour behaviour)
        {
            Behaviour = behaviour;
        }

        public ServerBehaviour Behaviour { get; private set; }

        public bool IsRunning
        {
            get;
            private set;
        }

        public void Run()
        {
            if (IsRunning)
                throw new InvalidOperationException("Already running");

            IsRunning = true;
            _listener = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            _listener.Start();

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

        private TcpListener _listener;

        public void Stop()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Not running");
            }

            IsRunning = false;
            _listener.Stop();
        }

        private void ConnectionThreadWork(object tcpClient)
        {
            ConnectionProcessor connectionProcessor = new ConnectionProcessor(this, (TcpClient)tcpClient);
            connectionProcessor.Start();
        }
    }
}