#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>Fake of the Konzulta SMS HTTP XML API.</summary>
    [Route("konzulta")]
    [AllowAnonymous]
    [FakeGatewaysEnabled]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class KonzultaController : ControllerBase
    {
        private const string XmlContentType = "text/xml; charset=utf-8";

        private readonly SmsStore store;
        private readonly IOptionsMonitor<FakeGatewaysOptions> options;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<KonzultaController> logger;

        public KonzultaController(SmsStore store, IOptionsMonitor<FakeGatewaysOptions> options, TimeProvider timeProvider, ILogger<KonzultaController> logger)
        {
            this.store = store;
            this.options = options;
            this.timeProvider = timeProvider;
            this.logger = logger;
        }

        [HttpPost("webservices/webservice.aspx")]
        [HttpPost("{**any}")]
        public async Task<IActionResult> Send()
        {
            try
            {
                string body;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    body = await reader.ReadToEndAsync();
                }

                KonzultaRequest request;
                try
                {
                    request = KonzultaProcessor.Parse(body);
                }
                catch (KonzultaRequestException ex)
                {
                    logger.LogWarning("Fake Konzulta: rejected request: {Error}", ex.Message);
                    return new ContentResult { StatusCode = 400, Content = ex.Message, ContentType = "text/plain; charset=utf-8" };
                }

                var refuse = options.CurrentValue.Konzulta?.RefuseNumbers;
                var results = new List<(string SmsId, int Status, string MsgId)>();
                var records = new List<SmsMessage>();

                foreach (KonzultaItem item in request.Items)
                {
                    string id = Guid.NewGuid().ToString("N");
                    string msgId = "fake-" + id;

                    bool refused = KonzultaProcessor.IsIncomplete(item) || KonzultaProcessor.IsRefusedNumber(item.MobileTerminate, refuse);
                    var (encoding, segments) = SmsEncodingCalculator.Calculate(item.Text, item.ContentType);

                    records.Add(new SmsMessage
                    {
                        Id = id,
                        ReceivedAt = Truncate(timeProvider.GetUtcNow().UtcDateTime),
                        To = item.MobileTerminate?.Trim() ?? "",
                        Sender = NullIfEmpty(item.MobileOriginate),
                        Text = item.Text ?? "",
                        ContentType = NullIfEmpty(item.ContentType),
                        SendTime = NullIfEmpty(item.SendTime),
                        ExternalSmsId = NullIfEmpty(item.SmsId),
                        GatewayUser = NullIfEmpty(request.UserName),
                        Status = refused ? SmsStatus.Refused : SmsStatus.Accepted,
                        MsgId = msgId,
                        Encoding = encoding,
                        Segments = segments
                    });

                    results.Add((item.SmsId?.Trim() ?? "", refused ? 1 : 0, msgId));
                }

                // Build everything first and store only when the whole response could be produced,
                // so a failed request that the client retries does not create duplicates.
                string responseXml = KonzultaProcessor.BuildResponse(results);

                store.AddRange(records);
                foreach (SmsMessage record in records)
                {
                    logger.LogInformation("Fake Konzulta: SMS to {Recipient} SmsId {SmsId} status {Status}", record.To, record.ExternalSmsId, record.Status);
                }

                return new ContentResult { StatusCode = 200, Content = responseXml, ContentType = XmlContentType };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // UseDeveloperExceptionPage would leak details and return HTML, keep the response simple.
                logger.LogError(ex, "Fake Konzulta: unexpected error");
                return new ContentResult { StatusCode = 400, Content = "Unable to process request", ContentType = "text/plain; charset=utf-8" };
            }
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        internal static DateTime Truncate(DateTime utc) => new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }
}
