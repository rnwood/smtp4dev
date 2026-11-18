using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class KonzultaEndpointTests
    {
        [Fact]
        public async Task SpecRequest_Returns200XmlWithSameSmsIdAndStatus0_AndStoresSms()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostKonzulta(TestData.KonzultaSpecRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.ToString().Should().Be("text/xml; charset=utf-8");
            var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
            var item = xml.Descendants("DataItem").Single();
            item.Element("SmsId").Value.Should().Be("987654");
            item.Element("Status").Value.Should().Be("0");
            item.Element("MsgId").Value.Should().StartWith("fake-");

            var sms = host.SmsStore.Query().Single();
            sms.To.Should().Be("+420700000001");
            sms.Sender.Should().Be("SENDER");
            sms.Text.Should().Contain("overovaci kod: 123456.");
            sms.ContentType.Should().Be("Unicode");
            sms.SendTime.Should().Be("20261117101530");
            sms.ExternalSmsId.Should().Be("987654");
            sms.GatewayUser.Should().Be("api-user");
            sms.Status.Should().Be("ACCEPTED");
            sms.MsgId.Should().Be(item.Element("MsgId").Value);
            sms.Encoding.Should().Be("UCS2");
            sms.Segments.Should().Be(2);
            sms.ReceivedAt.Kind.Should().Be(DateTimeKind.Utc);
            (sms.ReceivedAt.Ticks % TimeSpan.TicksPerMillisecond).Should().Be(0);
        }

        [Fact]
        public async Task CatchAllRoute_AcceptsOtherPaths()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostKonzulta(TestData.KonzultaRequest(TestData.KonzultaItem("+420700000001", "x", "1")), "/konzulta/some/other/path");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            host.SmsStore.Query().Should().HaveCount(1);
        }

        [Fact]
        public async Task ThreeItems_AreAllStoredAndAnswered()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostKonzulta(TestData.KonzultaRequest(
                TestData.KonzultaItem("+420700000001", "a", "1"),
                TestData.KonzultaItem("+420700000002", "b", "2"),
                TestData.KonzultaItem("+420700000003", "c", "3")));

            var items = XDocument.Parse(await response.Content.ReadAsStringAsync()).Descendants("DataItem").ToList();
            items.Select(i => i.Element("SmsId").Value).Should().Equal("1", "2", "3");
            items.Should().OnlyContain(i => i.Element("Status").Value == "0");
            host.SmsStore.Query().Should().HaveCount(3);
        }

        [Fact]
        public async Task ItemWithoutSmsId_IsRefusedOthersAccepted()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostKonzulta(TestData.KonzultaRequest(
                "<DataItem><MobileTerminate>+420700000001</MobileTerminate><Text>a</Text></DataItem>",
                TestData.KonzultaItem("+420700000002", "b", "2")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = XDocument.Parse(await response.Content.ReadAsStringAsync()).Descendants("DataItem").ToList();
            items.Should().HaveCount(2);
            items[0].Element("Status").Value.Should().Be("1");
            items[1].Element("SmsId").Value.Should().Be("2");
            items[1].Element("Status").Value.Should().Be("0");
        }

        [Fact]
        public async Task RefuseNumbers_AreAnsweredWithStatus1AndStoredAsRefused()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync(o =>
            {
                o.Settings["FakeGateways:Konzulta:RefuseNumbers:0"] = "+420700000099";
                o.Settings["FakeGateways:Konzulta:RefuseNumbers:1"] = "+42070001*";
            });

            var response = await host.Client.PostKonzulta(TestData.KonzultaRequest(
                TestData.KonzultaItem("+420700000099", "a", "1"),
                TestData.KonzultaItem("00420700010055", "b", "2"),
                TestData.KonzultaItem("+420700000001", "c", "3")));

            var items = XDocument.Parse(await response.Content.ReadAsStringAsync()).Descendants("DataItem").ToList();
            items.Select(i => i.Element("Status").Value).Should().Equal("1", "1", "0");
            host.SmsStore.Query().Select(s => s.Status).Should().Equal("REFUSED", "REFUSED", "ACCEPTED");
        }

        [Theory]
        [InlineData("<SmsServices><DataHeader>")]
        [InlineData("")]
        [InlineData("not xml")]
        public async Task InvalidXml_Returns400(string body)
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostKonzulta(body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().NotBeEmpty();
            host.SmsStore.Query().Should().BeEmpty();
        }

        [Fact]
        public async Task InvalidXml_ErrorDoesNotLeakRequestContent()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            string body = "<SmsServices><DataHeader><UserName>u</UserName><Password>SECRET-PW<ss</Password></DataHeader></SmsServices>";

            var response = await host.Client.PostKonzulta(body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            string text = await response.Content.ReadAsStringAsync();
            text.Should().MatchRegex(@"^Invalid XML at line \d+, position \d+$");
            text.Should().NotContain("SECRET-PW");
            host.Logs.Entries.Should().NotBeEmpty();
            host.Logs.Entries.Should().NotContain(e => e.Contains("SECRET-PW"));
            host.Logs.Entries.Should().Contain(e => e.Contains("KonzultaController") && e.Contains("rejected request: Invalid XML at line"));
            host.Logs.Entries.Where(e => e.Contains("KonzultaController")).Should().NotContain(e => e.Contains("<ss") || e.Contains("'ss'"));
        }

        [Fact]
        public async Task Request_NotifiesStoreListenersOnlyOnce()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            int changes = 0;
            host.SmsStore.Changed += () => changes++;

            await host.Client.PostKonzulta(TestData.KonzultaRequest(
                TestData.KonzultaItem("+420700000001", "a", "1"),
                TestData.KonzultaItem("+420700000002", "b", "2"),
                TestData.KonzultaItem("+420700000003", "c", "3")));

            changes.Should().Be(1);
            host.SmsStore.Query().Should().HaveCount(3);
        }

        [Fact]
        public async Task Emoji_And_Diacritics_AreStoredIntact()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            await host.Client.PostKonzulta(TestData.KonzultaRequest(TestData.KonzultaItem("+420700000001", "Ahoj &amp; příliš \U0001F600", "1")));

            host.SmsStore.Query().Single().Text.Should().Be("Ahoj & příliš \U0001F600");
        }

        [Fact]
        public async Task Password_IsNeverStoredReturnedOrLogged()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostKonzulta(TestData.KonzultaSpecRequest);
            var api = await host.Client.GetStringAsync("/api/fakegateways/sms");

            (await response.Content.ReadAsStringAsync()).Should().NotContain(TestData.Password);
            api.Should().NotContain(TestData.Password);
            System.Text.Json.JsonSerializer.Serialize(host.SmsStore.Query()).Should().NotContain(TestData.Password);
            host.Logs.Entries.Should().NotBeEmpty();
            host.Logs.Entries.Should().NotContain(e => e.Contains(TestData.Password));
        }

        [Fact]
        public async Task SmsTextIsNotLogged()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            await host.Client.PostKonzulta(TestData.KonzultaSpecRequest);

            host.Logs.Entries.Should().Contain(e => e.Contains("Information") && e.Contains("+420700000001") && e.Contains("987654"));
            host.Logs.Entries.Should().NotContain(e => e.Contains("overovaci kod"));
        }

        [Fact]
        public async Task GetOnKonzultaEndpoint_IsNotAllowed()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync("/konzulta/webservices/webservice.aspx");

            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }
}
