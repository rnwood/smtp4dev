#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>
    /// Fake of the Targito JSON-RPC 2.0 API. Always answers HTTP 200 with a JSON-RPC result or error,
    /// so clients do not retry forever on transport level errors.
    /// </summary>
    [Route("targito")]
    [AllowAnonymous]
    [FakeGatewaysEnabled]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TargitoController : ControllerBase
    {
        public const string JsonContentType = "application/json; charset=utf-8";

        private readonly TargitoStore store;
        private readonly IOptionsMonitor<FakeGatewaysOptions> options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<TargitoController> logger;

        public TargitoController(TargitoStore store, IOptionsMonitor<FakeGatewaysOptions> options, IHttpClientFactory httpClientFactory, TimeProvider timeProvider, ILogger<TargitoController> logger)
        {
            this.store = store;
            this.options = options;
            this.httpClientFactory = httpClientFactory;
            this.timeProvider = timeProvider;
            this.logger = logger;
        }

        [HttpPost("JsonRpc")]
        public async Task<IActionResult> JsonRpc()
        {
            JsonElement id = default;
            bool haveId = false;
            try
            {
                string body;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    body = await reader.ReadToEndAsync();
                }

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(body);
                }
                catch (JsonException ex)
                {
                    return Error(default, false, "-32700", "Parse error: " + ex.Message);
                }

                using (doc)
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        return Error(default, false, "-32600", "Invalid Request: a single JSON object is expected (batches are not supported)");
                    }

                    if (root.TryGetProperty("id", out JsonElement idElement))
                    {
                        id = idElement.Clone();
                        haveId = true;
                    }

                    string? method = root.TryGetProperty("method", out JsonElement m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                    if (string.IsNullOrEmpty(method))
                    {
                        return Error(id, haveId, "-32600", "Invalid Request: method is missing");
                    }

                    JsonElement parameters = root.TryGetProperty("params", out JsonElement p) ? p : default;
                    string? user = null;
                    if (root.TryGetProperty("auth", out JsonElement auth) && auth.ValueKind == JsonValueKind.Object)
                    {
                        // Only username is read. auth.password is deliberately ignored.
                        user = GetString(auth, "username");
                    }

                    switch (method)
                    {
                        case TargitoMethods.SendMailingToRecipient:
                            return HandleSend(id, haveId, parameters, root, user);
                        case TargitoMethods.ChangeEmailAddress:
                            return HandleChangeEmail(id, haveId, parameters, user);
                        default:
                            return await HandleOtherAsync(id, haveId, method, body);
                    }
                }
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fake Targito: unexpected error");
                return Error(id, haveId, "-32603", "Internal error: " + ex.GetType().Name);
            }
        }

        private IActionResult HandleSend(JsonElement id, bool haveId, JsonElement parameters, JsonElement root, string? user)
        {
            string? email = GetString(parameters, "email");
            string? templateId = GetString(parameters, "templateId");
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(templateId))
            {
                return Error(id, haveId, "-32602", "Invalid params: email and templateId are required");
            }

            string storeId = Guid.NewGuid().ToString("N");
            string mailingId = "fake-" + storeId;

            store.Add(new TargitoRecord
            {
                Id = storeId,
                ReceivedAt = KonzultaController.Truncate(timeProvider.GetUtcNow().UtcDateTime),
                Method = TargitoMethods.SendMailingToRecipient,
                Email = email,
                TemplateId = templateId,
                TemplateName = LookupTemplateName(templateId),
                SenderDomain = GetString(root, "sender_domain"),
                SendDateTime = GetString(parameters, "sendDateTime"),
                AccountUser = user,
                Data = ReadData(parameters),
                Params = parameters.Clone(),
                ResultId = mailingId
            });

            logger.LogInformation("Fake Targito: {Method} to {Recipient} template {TemplateId}", TargitoMethods.SendMailingToRecipient, email, templateId);
            return Result(id, haveId, w => { w.WriteStartObject(); w.WriteString("mailingId", mailingId); w.WriteEndObject(); });
        }

        private IActionResult HandleChangeEmail(JsonElement id, bool haveId, JsonElement parameters, string? user)
        {
            string? oldEmail = GetString(parameters, "oldEmail");
            string? newEmail = GetString(parameters, "newEmail");
            if (string.IsNullOrWhiteSpace(oldEmail) || string.IsNullOrWhiteSpace(newEmail))
            {
                return Error(id, haveId, "-32602", "Invalid params: oldEmail and newEmail are required");
            }

            string storeId = Guid.NewGuid().ToString("N");
            string recipientId = Random.Shared.NextInt64(1_000_000, 1_000_000_000_000).ToString();

            store.Add(new TargitoRecord
            {
                Id = storeId,
                ReceivedAt = KonzultaController.Truncate(timeProvider.GetUtcNow().UtcDateTime),
                Method = TargitoMethods.ChangeEmailAddress,
                Email = newEmail,
                OldEmail = oldEmail,
                AccountUser = user,
                Params = parameters.Clone(),
                ResultId = recipientId
            });

            logger.LogInformation("Fake Targito: {Method} from {OldRecipient} to {Recipient}", TargitoMethods.ChangeEmailAddress, oldEmail, newEmail);
            return Result(id, haveId, w => { w.WriteStartObject(); w.WriteString("recipientId", recipientId); w.WriteEndObject(); });
        }

        private async Task<IActionResult> HandleOtherAsync(JsonElement id, bool haveId, string method, string rawBody)
        {
            string? upstream = options.CurrentValue.Targito?.UpstreamUrl;
            if (string.IsNullOrWhiteSpace(upstream))
            {
                // Deliberately an error and never an empty template list - a client could take that as "all templates deleted".
                return Error(id, haveId, "-32601", $"Method {method} not supported by smtp4dev fake Targito gateway");
            }

            if (!Uri.TryCreate(upstream, UriKind.Absolute, out Uri? upstreamUri) || (upstreamUri.Scheme != Uri.UriSchemeHttp && upstreamUri.Scheme != Uri.UriSchemeHttps))
            {
                return Error(id, haveId, "-32000", "Upstream request failed: FakeGateways:Targito:UpstreamUrl is not a valid http(s) URL");
            }

            try
            {
                HttpClient client = httpClientFactory.CreateClient(ServiceCollectionExtensions.TargitoUpstreamHttpClientName);
                using var content = new StringContent(rawBody, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await client.PostAsync(upstreamUri, content, HttpContext.RequestAborted);
                string responseBody = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);

                logger.LogInformation("Fake Targito: forwarded {Method} to upstream, status {StatusCode}", method, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(responseBody))
                {
                    return Error(id, haveId, "-32000", $"Upstream request failed: HTTP {(int)response.StatusCode}");
                }

                if (method == TargitoMethods.GetMailingTemplates && !HasNonEmptyResultArray(responseBody))
                {
                    // Never hand an empty template list to the client - it could take it as "all templates deleted".
                    return Error(id, haveId, "-32000", "Upstream request failed: GetMailingTemplates returned no templates");
                }

                return new ContentResult { StatusCode = 200, Content = responseBody, ContentType = JsonContentType };
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Fake Targito: upstream call for {Method} failed: {Error}", method, ex.GetType().Name);
                return Error(id, haveId, "-32000", "Upstream request failed: " + ex.GetType().Name);
            }
        }

        private static bool HasNonEmptyResultArray(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("result", out JsonElement result)
                    && result.ValueKind == JsonValueKind.Array
                    && result.GetArrayLength() > 0;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private string? LookupTemplateName(string templateId)
        {
            var names = options.CurrentValue.Targito?.TemplateNames;
            if (names == null)
            {
                return null;
            }

            foreach (var pair in names)
            {
                if (string.Equals(pair.Key, templateId, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
            return null;
        }

        private static Dictionary<string, string?> ReadData(JsonElement parameters)
        {
            var data = new Dictionary<string, string?>();
            if (parameters.ValueKind == JsonValueKind.Object
                && parameters.TryGetProperty("data", out JsonElement dataElement)
                && dataElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in dataElement.EnumerateObject())
                {
                    data[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Null => null,
                        _ => property.Value.GetRawText()
                    };
                }
            }
            return data;
        }

        private static string? GetString(JsonElement obj, string name)
        {
            if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(name, out JsonElement value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };
        }

        private static IActionResult Result(JsonElement id, bool haveId, Action<Utf8JsonWriter> writeResult)
        {
            return Respond(id, haveId, w =>
            {
                w.WritePropertyName("result");
                writeResult(w);
            });
        }

        private static IActionResult Error(JsonElement id, bool haveId, string code, string message)
        {
            return Respond(id, haveId, w =>
            {
                w.WriteStartObject("error");
                w.WriteString("code", code);
                w.WriteString("message", message);
                w.WriteEndObject();
            });
        }

        private static IActionResult Respond(JsonElement id, bool haveId, Action<Utf8JsonWriter> writeBody)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("jsonrpc", "2.0");
                writer.WritePropertyName("id");
                if (haveId)
                {
                    id.WriteTo(writer);
                }
                else
                {
                    writer.WriteNullValue();
                }
                writeBody(writer);
                writer.WriteEndObject();
            }

            return new ContentResult { StatusCode = 200, Content = Encoding.UTF8.GetString(stream.ToArray()), ContentType = JsonContentType };
        }
    }
}
