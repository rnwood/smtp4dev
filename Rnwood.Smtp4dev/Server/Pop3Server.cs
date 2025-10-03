using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.Server
{
    public class Pop3Server : IHostedService, IDisposable
    {
        private readonly IOptionsMonitor<ServerOptions> serverOptions;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private Rnwood.Smtp4dev.Server.Pop3.Pop3Server inner;

        public Pop3Server(IOptionsMonitor<ServerOptions> serverOptions, IServiceScopeFactory serviceScopeFactory)
        {
            this.serverOptions = serverOptions;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        // Compatibility surface
        public bool IsRunning => inner?.IsRunning == true;

        public int[] ListeningPorts => inner?.ListeningPorts ?? Array.Empty<int>();

        public void TryStart()
        {
            EnsureInner();
            inner?.TryStart();
        }

        public void Stop()
        {
            inner?.Stop();
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            EnsureInner();
            return inner?.StartAsync(cancellationToken) ?? Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            return inner?.StopAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public void Dispose()
        {
            try { inner?.Dispose(); } catch { }
        }

        private void EnsureInner()
        {
            if (inner != null) return;
            using var scope = serviceScopeFactory.CreateScope();
            inner = scope.ServiceProvider.GetRequiredService<Rnwood.Smtp4dev.Server.Pop3.Pop3Server>();
        }
    }
}
