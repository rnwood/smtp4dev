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
            Extensions = new List<Extension>();
            AddStandardExtns();
        }

        public ServerBehaviour Behaviour { get; private set; }

        private void AddStandardExtns()
        {
            Extensions.Add(new EightBitMimeExtension());
            Extensions.Add(new StartTlsExtension());
            Extensions.Add(new SizeExtension());
            Extensions.Add(new AuthExtension());
        }
        

        public void Run()
        {
            foreach (Extension extension in Extensions)
            {
                extension.ServerStartup(this);
            }

            TcpListener l = new TcpListener(Behaviour.IpAddress, Behaviour.PortNumber);
            l.Start();

            while (!_stop)
            {
                TcpClient tcpClient = l.AcceptTcpClient();
                new Thread(ConnectionThreadWork).Start(tcpClient);
            }
        }

        private volatile bool _stop;

        public void Stop()
        {
            _stop = true;
        }

        public List<Extension> Extensions
        {
            get;
            private set;
        }

        public long? MaxMessageSize { get; set; }

        private void ConnectionThreadWork(object tcpClient)
        {
            ConnectionProcessor connectionProcessor = new ConnectionProcessor(this, (TcpClient)tcpClient);
            connectionProcessor.Start();
        }
    }
}