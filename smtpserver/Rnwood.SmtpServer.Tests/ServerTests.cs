// <copyright file="ServerTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="ServerTests" />
    /// </summary>
    public class ServerTests
    {
        /// <summary>
        /// The Start_CanConnect
        /// </summary>
        [Fact]
        public async void Start_CanConnect()
        {
            using (SmtpServer server = this.StartServer())
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync("localhost", server.PortNumber).ConfigureAwait(false);
                }

                server.Stop();
            }
        }

        /// <summary>
        /// The Start_IsRunning
        /// </summary>
        [Fact]
        public void Start_IsRunning()
        {
            using (SmtpServer server = this.StartServer())
            {
                Assert.True(server.IsRunning);
            }
        }

        /// <summary>  Tests that the port number is returned via the PortNumber property when AssignAutomatically is used.</summary>
        [Fact]
        public void StartOnAutomaticPort_PortNumberReturned()
        {
            SmtpServer server = new DefaultServer(false, StandardSmtpPort.AssignAutomatically);
            server.Start();
            Assert.NotEqual(0, server.PortNumber);
        }

        /// <summary>
        /// The StartOnInusePort_StartupExceptionThrown
        /// </summary>
        [SkippableFact]
        public void StartOnInusePort_StartupExceptionThrown()
        {
            //Exclusive port use is only available on Windows.
            //On other platforms the listener with the most specific address will receive
            //all the connections.
            Skip.IfNot(Environment.OSVersion.Platform == PlatformID.Win32NT);

            using (SmtpServer server1 = new DefaultServer(false, StandardSmtpPort.AssignAutomatically))
            {
                server1.Start();

                using (SmtpServer server2 = new DefaultServer(false, server1.PortNumber))
                {
                    Assert.Throws<SocketException>(() =>
                    {
                        server2.Start();
                    });
                }
            }
        }

        /// <summary>
        /// The Stop_CannotConnect
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Stop_CannotConnect()
        {
            using (SmtpServer server = this.StartServer())
            {
                int portNumber = server.PortNumber;
                server.Stop();

                TcpClient client = new TcpClient();
                await Assert.ThrowsAnyAsync<SocketException>(async () =>
                    await client.ConnectAsync("localhost", portNumber).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The Stop_KillConnectionFalse_ConnectionsNotKilled
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Stop_KillConnectionFalse_ConnectionsNotKilled()
        {
            SmtpServer server = this.StartServer();

            Task serverTask = Task.Run(async () =>
            {
                await Task.Run(() => server.WaitForNextConnection()).WithTimeout("waiting for next server connection").ConfigureAwait(false);
                Assert.Single(server.ActiveConnections);

                await Task.Run(() => server.Stop(false)).WithTimeout("stopping server").ConfigureAwait(false);
                ;
                Assert.Single(server.ActiveConnections);
                await Task.Run(() => server.KillConnections()).WithTimeout("killing connections").ConfigureAwait(false);
                Assert.Empty(server.ActiveConnections);
            });

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync("localhost", server.PortNumber).WithTimeout("waiting for client to connect").ConfigureAwait(false);
                await serverTask.WithTimeout(30, "waiting for server task to complete").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The Stop_KillConnectionsTrue_ConnectionsKilled
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task Stop_KillConnectionsTrue_ConnectionsKilled()
        {
            {
                SmtpServer server = this.StartServer();

                Task serverTask = Task.Run(async () =>
                {
                    await Task.Run(() => server.WaitForNextConnection()).WithTimeout("waiting for next server connection").ConfigureAwait(false);
                    Assert.Single(server.ActiveConnections);
                    await Task.Run(() => server.Stop(true)).WithTimeout("stopping server").ConfigureAwait(false);
                    Assert.Empty(server.ActiveConnections);
                });

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync("localhost", server.PortNumber).WithTimeout("waiting for client to connect").ConfigureAwait(false);
                    await serverTask.WithTimeout(30, "waiting for server task to complete").ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// The Stop_NotRunning
        /// </summary>
        [Fact]
        public void Stop_NotRunning()
        {
            using (SmtpServer server = this.StartServer())
            {
                server.Stop();
                Assert.False(server.IsRunning);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>The <see cref="SmtpServer"/></returns>
        private SmtpServer NewServer()
        {
            return new DefaultServer(false, StandardSmtpPort.AssignAutomatically);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>The <see cref="SmtpServer"/></returns>
        private SmtpServer StartServer()
        {
            SmtpServer server = this.NewServer();
            server.Start();
            return server;
        }
    }
}
