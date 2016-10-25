#region

using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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
            return new DefaultServer(false, Ports.AssignAutomatically);
        }

        [Fact]
        public void Start_IsRunning()
        {
            using (Server server = StartServer())
            {
                Assert.True(server.IsRunning);
            }
        }

        [Fact]
        public void StartOnInusePort_StartupExceptionThrown()
        {
            using (Server server1 = new DefaultServer(false, Ports.AssignAutomatically))
            {
                server1.Start();

                using (Server server2 = new DefaultServer(false, server1.PortNumber))
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
            using (Server server = StartServer())
            {
                server.Stop();
                Assert.False(server.IsRunning);
            }
        }

        [Fact]
        public async Task Stop_CannotConnect()
        {
            using (Server server = StartServer())
            {
                int portNumber = server.PortNumber;
                server.Stop();

                TcpClient client = new TcpClient();
                await Assert.ThrowsAnyAsync<SocketException>(async () =>
                    await client.ConnectAsync("localhost", portNumber)
                );
            }
        }

        [Fact]
        public async Task Stop_KillConnectionsTrue_ConnectionsKilled()
        {
            {
                Server server = StartServer();

                Task serverTask = Task.Run(async () =>
                {
                    await Task.Run(() => server.WaitForNextConnection()).WithTimeout("waiting for next server connection");
                    Assert.Equal(1, server.ActiveConnections.Count());
                    await Task.Run(() => server.Stop(true)).WithTimeout("stopping server");
                    Assert.Equal(0, server.ActiveConnections.Count());
                });

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync("localhost", server.PortNumber).WithTimeout("waiting for client to connect");
                    await serverTask.WithTimeout(30, "waiting for server task to complete");
                }
            }
        }

        [Fact]
        public async Task Stop_KillConnectionFalse_ConnectionsNotKilled()
        {
            Server server = StartServer();

            Task serverTask = Task.Run(async () =>
            {
                await Task.Run(() => server.WaitForNextConnection()).WithTimeout("waiting for next server connection");
                Assert.Equal(1, server.ActiveConnections.Count());

                await Task.Run(() => server.Stop(false)).WithTimeout("stopping server");
                ;
                Assert.Equal(1, server.ActiveConnections.Count());
                await Task.Run(() => server.KillConnections()).WithTimeout("killing connections");
                Assert.Equal(0, server.ActiveConnections.Count());
            });

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync("localhost", server.PortNumber).WithTimeout("waiting for client to connect");
                await serverTask.WithTimeout(30, "waiting for server task to complete");
            }
        }

        [Fact]
        public async void Start_CanConnect()
        {
            using (Server server = StartServer())
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync("localhost", server.PortNumber);
                }

                server.Stop();
            }
        }
    }
}