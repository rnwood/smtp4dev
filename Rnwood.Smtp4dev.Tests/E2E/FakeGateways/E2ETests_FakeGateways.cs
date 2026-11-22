using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.FakeGateways
{
    /// <summary>
    /// Starts the real smtp4dev process and talks to the fake gateways over HTTP only.
    /// IMAP and POP3 stay enabled because the E2E harness waits for their startup lines.
    /// </summary>
    [Collection("E2ETests")]
    public class E2ETests_FakeGateways : E2ETests
    {
        private const string Password = "E2E-SECRET-PASSWORD";

        public E2ETests_FakeGateways(ITestOutputHelper output) : base(output)
        {
        }

        private void RunWithExtraArgs(string[] extraArgs, Action<E2ETestContext> test, E2ETestOptions options = null)
        {
            string previous = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_ARGS");
            try
            {
                if (extraArgs.Length > 0)
                {
                    string joined = string.Join("\n", extraArgs);
                    Environment.SetEnvironmentVariable("SMTP4DEV_E2E_ARGS", string.IsNullOrEmpty(previous) ? joined : previous + "\n" + joined);
                }
                RunE2ETest(test, options);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SMTP4DEV_E2E_ARGS", previous);
            }
        }

        private static string NowIso() => DateTime.UtcNow.AddSeconds(-2).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

        private static HttpClient NewClient(Uri baseUrl)
        {
            // BaseAddress must end with a slash so that relative paths are appended to the base path.
            string url = baseUrl.ToString().TrimEnd('/') + "/";
            return new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(60) };
        }

        private static string KonzultaRequest(string to, string text, string smsId) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?><SmsServices><DataHeader><DataType>SMS</DataType><UserName>api-user</UserName><Password>" + Password
            + "</Password></DataHeader><DataArray><DataTemplate><LongSms>1</LongSms><DataItem><MobileTerminate>" + to + "</MobileTerminate><Text>" + text
            + "</Text><SmsId>" + smsId + "</SmsId></DataItem></DataTemplate></DataArray></SmsServices>";

        private static string TargitoRequest(string email, string confirmUrl) =>
            "{\"method\":\"SendMailingToRecipient\",\"auth\":{\"username\":\"api-user\",\"password\":\"" + Password + "\"},\"id\":42,\"jsonrpc\":\"2.0\","
            + "\"params\":{\"email\":\"" + email + "\",\"templateId\":\"tpl-1\",\"data\":{\"CONFIRM_URL\":\"" + confirmUrl + "\"}},\"sender_domain\":\"example.cz\"}";

        /// <summary>The scenario a client application test performs: trigger a message, then read the code/link from smtp4dev.</summary>
        private static void VerifyBothGateways(HttpClient client)
        {
            using (var health = JsonDocument.Parse(client.GetStringAsync("api/fakegateways/health").GetAwaiter().GetResult()))
            {
                Assert.Equal("ok", health.RootElement.GetProperty("status").GetString());
                Assert.Equal(new[] { "konzulta", "targito" }, health.RootElement.GetProperty("features").EnumerateArray().Select(e => e.GetString()).ToArray());
            }

            // ---- Konzulta
            string since = NowIso();
            var smsResponse = client.PostAsync("konzulta/webservices/webservice.aspx",
                new StringContent(KonzultaRequest("+420700000001", "pouzijte tento overovaci kod: 123456.", "987654"), Encoding.UTF8, "text/xml")).GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.OK, smsResponse.StatusCode);
            var smsXml = XDocument.Parse(smsResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            Assert.Equal("987654", smsXml.Descendants("DataItem").Single().Element("SmsId").Value);
            Assert.Equal("0", smsXml.Descendants("DataItem").Single().Element("Status").Value);

            string waitUrl = "api/fakegateways/sms/wait?to=" + Uri.EscapeDataString("+420700000001") + "&since=" + Uri.EscapeDataString(since)
                + "&codeRegex=" + Uri.EscapeDataString(@"(\d{6})") + "&timeoutSeconds=10";
            using (var sms = JsonDocument.Parse(client.GetStringAsync(waitUrl).GetAwaiter().GetResult()))
            {
                Assert.Equal("123456", sms.RootElement.GetProperty("code").GetString());
                Assert.Equal("ACCEPTED", sms.RootElement.GetProperty("status").GetString());
                Assert.Equal("987654", sms.RootElement.GetProperty("externalSmsId").GetString());
            }

            // ---- Targito
            since = NowIso();
            string confirmUrl = "https://app.example.com/verify?gah=G&code=C";
            var targitoResponse = client.PostAsync("targito/JsonRpc", new StringContent(TargitoRequest("e2e.a@example.com", confirmUrl), new UTF8Encoding(false), "application/json")).GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.OK, targitoResponse.StatusCode);
            using (var rpc = JsonDocument.Parse(targitoResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()))
            {
                Assert.Equal(42, rpc.RootElement.GetProperty("id").GetInt32());
                Assert.StartsWith("fake-", rpc.RootElement.GetProperty("result").GetProperty("mailingId").GetString());
            }

            string targitoWaitUrl = "api/fakegateways/targito/wait?email=e2e.a@example.com&dataKey=CONFIRM_URL&since=" + Uri.EscapeDataString(since) + "&timeoutSeconds=10";
            using (var item = JsonDocument.Parse(client.GetStringAsync(targitoWaitUrl).GetAwaiter().GetResult()))
            {
                Assert.Equal(confirmUrl, item.RootElement.GetProperty("data").GetProperty("CONFIRM_URL").GetString());
                Assert.Equal("example.cz", item.RootElement.GetProperty("senderDomain").GetString());
            }

            // ---- passwords never come back from the API
            Assert.DoesNotContain(Password, client.GetStringAsync("api/fakegateways/sms").GetAwaiter().GetResult());
            Assert.DoesNotContain(Password, client.GetStringAsync("api/fakegateways/targito").GetAwaiter().GetResult());

            // ---- wait times out with 404
            var timeout = client.GetAsync("api/fakegateways/sms/wait?to=%2B420999&timeoutSeconds=1&since=" + Uri.EscapeDataString(NowIso())).GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.NotFound, timeout.StatusCode);
            Assert.Equal("{\"error\":\"timeout\"}", timeout.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            // ---- delete
            Assert.Equal(HttpStatusCode.NoContent, client.DeleteAsync("api/fakegateways/sms").GetAwaiter().GetResult().StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, client.DeleteAsync("api/fakegateways/targito").GetAwaiter().GetResult().StatusCode);
            Assert.Equal("[]", client.GetStringAsync("api/fakegateways/sms").GetAwaiter().GetResult());
        }

        [SkipInDockerFact]
        public void FakeGateways_EnabledByCommandLineSwitch_WorkOverHttp()
        {
            RunWithExtraArgs(new[] { "--fakegateways" }, context =>
            {
                using var client = NewClient(context.BaseUrl);
                VerifyBothGateways(client);
            });
        }

        [SkipInDockerFact]
        public void FakeGateways_EnabledByEnvironmentVariable_WorkOverHttp()
        {
            var options = new E2ETestOptions();
            options.EnvironmentVariables["FakeGateways__Enabled"] = "true";

            RunE2ETest(context =>
            {
                using var client = NewClient(context.BaseUrl);
                VerifyBothGateways(client);
            }, options);
        }

        [SkipInDockerFact]
        public void FakeGateways_WithBasePath_AreServedBelowBasePath()
        {
            var options = new E2ETestOptions { BasePath = "/smtp4dev" };

            RunWithExtraArgs(new[] { "--fakegateways" }, context =>
            {
                Assert.EndsWith("/smtp4dev", context.BaseUrl.ToString().TrimEnd('/'));
                using var client = NewClient(context.BaseUrl);
                VerifyBothGateways(client);

                // Not available without the base path
                var root = new HttpClient { BaseAddress = new Uri(context.BaseUrl.GetLeftPart(UriPartial.Authority) + "/") };
                var response = root.PostAsync("targito/JsonRpc", new StringContent("{}", Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            }, options);
        }

        [SkipInDockerFact]
        public void FakeGateways_DisabledByDefault_EndpointsReturn404()
        {
            RunE2ETest(context =>
            {
                using var client = NewClient(context.BaseUrl);

                using (var health = JsonDocument.Parse(client.GetStringAsync("api/fakegateways/health").GetAwaiter().GetResult()))
                {
                    Assert.Equal("disabled", health.RootElement.GetProperty("status").GetString());
                    Assert.Empty(health.RootElement.GetProperty("features").EnumerateArray());
                }

                Assert.Equal(HttpStatusCode.NotFound, client.PostAsync("konzulta/webservices/webservice.aspx", new StringContent(KonzultaRequest("+420700000001", "x", "1"), Encoding.UTF8, "text/xml")).GetAwaiter().GetResult().StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, client.PostAsync("targito/JsonRpc", new StringContent(TargitoRequest("a@example.com", "https://app.example.com"), Encoding.UTF8, "application/json")).GetAwaiter().GetResult().StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, client.GetAsync("api/fakegateways/sms").GetAwaiter().GetResult().StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, client.GetAsync("api/fakegateways/targito").GetAwaiter().GetResult().StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, client.GetAsync("api/fakegateways/sms/wait?since=" + Uri.EscapeDataString(NowIso())).GetAwaiter().GetResult().StatusCode);
            });
        }

        [SkipInDockerFact]
        public void FakeGateways_WithWebAuthentication_FakeEndpointsStayAnonymousApiRequiresLogin()
        {
            RunWithExtraArgs(new[] { "--fakegateways", "--webauthenticationrequired", "--user=api-test=api-test-pw" }, context =>
            {
                using var client = NewClient(context.BaseUrl);

                // The application under test calls the fake gateway without credentials.
                Assert.Equal(HttpStatusCode.OK, client.PostAsync("konzulta/webservices/webservice.aspx", new StringContent(KonzultaRequest("+420700000001", "kod 111111", "1"), Encoding.UTF8, "text/xml")).GetAwaiter().GetResult().StatusCode);
                Assert.Equal(HttpStatusCode.OK, client.PostAsync("targito/JsonRpc", new StringContent(TargitoRequest("a@example.com", "https://app.example.com/x"), Encoding.UTF8, "application/json")).GetAwaiter().GetResult().StatusCode);
                Assert.Equal(HttpStatusCode.OK, client.GetAsync("api/fakegateways/health").GetAwaiter().GetResult().StatusCode);

                // The API is protected like the rest of the smtp4dev API.
                Assert.Equal(HttpStatusCode.Unauthorized, client.GetAsync("api/fakegateways/sms").GetAwaiter().GetResult().StatusCode);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("api-test:api-test-pw")));
                using var sms = JsonDocument.Parse(client.GetStringAsync("api/fakegateways/sms").GetAwaiter().GetResult());
                Assert.Equal(1, sms.RootElement.GetArrayLength());
            });
        }
    }
}
