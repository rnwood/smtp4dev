using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Tests.TestHelpers;
using Xunit;
using Rnwood.Smtp4dev.Data;

namespace Rnwood.Smtp4dev.Tests.Server.Pop3
{
    /// <summary>
    /// Tests for IPv6 fallback behavior for the POP3 server
    /// </summary>
    public class Pop3IPv6FallbackTests
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
        public async Task Pop3Server_StartWithIPv6Any_ShouldFallbackToIPv4WhenIPv6Unavailable()
        {
            await RunWithTimeout(async () =>
            {
                var so = new ServerOptions { Pop3Port = 0, AllowRemoteConnections = true, DisableIPv6 = false };
                var optionsMonitor = new TestOptionsMonitor<ServerOptions>(so);

                var services = new ServiceCollection();
                // Provide an explicit instance to avoid DI activating the wrong TestMessagesRepository overload
                services.AddScoped<IMessagesRepository>(_ => new TestMessagesRepository());
                using var loggerFactory = LoggerFactory.Create(b => { });
                var sp = services.BuildServiceProvider();

                using var pop3 = new Rnwood.Smtp4dev.Server.Pop3.Pop3Server(optionsMonitor, loggerFactory.CreateLogger<Rnwood.Smtp4dev.Server.Pop3.Pop3Server>(), sp.GetRequiredService<IServiceScopeFactory>());

                // This should not throw even if IPv6 is unavailable on the host
                Exception startException = null;
                try
                {
                    pop3.TryStart();
                }
                catch (Exception ex)
                {
                    startException = ex;
                }

                Assert.Null(startException);
                Assert.True(pop3.IsRunning);
                Assert.NotEmpty(pop3.ListeningPorts);

                // Ports should be assigned (non-zero)
                foreach (var port in pop3.ListeningPorts)
                {
                    Assert.True(port > 0);
                }

                pop3.Stop();
                Assert.False(pop3.IsRunning);
            }, TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task Pop3Server_StartWithIPv6Loopback_ShouldFallbackToIPv4WhenIPv6Unavailable()
        {
            await RunWithTimeout(async () =>
            {
                var so = new ServerOptions { Pop3Port = 0, AllowRemoteConnections = false, DisableIPv6 = false };
                var optionsMonitor = new TestOptionsMonitor<ServerOptions>(so);

                var services = new ServiceCollection();
                services.AddScoped<IMessagesRepository>(_ => new TestMessagesRepository());
                using var loggerFactory = LoggerFactory.Create(b => { });
                var sp = services.BuildServiceProvider();

                using var pop3 = new Rnwood.Smtp4dev.Server.Pop3.Pop3Server(optionsMonitor, loggerFactory.CreateLogger<Rnwood.Smtp4dev.Server.Pop3.Pop3Server>(), sp.GetRequiredService<IServiceScopeFactory>());

                Exception startException = null;
                try
                {
                    pop3.TryStart();
                }
                catch (Exception ex)
                {
                    startException = ex;
                }

                Assert.Null(startException);
                Assert.True(pop3.IsRunning);
                Assert.NotEmpty(pop3.ListeningPorts);

                foreach (var port in pop3.ListeningPorts)
                {
                    Assert.True(port > 0);
                }

                pop3.Stop();
                Assert.False(pop3.IsRunning);
            }, TimeSpan.FromSeconds(20));
        }
    }
}
