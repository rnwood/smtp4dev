#region

using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
            using (Server server1 = new DefaultServer(Ports.AssignAutomatically))
            {
                server1.Start();

                using (Server server2 = new DefaultServer(server1.PortNumber))
                {
                    Assert.Throws<SocketException>(() =>
                    {
                        server2.Start();
                    });
                }
            }
        }

        [Fact]
        public void Stop_NotRunning()
        {
            Server server = StartServer();
            server.Stop();
            Assert.False(server.IsRunning);
        }

        [Fact]
        public async Task Stop_CannotConnect()
        {
            Server server = StartServer();
            int portNumber = server.PortNumber;
            server.Stop();

            TcpClient client = new TcpClient();
            await Assert.ThrowsAnyAsync<SocketException>(async () =>
                await client.ConnectAsync("localhost", portNumber)
            );
        }

        [Fact]
        public async Task Stop_KillConnectionTrue_ConnectionsKilled()
        {
            Server server = StartServer();

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync("localhost", server.PortNumber);
            }

            server.Stop(true);

            Assert.Equal(0, server.ActiveConnections.Count());
        }

        [Fact]
        public async void Start_CanConnect()
        {
            Server server = StartServer();

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync("localhost", server.PortNumber);
            }

            server.Stop();
        }
    }
}