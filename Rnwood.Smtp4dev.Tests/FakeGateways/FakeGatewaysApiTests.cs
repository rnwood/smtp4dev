using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class FakeGatewaysApiTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc);

        private static async Task<List<string>> ListIds(FakeGatewaysTestHost host, string url)
        {
            var response = await host.Client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            return json.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList();
        }

        // ---------------------------------------------------------------- JSON shape

        [Fact]
        public async Task SmsJson_HasContractShape()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(receivedAt: new DateTime(2026, 11, 17, 10, 15, 30, 123, DateTimeKind.Utc), id: "s1", text: "kod 123456"));

            using var json = JsonDocument.Parse(await host.Client.GetStringAsync("/api/fakegateways/sms/s1"));
            var root = json.RootElement;

            root.GetProperty("receivedAt").GetString().Should().Be("2026-11-17T10:15:30.123Z");
            root.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(new[]
            {
                "id", "receivedAt", "to", "sender", "text", "contentType", "sendTime", "externalSmsId", "gatewayUser", "status", "msgId", "encoding", "segments", "code"
            });
            root.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("sender").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Fact]
        public async Task SmsReceivedAtIsAlwaysThreeDecimalsAndZ()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(receivedAt: new DateTime(2026, 11, 17, 10, 15, 30, DateTimeKind.Utc), id: "s1"));

            using var json = JsonDocument.Parse(await host.Client.GetStringAsync("/api/fakegateways/sms/s1"));

            json.RootElement.GetProperty("receivedAt").GetString().Should().Be("2026-11-17T10:15:30.000Z");
        }

        [Fact]
        public async Task TargitoJson_HasContractShape_AndPreservesDataKeyCase()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            await host.Client.PostTargito(TestData.TargitoSend());
            string id = host.TargitoStore.Query().Single().Id;

            using var json = JsonDocument.Parse(await host.Client.GetStringAsync("/api/fakegateways/targito/" + id));
            var root = json.RootElement;

            root.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(new[]
            {
                "id", "receivedAt", "method", "email", "oldEmail", "templateId", "templateName", "senderDomain", "sendDateTime", "accountUser", "data", "params", "resultId"
            });
            Regex.IsMatch(root.GetProperty("receivedAt").GetString(), @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$").Should().BeTrue();
            root.GetProperty("data").GetProperty("CONFIRM_URL").GetString().Should().Contain("code=C");
            root.GetProperty("params").GetProperty("templateId").GetString().Should().Be("tpl-1");
            root.GetProperty("oldEmail").ValueKind.Should().Be(JsonValueKind.Null);
        }

        // ---------------------------------------------------------------- SMS list filters

        [Fact]
        public async Task SmsList_NewestFirst_AndFilters()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(to: "+420700000001", text: "Alpha code", receivedAt: T0, id: "1"));
            host.SmsStore.Add(TestData.Sms(to: "+420700000002", text: "Beta", receivedAt: T0.AddMinutes(1), id: "2"));
            host.SmsStore.Add(TestData.Sms(to: "+420700000001", text: "gamma CODE", receivedAt: T0.AddMinutes(2), id: "3"));

            (await ListIds(host, "/api/fakegateways/sms")).Should().Equal("3", "2", "1");
            (await ListIds(host, "/api/fakegateways/sms?to=%2B420700000001")).Should().Equal("3", "1");
            (await ListIds(host, "/api/fakegateways/sms?to=00420700000001")).Should().Equal("3", "1");
            (await ListIds(host, "/api/fakegateways/sms?to=%2B420%20700%20000%20001")).Should().Equal("3", "1");
            (await ListIds(host, "/api/fakegateways/sms?textContains=code")).Should().Equal("3", "1");
            (await ListIds(host, "/api/fakegateways/sms?since=2026-11-17T10:01:00.000Z")).Should().Equal("3", "2");
            (await ListIds(host, "/api/fakegateways/sms?since=2026-11-17T10:01:00.001Z")).Should().Equal("3");
            (await ListIds(host, "/api/fakegateways/sms?to=%2B420700000001&since=2026-11-17T10:01:00Z&textContains=gamma")).Should().Equal("3");
            (await ListIds(host, "/api/fakegateways/sms?to=%2B420999")).Should().BeEmpty();
        }

        [Fact]
        public async Task SmsList_SinceOffsetIsConvertedToUtc()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(receivedAt: T0, id: "1"));

            // 12:00+02:00 == 10:00Z -> included, 12:00:00.001+02:00 -> excluded
            (await ListIds(host, "/api/fakegateways/sms?since=2026-11-17T12:00:00.000%2B02:00")).Should().Equal("1");
            (await ListIds(host, "/api/fakegateways/sms?since=2026-11-17T12:00:00.001%2B02:00")).Should().BeEmpty();
        }

        [Fact]
        public async Task SmsList_CodeRegex_GroupAndWholeMatch()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(text: "overovaci kod: 123456.", id: "1"));

            async Task<string> Code(string regex)
            {
                using var json = JsonDocument.Parse(await host.Client.GetStringAsync("/api/fakegateways/sms?codeRegex=" + Uri.EscapeDataString(regex)));
                var code = json.RootElement[0].GetProperty("code");
                return code.ValueKind == JsonValueKind.Null ? null : code.GetString();
            }

            (await Code(@"kod: (\d{6})")).Should().Be("123456");
            (await Code(@"\d{6}")).Should().Be("123456");
            (await Code(@"\d{9}")).Should().BeNull();
        }

        [Fact]
        public async Task SmsList_InvalidCodeRegex_Returns400()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync("/api/fakegateways/sms?codeRegex=" + Uri.EscapeDataString("("));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetString().Should().Contain("codeRegex");
        }

        [Fact]
        public async Task Wait_NonNumericTimeout_UsesErrorShapeNotProblemDetails()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=abc");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var json = await response.ReadJson();
            json.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("error");
        }

        [Fact]
        public async Task SmsList_CodeRegexTimeout_Returns400QuicklyInsteadOfOneSecondPerItem()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            for (int i = 0; i < 30; i++)
            {
                host.SmsStore.Add(TestData.Sms(text: new string('a', 60) + "!", id: i.ToString()));
            }
            var sw = Stopwatch.StartNew();

            var response = await host.Client.GetAsync("/api/fakegateways/sms?codeRegex=" + Uri.EscapeDataString("^(a+)+$"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetString().Should().Be("codeRegex timed out");
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task SmsWait_CodeRegexTimeout_Returns400()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(text: new string('a', 60) + "!", receivedAt: T0));

            var response = await host.Client.GetAsync("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&codeRegex=" + Uri.EscapeDataString("^(a+)+$"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SmsList_InvalidSince_Returns400()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            (await host.Client.GetAsync("/api/fakegateways/sms?since=yesterday")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ---------------------------------------------------------------- Targito list filters

        [Fact]
        public async Task TargitoList_Filters()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.TargitoStore.Add(TestData.Targito(email: "A@example.com", templateId: "t1", data: new Dictionary<string, string> { ["CONFIRM_URL"] = "x" }, receivedAt: T0, id: "1"));
            host.TargitoStore.Add(TestData.Targito(email: "b@example.com", templateId: "t2", receivedAt: T0.AddMinutes(1), id: "2"));
            host.TargitoStore.Add(TestData.Targito(email: "new@example.com", templateId: null, method: "ChangeEmailAddress", receivedAt: T0.AddMinutes(2), id: "3"));

            (await ListIds(host, "/api/fakegateways/targito")).Should().Equal("3", "2", "1");
            (await ListIds(host, "/api/fakegateways/targito?email=a@example.com")).Should().Equal("1");
            (await ListIds(host, "/api/fakegateways/targito?email=new@example.com")).Should().Equal("3");
            (await ListIds(host, "/api/fakegateways/targito?templateId=t2")).Should().Equal("2");
            (await ListIds(host, "/api/fakegateways/targito?method=ChangeEmailAddress")).Should().Equal("3");
            (await ListIds(host, "/api/fakegateways/targito?dataKey=CONFIRM_URL")).Should().Equal("1");
            (await ListIds(host, "/api/fakegateways/targito?since=2026-11-17T10:01:00Z")).Should().Equal("3", "2");
            (await ListIds(host, "/api/fakegateways/targito?email=a@example.com&templateId=t2")).Should().BeEmpty();
        }

        // ---------------------------------------------------------------- wait

        [Fact]
        public async Task SmsWait_ReturnsOldestMatchingItemImmediately()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(to: "+420700000001", text: "old kod 111111", receivedAt: T0, id: "old"));
            host.SmsStore.Add(TestData.Sms(to: "+420700000001", text: "first kod 222222", receivedAt: T0.AddMinutes(5), id: "first"));
            host.SmsStore.Add(TestData.Sms(to: "+420700000001", text: "second kod 333333", receivedAt: T0.AddMinutes(6), id: "second"));

            var response = await host.Client.GetAsync("/api/fakegateways/sms/wait?to=%2B420700000001&since=2026-11-17T10:02:00Z&codeRegex=" + Uri.EscapeDataString(@"kod (\d+)"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("id").GetString().Should().Be("first");
            json.RootElement.GetProperty("code").GetString().Should().Be("222222");
        }

        [Fact]
        public async Task SmsWait_FindsItemThatArrivesDuringWait()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            string since = DateTime.UtcNow.AddSeconds(-1).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            var sw = Stopwatch.StartNew();

            var waitTask = host.Client.GetAsync("/api/fakegateways/sms/wait?to=%2B420700000001&timeoutSeconds=20&since=" + since);
            await Task.Delay(500);
            waitTask.IsCompleted.Should().BeFalse();
            await host.Client.PostKonzulta(TestData.KonzultaRequest(TestData.KonzultaItem("+420700000001", "kod 654321", "1")));

            var response = await waitTask.WaitAsync(TimeSpan.FromSeconds(10));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("text").GetString().Should().Be("kod 654321");
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
        }

        [Fact]
        public async Task SmsWait_Timeout_Returns404WithError()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync("/api/fakegateways/sms/wait?to=%2B420700000001&timeoutSeconds=1&since=2026-11-17T10:00:00Z");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await response.Content.ReadAsStringAsync()).Should().Be("{\"error\":\"timeout\"}");
        }

        [Fact]
        public async Task SmsWait_IgnoresItemsBeforeSince()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(to: "+420700000001", receivedAt: T0));

            var response = await host.Client.GetAsync("/api/fakegateways/sms/wait?to=%2B420700000001&timeoutSeconds=1&since=2026-11-17T10:00:00.001Z");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData("/api/fakegateways/sms/wait?to=%2B420700000001")]
        [InlineData("/api/fakegateways/targito/wait?email=a@example.com")]
        [InlineData("/api/fakegateways/sms/wait?since=garbage")]
        [InlineData("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=0")]
        [InlineData("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=121")]
        [InlineData("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=abc")]
        [InlineData("/api/fakegateways/targito/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=1.5")]
        [InlineData("/api/fakegateways/targito/wait?since=2026-11-17T10:00:00Z&timeoutSeconds=-5")]
        [InlineData("/api/fakegateways/sms/wait?since=2026-11-17T10:00:00Z&codeRegex=%28")]
        public async Task Wait_InvalidParameters_Return400(string url)
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task TargitoWait_DefaultsToSendMailingToRecipientMethod()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.TargitoStore.Add(TestData.Targito(email: "a@example.com", method: "ChangeEmailAddress", receivedAt: T0, id: "change"));
            host.TargitoStore.Add(TestData.Targito(email: "a@example.com", receivedAt: T0.AddMinutes(1), id: "send"));

            using var json = await (await host.Client.GetAsync("/api/fakegateways/targito/wait?email=a@example.com&since=2026-11-17T09:00:00Z")).ReadJson();
            json.RootElement.GetProperty("id").GetString().Should().Be("send");

            using var json2 = await (await host.Client.GetAsync("/api/fakegateways/targito/wait?email=a@example.com&method=ChangeEmailAddress&since=2026-11-17T09:00:00Z")).ReadJson();
            json2.RootElement.GetProperty("id").GetString().Should().Be("change");
        }

        [Fact]
        public async Task TargitoWait_FindsItemWithDataKeyThatArrivesDuringWait()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            string since = DateTime.UtcNow.AddSeconds(-1).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            var waitTask = host.Client.GetAsync("/api/fakegateways/targito/wait?email=e2e.a@example.com&dataKey=CONFIRM_URL&timeoutSeconds=20&since=" + since);
            await Task.Delay(300);
            await host.Client.PostTargito(TestData.TargitoSend(dataJson: "{\"OTHER\":\"x\"}"));
            await Task.Delay(300);
            waitTask.IsCompleted.Should().BeFalse("the first item has no CONFIRM_URL");
            await host.Client.PostTargito(TestData.TargitoSend());

            var response = await waitTask.WaitAsync(TimeSpan.FromSeconds(10));
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("data").GetProperty("CONFIRM_URL").GetString().Should().Contain("gah=G");
        }

        [Fact]
        public async Task TargitoWait_Timeout_Returns404()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.GetAsync("/api/fakegateways/targito/wait?email=a@example.com&timeoutSeconds=1&since=2026-11-17T10:00:00Z");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await response.Content.ReadAsStringAsync()).Should().Be("{\"error\":\"timeout\"}");
        }

        // ---------------------------------------------------------------- get / delete

        [Fact]
        public async Task GetById_FoundAndNotFound()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            host.SmsStore.Add(TestData.Sms(id: "s1"));
            host.TargitoStore.Add(TestData.Targito(id: "t1"));

            (await host.Client.GetAsync("/api/fakegateways/sms/s1")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await host.Client.GetAsync("/api/fakegateways/sms/nope")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await host.Client.GetAsync("/api/fakegateways/targito/t1")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await host.Client.GetAsync("/api/fakegateways/targito/nope")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            // ids are not interchangeable between gateways
            (await host.Client.GetAsync("/api/fakegateways/sms/t1")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteSms_AllFilterAndById()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            void Seed()
            {
                host.SmsStore.RemoveWhere();
                host.SmsStore.Add(TestData.Sms(to: "+420700000001", id: "1"));
                host.SmsStore.Add(TestData.Sms(to: "+420700000002", id: "2"));
                host.SmsStore.Add(TestData.Sms(to: "00420700000001", id: "3"));
            }

            Seed();
            (await host.Client.DeleteAsync("/api/fakegateways/sms?to=%2B420700000001")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            host.SmsStore.Query().Select(i => i.Id).Should().Equal("2");

            Seed();
            (await host.Client.DeleteAsync("/api/fakegateways/sms/2")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            host.SmsStore.Query().Select(i => i.Id).Should().Equal("1", "3");
            (await host.Client.DeleteAsync("/api/fakegateways/sms/2")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            (await host.Client.DeleteAsync("/api/fakegateways/sms")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            host.SmsStore.Query().Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteTargito_AllFilterAndById()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            void Seed()
            {
                host.TargitoStore.RemoveWhere();
                host.TargitoStore.Add(TestData.Targito(email: "a@example.com", id: "1"));
                host.TargitoStore.Add(TestData.Targito(email: "b@example.com", id: "2"));
                host.TargitoStore.Add(TestData.Targito(email: "A@EXAMPLE.com", id: "3"));
            }

            Seed();
            (await host.Client.DeleteAsync("/api/fakegateways/targito?email=a@example.com")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            host.TargitoStore.Query().Select(i => i.Id).Should().Equal("2");

            Seed();
            (await host.Client.DeleteAsync("/api/fakegateways/targito/1")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            host.TargitoStore.Query().Select(i => i.Id).Should().Equal("2", "3");
            (await host.Client.DeleteAsync("/api/fakegateways/targito/1")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            (await host.Client.DeleteAsync("/api/fakegateways/targito")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            host.TargitoStore.Query().Should().BeEmpty();
        }

        [Fact]
        public async Task MaxStoredItems_IsRespected()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync(o => o.Settings["FakeGateways:MaxStoredItems"] = "2");

            for (int i = 1; i <= 3; i++)
            {
                await host.Client.PostKonzulta(TestData.KonzultaRequest(TestData.KonzultaItem("+420700000001", "t" + i, i.ToString())));
            }

            host.SmsStore.Query().Select(s => s.ExternalSmsId).Should().Equal("2", "3");
        }
    }
}
