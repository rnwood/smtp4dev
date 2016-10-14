#region

using System.Net.Sockets;
using System.Threading;
using Xunit;

#endregion

namespace Rnwood.SmtpServer.Tests
{
    public class ServerTests
    {
        private Server StartServer()
        {
            Server server = NewServer();
            server.Start();
            return server;
        }

        private Server NewServer()
        {
            return new DefaultServer(Ports.AssignAutomatically);
        }

        [Fact]
        public void Start_IsRunning()
        {
            Server server = StartServer();
            Assert.True(server.IsRunning);
            server.Stop();
        }

        [Fact]
        public void StartOnInusePort_StartupExceptionThrown()
        {
            Assert.Throws<SocketException>(() =>
            {
                Server server1 = new DefaultServer(Ports.AssignAutomatically);
                server1.Start();

                try
                {
                    Server server2 = new DefaultServer(server1.PortNumber);
                    server2.Start();
                }
                finally
                {
                    server1.Stop();
                }
            });
        }

        [Fact]
        public void Stop_NotRunning()
        {
            Server server = StartServer();
            server.Stop();
            Assert.False(server.IsRunning);
        }

        [Fact]
        public async void Start_CanConnect()
        {
            Server server = StartServer();

            TcpClient client = new TcpClient();
            await client.ConnectAsync("localhost", server.PortNumber);
            client.Dispose();

            server.Stop();
        }
    }
}