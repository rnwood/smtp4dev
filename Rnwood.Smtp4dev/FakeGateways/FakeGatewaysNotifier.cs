#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.Hubs;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>
    /// Pushes "smschanged" / "targitochanged" events to all clients of the existing notifications hub
    /// whenever the corresponding store changes.
    /// </summary>
    public class FakeGatewaysNotifier : IHostedService
    {
        public const string SmsChangedEvent = "smschanged";
        public const string TargitoChangedEvent = "targitochanged";

        private readonly IHubContext<NotificationsHub> hubContext;
        private readonly SmsStore smsStore;
        private readonly TargitoStore targitoStore;
        private readonly ILogger<FakeGatewaysNotifier> logger;
        private Action? smsHandler;
        private Action? targitoHandler;

        public FakeGatewaysNotifier(IHubContext<NotificationsHub> hubContext, SmsStore smsStore, TargitoStore targitoStore, ILogger<FakeGatewaysNotifier> logger)
        {
            this.hubContext = hubContext;
            this.smsStore = smsStore;
            this.targitoStore = targitoStore;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            smsHandler = () => Send(SmsChangedEvent);
            targitoHandler = () => Send(TargitoChangedEvent);
            smsStore.Changed += smsHandler;
            targitoStore.Changed += targitoHandler;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (smsHandler != null)
            {
                smsStore.Changed -= smsHandler;
                smsHandler = null;
            }
            if (targitoHandler != null)
            {
                targitoStore.Changed -= targitoHandler;
                targitoHandler = null;
            }
            return Task.CompletedTask;
        }

        private void Send(string eventName)
        {
            _ = SendAsync(eventName);
        }

        private async Task SendAsync(string eventName)
        {
            try
            {
                await hubContext.Clients.All.SendAsync(eventName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send {Event} notification", eventName);
            }
        }
    }
}
