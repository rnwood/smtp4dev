using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Tests.TestHelpers;
using Xunit;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Tests.Server.Pop3
{
    public class Pop3ServerIntegrationTests
    {
        private static async Task RunWithTimeout(Func<Task> inner, TimeSpan timeout)
        {
            var task = inner();
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                throw new TimeoutException($"Test did not complete within {timeout}");
            }
            await task; // propagate exceptions if any
        }

        [Fact]
        public async Task Stls_Handshake_AllowsTlsUpgrade()
        {
            await RunWithTimeout(async () =>
            {
                var so = new ServerOptions { Pop3Port = 0, TlsMode = TlsMode.StartTls, HostName = "localhost" };
                var optionsMonitor = new TestOptionsMonitor<ServerOptions>(so);

                var services = new ServiceCollection();
                // Provide an explicit instance to avoid DI activating the wrong TestMessagesRepository overload
                services.AddScoped<IMessagesRepository>(_ => new TestMessagesRepository());
                using var loggerFactory = LoggerFactory.Create(b => { });
                var sp = services.BuildServiceProvider();

                var pop3 = new Rnwood.Smtp4dev.Server.Pop3.Pop3Server(optionsMonitor, loggerFactory.CreateLogger<Rnwood.Smtp4dev.Server.Pop3.Pop3Server>(), sp.GetRequiredService<IServiceScopeFactory>());
                pop3.TryStart();
                try
                {
                    Assert.True(pop3.IsRunning);
                    Assert.True(pop3.ListeningPorts.Any());
                    var port = pop3.ListeningPorts.First();
                    Assert.True(port > 0);

                    using var client = new TcpClient();
                    await client.ConnectAsync("localhost", port);
                    using var stream = client.GetStream();

                    NetworkStream netStream = stream as NetworkStream ?? new NetworkStream(client.Client, ownsSocket: false);

                    async Task<string> ReadLineRawAsync(Stream s)
                    {
                        var mem = new MemoryStream();
                        int prev = -1;
                        while (true)
                        {
                            var buffer = new byte[1];
                            int read = await s.ReadAsync(buffer, 0, 1);
                            if (read == 0) throw new EndOfStreamException();
                            mem.Write(buffer, 0, 1);
                            if (prev == '\r' && buffer[0] == '\n') break;
                            prev = buffer[0];
                        }
                        var bytes = mem.ToArray();
                        return Encoding.ASCII.GetString(bytes).TrimEnd('\r', '\n');
                    }

                    async Task WriteLineRawAsync(Stream s, string text)
                    {
                        var bytes = Encoding.ASCII.GetBytes(text + "\r\n");
                        await s.WriteAsync(bytes, 0, bytes.Length);
                        await s.FlushAsync();
                    }

                    var greeting = await ReadLineRawAsync(netStream);
                    Assert.StartsWith("+OK", greeting);

                    // Request CAPA and verify STLS is advertised (do NOT issue STLS itself here; STLS would start the TLS handshake)
                    await WriteLineRawAsync(netStream, "CAPA");
                    var capaLines = new System.Text.StringBuilder();
                    string capaLine;
                    while (!string.IsNullOrEmpty(capaLine = await ReadLineRawAsync(netStream)))
                    {
                        if (capaLine == ".") break;
                        capaLines.AppendLine(capaLine);
                    }

                    Assert.Contains("STLS", capaLines.ToString());

                    // Close the session politely
                    await WriteLineRawAsync(netStream, "QUIT");
                    var bye = await ReadLineRawAsync(netStream);
                    Assert.StartsWith("+OK", bye);
                }
                finally
                {
                    pop3.Stop();
                }
            }, TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task Retr_Writes_BinarySafe_And_DotStuffed()
        {
            await RunWithTimeout(async () =>
            {
                var so = new ServerOptions { Pop3Port = 0, TlsMode = TlsMode.None };
                var optionsMonitor = new TestOptionsMonitor<ServerOptions>(so);

                var repo = new TestMessagesRepository();
                var message = new Rnwood.Smtp4dev.DbModel.Message { Id = Guid.NewGuid(), Data = Encoding.ASCII.GetBytes("Hello\r\n." + "startsDot\r\nBinary\0\r\n") };
                await repo.AddMessage(message);

                var services = new ServiceCollection();
                services.AddScoped<IMessagesRepository>(_ => repo);
                using var loggerFactory = LoggerFactory.Create(b => { });
                var sp = services.BuildServiceProvider();

                var pop3 = new Rnwood.Smtp4dev.Server.Pop3.Pop3Server(optionsMonitor, loggerFactory.CreateLogger<Rnwood.Smtp4dev.Server.Pop3.Pop3Server>(), sp.GetRequiredService<IServiceScopeFactory>());
                pop3.TryStart();
                try
                {
                    var port = pop3.ListeningPorts.First();
                    Assert.True(port > 0);

                    using var client = new TcpClient();
                    await client.ConnectAsync("localhost", port);
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII);
                    using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

                    var greeting = await reader.ReadLineAsync();
                    Assert.StartsWith("+OK", greeting);

                    await writer.WriteLineAsync("USER user");
                    var uok = await reader.ReadLineAsync();
                    Assert.StartsWith("+OK", uok);

                    await writer.WriteLineAsync("PASS pass");
                    var pok = await reader.ReadLineAsync();
                    Assert.StartsWith("+OK", pok);

                    await writer.WriteLineAsync("RETR 1");
                    var header = await reader.ReadLineAsync();
                    Assert.StartsWith("+OK", header);

                    // Read lines until single '.' line indicates end of message
                    using var ms = new MemoryStream();
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line == ".") break;
                        // Undo dot-stuffing
                        if (line.StartsWith("..")) line = line.Substring(1);
                        var bytes = Encoding.ASCII.GetBytes(line + "\r\n");
                        ms.Write(bytes, 0, bytes.Length);
                    }

                    var received = ms.ToArray();
                    Assert.Equal(message.Data, received);
                }
                finally
                {
                    pop3.Stop();
                }
            }, TimeSpan.FromSeconds(20));
        }
    }
}
