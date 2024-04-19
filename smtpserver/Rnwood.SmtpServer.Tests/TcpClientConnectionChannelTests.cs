// <copyright file="TcpClientConnectionChannelTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="TcpClientConnectionChannelTests" />
/// </summary>
public class TcpClientConnectionChannelTests
{
    /// <summary>
    ///     The ReadLineAsync_ThrowsOnConnectionClose
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ReadLineAsync_ThrowsOnConnectionClose()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);

        try
        {
            listener.Start();
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

            TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port)
                ;

            using (TcpClient serverTcpClient = await acceptTask)
            {
                TcpClientConnectionChannel channel = new TcpClientConnectionChannel(serverTcpClient, Encoding.Default);
                client.Dispose();

                await Assert.ThrowsAsync<ConnectionUnexpectedlyClosedException>(async () =>
                {
                    await channel.ReadLine();
                });
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}
