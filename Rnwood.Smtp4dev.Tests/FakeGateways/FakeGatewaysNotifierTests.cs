using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Rnwood.Smtp4dev.FakeGateways;
using Rnwood.Smtp4dev.Hubs;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class FakeGatewaysNotifierTests
    {
        private static (FakeGatewaysNotifier Notifier, IClientProxy Proxy, SmsStore Sms, TargitoStore Targito) Create()
        {
            var proxy = Substitute.For<IClientProxy>();
            var clients = Substitute.For<IHubClients>();
            clients.All.Returns(proxy);
            var hub = Substitute.For<IHubContext<NotificationsHub>>();
            hub.Clients.Returns(clients);

            var sms = new SmsStore(10);
            var targito = new TargitoStore(10);
            return (new FakeGatewaysNotifier(hub, sms, targito, NullLogger<FakeGatewaysNotifier>.Instance), proxy, sms, targito);
        }

        [Fact]
        public async Task SendsSmsChangedAndTargitoChangedEvents()
        {
            var (notifier, proxy, sms, targito) = Create();
            await notifier.StartAsync(CancellationToken.None);

            sms.Add(TestData.Sms());
            targito.Add(TestData.Targito());

            await proxy.Received(1).SendCoreAsync("smschanged", Arg.Any<object[]>(), Arg.Any<CancellationToken>());
            await proxy.Received(1).SendCoreAsync("targitochanged", Arg.Any<object[]>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SendsEventOnDelete()
        {
            var (notifier, proxy, sms, _) = Create();
            sms.Add(TestData.Sms(id: "1"));
            await notifier.StartAsync(CancellationToken.None);

            sms.Remove("1");

            await proxy.Received(1).SendCoreAsync("smschanged", Arg.Any<object[]>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NoEventsAfterStop()
        {
            var (notifier, proxy, sms, _) = Create();
            await notifier.StartAsync(CancellationToken.None);
            await notifier.StopAsync(CancellationToken.None);

            sms.Add(TestData.Sms());

            await proxy.DidNotReceiveWithAnyArgs().SendCoreAsync(default, default, default);
        }

        [Fact]
        public async Task HubFailureDoesNotBreakStore()
        {
            var (notifier, proxy, sms, _) = Create();
            proxy.SendCoreAsync(default, default, default).ReturnsForAnyArgs(Task.FromException(new System.InvalidOperationException()));
            await notifier.StartAsync(CancellationToken.None);

            sms.Add(TestData.Sms());

            sms.Query().Should().HaveCount(1);
        }
    }
}
