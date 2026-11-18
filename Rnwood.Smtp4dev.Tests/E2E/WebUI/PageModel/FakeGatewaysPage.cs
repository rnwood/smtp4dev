using Microsoft.Playwright;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel
{
    /// <summary>Page model for the SMS and Targito tabs plus helpers that play the role of the application calling the gateways.</summary>
    public class FakeGatewaysPage
    {
        private readonly IPage page;
        private readonly Uri baseUrl;

        public FakeGatewaysPage(IPage page, Uri baseUrl)
        {
            this.page = page;
            this.baseUrl = baseUrl;
        }

        #region Tabs

        public ILocator SmsTab => page.GetByTestId("tab-sms");
        public ILocator TargitoTab => page.GetByTestId("tab-targito");
        public ILocator MessagesTab => page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Messages" });

        public ILocator ActiveTab => page.Locator("#maintabs .el-tabs__item.is-active");

        #endregion

        #region SMS

        public ILocator SmsList => page.GetByTestId("sms-list");
        public ILocator SmsRows => SmsList.Locator("table.el-table__body tr.el-table__row");
        public ILocator SmsRowById(string id) => SmsList.Locator($"table.el-table__body tr.el-table__row:has([data-sms-id='{id}'])");
        public ILocator SmsView => page.GetByTestId("sms-view");
        public ILocator SmsViewText => page.GetByTestId("sms-view-text");
        public ILocator SmsViewTo => page.GetByTestId("sms-view-to");
        public ILocator SmsViewDelete => page.GetByTestId("sms-view-delete");

        #endregion

        #region Targito

        public ILocator TargitoList => page.GetByTestId("targito-list");
        public ILocator TargitoRows => TargitoList.Locator("table.el-table__body tr.el-table__row");
        public ILocator TargitoView => page.GetByTestId("targito-view");
        public ILocator TargitoViewDelete => page.GetByTestId("targito-view-delete");
        public ILocator TargitoDataRow(string key) => page.GetByTestId("targito-view-data").Locator($"tr[data-key='{key}']");

        #endregion

        public async Task OpenSmsTabAsync()
        {
            await SmsTab.ClickAsync();
            await SmsList.WaitForAsync();
        }

        public async Task OpenTargitoTabAsync()
        {
            await TargitoTab.ClickAsync();
            await TargitoList.WaitForAsync();
        }

        #region Calls made by the "application under test"

        private HttpClient NewClient()
        {
            // BaseAddress must end with a slash so that relative paths are appended to the base path.
            return new HttpClient { BaseAddress = new Uri(baseUrl.ToString().TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task PostSmsAsync(string to, string text, string smsId)
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><SmsServices><DataHeader><DataType>SMS</DataType><UserName>api-user</UserName><Password>pw</Password></DataHeader>"
                + "<DataArray><DataTemplate><LongSms>1</LongSms><DataItem><MobileTerminate>" + to + "</MobileTerminate><Text>" + System.Security.SecurityElement.Escape(text)
                + "</Text><SmsId>" + smsId + "</SmsId></DataItem></DataTemplate></DataArray></SmsServices>";

            using var client = NewClient();
            var response = await client.PostAsync("konzulta/webservices/webservice.aspx", new StringContent(xml, Encoding.UTF8, "text/xml"));
            response.EnsureSuccessStatusCode();
        }

        /// <param name="dataJson">The JSON object literal for params.data, e.g. {"CONFIRM_URL":"https://..."}.</param>
        public async Task PostTargitoAsync(string email, string dataJson)
        {
            string json = "{\"method\":\"SendMailingToRecipient\",\"auth\":{\"username\":\"api-user\",\"password\":\"pw\"},\"id\":1,\"jsonrpc\":\"2.0\","
                + "\"params\":{\"email\":\"" + email + "\",\"templateId\":\"tpl-1\",\"data\":" + dataJson + "},\"sender_domain\":\"example.cz\"}";

            using var client = NewClient();
            var response = await client.PostAsync("targito/JsonRpc", new StringContent(json, new UTF8Encoding(false), "application/json"));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Returns the ids of the stored SMS, newest first.</summary>
        public async Task<string[]> GetSmsIdsAsync()
        {
            using var client = NewClient();
            using var doc = JsonDocument.Parse(await client.GetStringAsync("api/fakegateways/sms"));
            var ids = new System.Collections.Generic.List<string>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                ids.Add(item.GetProperty("id").GetString());
            }
            return ids.ToArray();
        }

        public async Task<int> GetTargitoCountAsync()
        {
            using var client = NewClient();
            using var doc = JsonDocument.Parse(await client.GetStringAsync("api/fakegateways/targito"));
            return doc.RootElement.GetArrayLength();
        }

        #endregion
    }
}
