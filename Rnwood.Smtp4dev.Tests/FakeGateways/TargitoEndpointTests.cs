using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class TargitoEndpointTests
    {
        [Fact]
        public async Task SendMailingToRecipient_StoresRecordAndReturnsMailingId()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync(o => o.Settings["FakeGateways:Targito:TemplateNames:tpl-1"] = "Registration");

            var response = await host.Client.PostTargito(TestData.TargitoSend());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
            json.RootElement.GetProperty("id").GetInt32().Should().Be(42);
            string mailingId = json.RootElement.GetProperty("result").GetProperty("mailingId").GetString();
            mailingId.Should().StartWith("fake-");

            var record = host.TargitoStore.Query().Single();
            record.Method.Should().Be("SendMailingToRecipient");
            record.Email.Should().Be("e2e.a@example.com");
            record.TemplateId.Should().Be("tpl-1");
            record.TemplateName.Should().Be("Registration");
            record.SenderDomain.Should().Be("example.cz");
            record.AccountUser.Should().Be("api-user");
            record.Data["CONFIRM_URL"].Should().Be("https://app.example.com/verify?gah=G&code=C");
            record.Params.GetProperty("email").GetString().Should().Be("e2e.a@example.com");
            record.ResultId.Should().Be(mailingId);
        }

        [Fact]
        public async Task UnicodeEscapesAreDecoded()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            string body = "{\"method\":\"SendMailingToRecipient\",\"id\":1,\"params\":{\"email\":\"a@example.com\",\"templateId\":\"t\",\"data\":{\"NAME\":\"P\\u0159\\u00edli\\u0161 \\u017elu\\u0165ou\\u010dk\\u00fd\"}}}";

            await host.Client.PostTargito(body);

            host.TargitoStore.Query().Single().Data["NAME"].Should().Be("Příliš žluťoučký");
        }

        [Theory]
        [InlineData("7", "7")]
        [InlineData("\"abc-1\"", "\"abc-1\"")]
        [InlineData("null", "null")]
        public async Task IdIsEchoedAsReceived(string idJson, string expectedRaw)
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito(TestData.TargitoSend(id: idJson));

            using var json = await response.ReadJson();
            json.RootElement.GetProperty("id").GetRawText().Should().Be(expectedRaw);
        }

        [Fact]
        public async Task ChangeEmailAddress_StoresNewEmailAsEmailAndReturnsRecipientId()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();
            string body = "{\"method\":\"ChangeEmailAddress\",\"auth\":{\"username\":\"api-user\",\"password\":\"" + TestData.Password + "\"},\"id\":5,\"jsonrpc\":\"2.0\",\"params\":{\"oldEmail\":\"old@example.com\",\"newEmail\":\"new@example.com\"}}";

            var response = await host.Client.PostTargito(body);

            using var json = await response.ReadJson();
            string recipientId = json.RootElement.GetProperty("result").GetProperty("recipientId").GetString();
            recipientId.Should().NotBeNullOrEmpty();
            long.TryParse(recipientId, out _).Should().BeTrue();
            var record = host.TargitoStore.Query().Single();
            record.Method.Should().Be("ChangeEmailAddress");
            record.Email.Should().Be("new@example.com");
            record.OldEmail.Should().Be("old@example.com");
            record.ResultId.Should().Be(recipientId);
        }

        [Fact]
        public async Task MissingParams_ReturnsInvalidParamsError_AndStoresNothing()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito("{\"method\":\"SendMailingToRecipient\",\"id\":1,\"params\":{}}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32602");
            host.TargitoStore.Query().Should().BeEmpty();
        }

        [Theory]
        [InlineData("GetMailingTemplates")]
        [InlineData("GetMailingStatus")]
        [InlineData("SendMailingToRecipients")]
        [InlineData("SomethingUnknown")]
        public async Task OtherMethodsWithoutUpstream_ReturnMethodNotFoundError_NeverAResult(string method)
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito("{\"method\":\"" + method + "\",\"id\":\"x1\",\"jsonrpc\":\"2.0\"}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.TryGetProperty("result", out _).Should().BeFalse("an empty template list must never be returned");
            json.RootElement.GetProperty("id").GetString().Should().Be("x1");
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32601");
            json.RootElement.GetProperty("error").GetProperty("message").GetString().Should().Contain(method);
            host.TargitoStore.Query().Should().BeEmpty();
        }

        [Theory]
        [InlineData("{not json")]
        [InlineData("")]
        public async Task InvalidJson_Returns200WithParseError(string body)
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito(body);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32700");
            json.RootElement.GetProperty("id").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("\"text\"")]
        [InlineData("{\"id\":1}")]
        public async Task NotAJsonRpcObject_Returns200WithInvalidRequestError(string body)
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito(body);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32600");
        }

        [Fact]
        public async Task PathIsCaseInsensitive()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito(TestData.TargitoSend(), "/TARGITO/jsonrpc");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            host.TargitoStore.Query().Should().HaveCount(1);
        }

        [Fact]
        public async Task Password_IsNeverStoredReturnedOrLogged()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync();

            var response = await host.Client.PostTargito(TestData.TargitoSend());
            var api = await host.Client.GetStringAsync("/api/fakegateways/targito");

            (await response.Content.ReadAsStringAsync()).Should().NotContain(TestData.Password);
            api.Should().NotContain(TestData.Password);
            JsonSerializer.Serialize(host.TargitoStore.Query()).Should().NotContain(TestData.Password);
            host.Logs.Entries.Should().NotBeEmpty();
            host.Logs.Entries.Should().NotContain(e => e.Contains(TestData.Password));
            host.Logs.Entries.Should().NotContain(e => e.Contains("CONFIRM_URL") || e.Contains("verify?gah"));
        }

        // ---------------------------------------------------------- upstream proxy

        private class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string, HttpResponseMessage> respond;

            public StubHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond)
            {
                this.respond = respond;
            }

            public List<(Uri Url, string Body, string ContentType)> Requests { get; } = new List<(Uri, string, string)>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request.RequestUri, body, request.Content?.Headers.ContentType?.MediaType));
                return respond(request, body);
            }
        }

        private static Task<FakeGatewaysTestHost> HostWithUpstream(StubHandler handler) => FakeGatewaysTestHost.StartAsync(o =>
        {
            o.Settings["FakeGateways:Targito:UpstreamUrl"] = "https://upstream.example.com/JsonRpc";
            o.ConfigureServices = s => s.AddHttpClient(ServiceCollectionExtensions.TargitoUpstreamHttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        });

        [Fact]
        public async Task Upstream_UnsupportedMethodIsForwardedRawAndResponseReturnedRaw()
        {
            string upstreamResponse = "{\"jsonrpc\":\"2.0\",\"id\":9,\"result\":[{\"templateId\":\"t1\",\"name\":\"N\"}]}";
            var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(upstreamResponse, Encoding.UTF8, "application/json") });
            await using var host = await HostWithUpstream(handler);
            string request = "{\"method\":\"GetMailingTemplates\",\"auth\":{\"username\":\"u\",\"password\":\"p\"},\"id\":9,\"jsonrpc\":\"2.0\"}";

            var response = await host.Client.PostTargito(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be(upstreamResponse);
            handler.Requests.Should().HaveCount(1);
            handler.Requests[0].Url.ToString().Should().Be("https://upstream.example.com/JsonRpc");
            handler.Requests[0].Body.Should().Be(request);
            handler.Requests[0].ContentType.Should().Be("application/json");
            host.TargitoStore.Query().Should().BeEmpty();
            host.Logs.Entries.Should().NotContain(e => e.Contains("\"password\""));
        }

        [Theory]
        [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":[]}")]
        [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1}")]
        [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":null}")]
        [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}")]
        [InlineData("[]")]
        [InlineData("not json")]
        public async Task Upstream_GetMailingTemplatesWithoutTemplates_BecomesJsonRpcError(string upstreamBody)
        {
            var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(upstreamBody, Encoding.UTF8, "application/json") });
            await using var host = await HostWithUpstream(handler);

            var response = await host.Client.PostTargito("{\"method\":\"GetMailingTemplates\",\"id\":1}");

            using var json = await response.ReadJson();
            json.RootElement.TryGetProperty("result", out _).Should().BeFalse();
            json.RootElement.GetProperty("id").GetInt32().Should().Be(1);
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32000");
        }

        [Fact]
        public async Task Upstream_IsNotUsedForSimulatedMethods()
        {
            var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
            await using var host = await HostWithUpstream(handler);

            await host.Client.PostTargito(TestData.TargitoSend());

            handler.Requests.Should().BeEmpty();
            host.TargitoStore.Query().Should().HaveCount(1);
        }

        [Fact]
        public async Task Upstream_FailureBecomesJsonRpcError()
        {
            var handler = new StubHandler((_, _) => throw new HttpRequestException("boom"));
            await using var host = await HostWithUpstream(handler);

            var response = await host.Client.PostTargito("{\"method\":\"GetMailingStatus\",\"id\":3,\"params\":{\"jobId\":\"1\"}}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("id").GetInt32().Should().Be(3);
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32000");
        }

        [Fact]
        public async Task Upstream_HttpErrorStatusBecomesJsonRpcError()
        {
            var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("oops") });
            await using var host = await HostWithUpstream(handler);

            var response = await host.Client.PostTargito("{\"method\":\"GetMailingStatus\",\"id\":3}");

            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32000");
            json.RootElement.GetProperty("error").GetProperty("message").GetString().Should().Contain("500");
        }

        [Fact]
        public async Task Upstream_InvalidUrlBecomesJsonRpcError()
        {
            await using var host = await FakeGatewaysTestHost.StartAsync(o => o.Settings["FakeGateways:Targito:UpstreamUrl"] = "not a url");

            var response = await host.Client.PostTargito("{\"method\":\"GetMailingStatus\",\"id\":3}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await response.ReadJson();
            json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("-32000");
        }
    }
}
