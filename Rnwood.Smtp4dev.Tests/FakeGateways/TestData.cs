using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.FakeGateways;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    internal static class TestData
    {
        public const string Password = "SECRET-PASSWORD-42";

        public const string KonzultaSpecRequest = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<SmsServices>
<DataHeader>
<DataType>SMS</DataType>
<UserName>api-user</UserName>
<Password>" + Password + @"</Password>
</DataHeader>
<DataArray>
<DataTemplate>
<LongSms>1</LongSms>
<DataItem>
<SendTime>20261117101530</SendTime>
<MobileOriginate>SENDER</MobileOriginate>
<MobileTerminate>+420700000001</MobileTerminate>
<Text>Dobry den, pro zmenu Vaseho mobilniho cisla: +420700000001 pouzijte tento overovaci kod: 123456.</Text>
<ContentType>Unicode</ContentType>
<SmsId>987654</SmsId>
</DataItem>
</DataTemplate>
</DataArray>
</SmsServices>";

        public static string KonzultaItem(string to, string text, string smsId) =>
            $"<DataItem><MobileTerminate>{to}</MobileTerminate><Text>{text}</Text><SmsId>{smsId}</SmsId></DataItem>";

        public static string KonzultaRequest(params string[] items) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?><SmsServices><DataHeader><DataType>SMS</DataType><UserName>api-user</UserName><Password>" + Password
            + "</Password></DataHeader><DataArray><DataTemplate><LongSms>1</LongSms>" + string.Concat(items) + "</DataTemplate></DataArray></SmsServices>";

        public static string TargitoSend(string id = "42", string email = "e2e.a@example.com", string templateId = "tpl-1", string dataJson = "{\"CONFIRM_URL\":\"https://app.example.com/verify?gah=G&code=C\"}") =>
            "{\"method\":\"SendMailingToRecipient\",\"auth\":{\"username\":\"api-user\",\"password\":\"" + Password + "\"},\"id\":" + id
            + ",\"jsonrpc\":\"2.0\",\"params\":{\"email\":\"" + email + "\",\"templateId\":\"" + templateId + "\",\"data\":" + dataJson + "},\"sender_domain\":\"example.cz\"}";

        public static Task<HttpResponseMessage> PostKonzulta(this HttpClient client, string xml, string path = "/konzulta/webservices/webservice.aspx") =>
            client.PostAsync(path, new StringContent(xml, Encoding.UTF8, "text/xml"));

        public static Task<HttpResponseMessage> PostTargito(this HttpClient client, string json, string path = "/targito/JsonRpc") =>
            client.PostAsync(path, new StringContent(json, new UTF8Encoding(false), "application/json"));

        public static async Task<JsonDocument> ReadJson(this HttpResponseMessage response) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        public static SmsMessage Sms(string to = "+420700000001", string text = "hello", DateTime? receivedAt = null, string id = null) => new SmsMessage
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            ReceivedAt = receivedAt ?? DateTime.UtcNow,
            To = to,
            Text = text,
            MsgId = "fake-x",
            ExternalSmsId = "1"
        };

        public static TargitoRecord Targito(string email = "a@example.com", string templateId = "t1", string method = "SendMailingToRecipient",
            Dictionary<string, string> data = null, DateTime? receivedAt = null, string id = null) => new TargitoRecord
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            ReceivedAt = receivedAt ?? DateTime.UtcNow,
            Method = method,
            Email = email,
            TemplateId = templateId,
            Data = data == null ? new Dictionary<string, string>() : new Dictionary<string, string>(data),
            Params = JsonDocument.Parse("{}").RootElement.Clone()
        };
    }
}
