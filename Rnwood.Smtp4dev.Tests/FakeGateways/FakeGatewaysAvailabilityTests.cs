using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class FakeGatewaysDisabledTests
    {
        [Fact]
        public async Task Disabled_FakeEndpointsAndApiReturn404_HealthReportsDisabled()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync(o => o.Enabled = false);

            (await host.Client.PostKonzulta(TestData.KonzultaSpecRequest)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.PostKonzulta(TestData.KonzultaSpecRequest, "/konzulta/anything")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.PostTargito(TestData.TargitoSend())).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.GetAsync("/api/fakegateways/sms")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.GetAsync("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=1")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.GetAsync("/api/fakegateways/sms/abc")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.DeleteAsync("/api/fakegateways/sms")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.GetAsync("/api/fakegateways/targito")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.GetAsync("/api/fakegateways/targito/wait?since=2026-11-17T10:00:00Z")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.DeleteAsync("/api/fakegateways/targito/abc")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            host.SmsStore.Query().Should().BeEmpty();
            host.TargitoStore.Query().Should().BeEmpty();

            using var json = await (await host.Client.GetAsync("/api/fakegateways/health")).ReadJson();
            json.RootElement.GetProperty("status").GetString().Should().Be("disabled");
            json.RootElement.GetProperty("features").GetArrayLength().Should().Be(0);
            json.RootElement.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Enabled_HealthReportsFeatures()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync("/api/fakegateways/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("status").GetString().Should().Be("ok");
            json.RootElement.GetProperty("features").EnumerateArray().Select(e => e.GetString()).Should().Equal("konzulta", "targito");
            json.RootElement.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        }
    }

    public class FakeGatewaysAuthenticationTests
    {
        [Fact]
        public async Task FakeEndpointsAndHealthAreAnonymous_ApiRequiresAuthentication()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync(o => o.RequireAuthentication = true);

            (await host.Client.PostKonzulta(TestData.KonzultaSpecRequest)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await host.Client.PostTargito(TestData.TargitoSend())).StatusCode.Should().Be(HttpStatusCode.OK);
            (await host.Client.GetAsync("/api/fakegateways/health")).StatusCode.Should().Be(HttpStatusCode.OK);

            (await host.Client.GetAsync("/api/fakegateways/sms")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await host.Client.GetAsync("/api/fakegateways/targito")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await host.Client.DeleteAsync("/api/fakegateways/sms")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            host.SmsStore.Query().Should().HaveCount(1);
            host.TargitoStore.Query().Should().HaveCount(1);
        }
    }
}
