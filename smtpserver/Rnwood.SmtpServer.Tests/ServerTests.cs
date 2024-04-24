// <copyright file="ServerTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ServerTests" />
/// </summary>
public class ServerTests
{
    /// <summary>
    ///     Tests that when running, can connect in all the various combinations of allow remote and IPV6 vs IPV4
    /// </summary>
    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, true, true)]

    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, true, true)]
    public async Task Start_CanConnect(bool allowRemoteConnections, bool testRemoteConnection, bool enableIpV6, bool testIpV6)
    {

        using (SmtpServer server = StartServer(allowRemoteConnections, enableIpV6))
        {
            IPAddress ipAddress;
            if (allowRemoteConnections)
            {
                ipAddress = (enableIpV6 ? IPAddress.IPv6Any : IPAddress.Any);
            }
            else
            {

                ipAddress = (testIpV6 ? IPAddress.IPv6Loopback : IPAddress.Loopback);
            }

            int port = server.ListeningEndpoints.Single(p => p.Address.ToString() == ipAddress.ToString()).Port;
            using (TcpClient client = new TcpClient(testIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork))
            { 
                await client.ConnectAsync(testRemoteConnection ? Dns.GetHostName() : "localhost", port);
                Assert.True(client.Connected);
            }

            server.Stop();
        }

    }

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, true, true)]
    public async Task Start_CanNotConnect(bool allowRemoteConnections, bool testRemoteConnection, bool enableIpV6, bool testIpV6)
    {

        using (SmtpServer server = StartServer(allowRemoteConnections, enableIpV6))
        {
            IPAddress ipAddress;
            if (allowRemoteConnections)
            {
                ipAddress = (enableIpV6 ? IPAddress.IPv6Any : IPAddress.Any);
            }
            else
            {

                ipAddress = (testIpV6 ? IPAddress.IPv6Loopback : IPAddress.Loopback);
            }

            int port = server.ListeningEndpoints.Single(p => p.Address.ToString() == ipAddress.ToString()).Port;
            using (TcpClient client = new TcpClient(testIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork))
            {
                await Assert.ThrowsAsync<SocketException>(async () => { await client.ConnectAsync(testRemoteConnection ? Dns.GetHostName() : "localhost", port); });   
            }

            server.Stop();
        }

    }



    /// <summary>
    ///     The Start_IsRunning
    /// </summary>
    [Fact]
    public void Start_IsRunning()
    {
        using (SmtpServer server = StartServer())
        {
            Assert.True(server.IsRunning);
        }
    }

    /// <summary>  Tests that the port number is returned via the PortNumber property when AssignAutomatically is used.</summary>
    [Fact]
    public void StartOnAutomaticPort_PortNumberReturned()
    {
        SmtpServer server = new SmtpServer(new ServerOptions(false, false, "test", (int)StandardSmtpPort.AssignAutomatically, true, [], [], null, null));
        server.Start();
        Assert.NotEqual(0, server.ListeningEndpoints.First().Port);
    }

    /// <summary>
    ///     The StartOnInusePort_StartupExceptionThrown
    /// </summary>
    [SkippableFact]
    public void StartOnInusePort_StartupExceptionThrown()
    {
        //Exclusive port use is only available on Windows.
        //On other platforms the listener with the most specific address will receive
        //all the connections.
        Skip.IfNot(Environment.OSVersion.Platform == PlatformID.Win32NT);

        using (SmtpServer server1 = new SmtpServer(new Rnwood.SmtpServer.ServerOptions(false, false, "test", (int)StandardSmtpPort.AssignAutomatically, true, [], [], null, null)))
        {
            server1.Start();

            using (SmtpServer server2 = new SmtpServer(new Rnwood.SmtpServer.ServerOptions(false, false, "test", server1.ListeningEndpoints.First().Port, true, [], [], null, null)))
            {
                Assert.Throws<SocketException>(() => { server2.Start(); });
            }
        }
    }

    /// <summary>
    ///     The Stop_CannotConnect
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Stop_CannotConnect()
    {
        using (SmtpServer server = StartServer())
        {
            int portNumber = server.ListeningEndpoints.First().Port;
            server.Stop();

            using TcpClient client = new TcpClient();
            await Assert.ThrowsAnyAsync<SocketException>(async () =>
                await client.ConnectAsync("localhost", portNumber)
            );
        }
    }

    /// <summary>
    ///     The Stop_KillConnectionFalse_ConnectionsNotKilled
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Stop_KillConnectionFalse_ConnectionsNotKilled()
    {
        SmtpServer server = StartServer();

        Task serverTask = Task.Run(async () =>
        {
            await Task.Run(() => server.WaitForNextConnection()).WithTimeout("waiting for next server connection")
                ;
            Assert.Single(server.ActiveConnections);

            await Task.Run(() => server.Stop(false)).WithTimeout("stopping server");
            Assert.Single(server.ActiveConnections);
            await Task.Run(() => server.KillConnections()).WithTimeout("killing connections");
            Assert.Empty(server.ActiveConnections);
        });

        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync("localhost", server.ListeningEndpoints.First().Port).WithTimeout("waiting for client to connect")
                ;
            await serverTask.WithTimeout(30, "waiting for server task to complete");
        }
    }

    /// <summary>
    ///     The Stop_KillConnectionsTrue_ConnectionsKilled
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Stop_KillConnectionsTrue_ConnectionsKilled()
    {
        SmtpServer server = StartServer();

        Task serverTask = Task.Run(async () =>
        {
            await Task.Run(() => server.WaitForNextConnection())
                .WithTimeout("waiting for next server connection");
            Assert.Single(server.ActiveConnections);
            await Task.Run(() => server.Stop(true)).WithTimeout("stopping server");
            Assert.Empty(server.ActiveConnections);
        });

        using TcpClient client = new TcpClient();
        await client.ConnectAsync("localhost", server.ListeningEndpoints.First().Port)
            .WithTimeout("waiting for client to connect");
        await serverTask.WithTimeout(30, "waiting for server task to complete");
    }

    /// <summary>
    ///     The Stop_NotRunning
    /// </summary>
    [Fact]
    public void Stop_NotRunning()
    {
        using (SmtpServer server = StartServer())
        {
            server.Stop();
            Assert.False(server.IsRunning);
        }
    }

    /// <summary>
    /// </summary>
    /// <returns>The <see cref="SmtpServer" /></returns>
    private SmtpServer NewServer(bool allowRemoteConnections, bool allowIpV6) => new SmtpServer(new Rnwood.SmtpServer.ServerOptions(allowRemoteConnections, allowIpV6, "test", (int)StandardSmtpPort.AssignAutomatically, false, [], [], null, null));

    /// <summary>
    /// </summary>
    /// <returns>The <see cref="SmtpServer" /></returns>
    private SmtpServer StartServer(bool allowRemoteConnections = false, bool allowIpV6 = true)
    {
        SmtpServer server = NewServer(allowRemoteConnections, allowIpV6);
        server.Start();
        return server;
    }
}
