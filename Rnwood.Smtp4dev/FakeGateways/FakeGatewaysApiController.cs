#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>
    /// Read/wait/delete API over the messages received by the fake gateways. Used by the UI and by e2e tests.
    /// </summary>
    [Route("api/fakegateways")]
    [ApiController]
    [FakeGatewaysEnabled]
    public class FakeGatewaysApiController : ControllerBase
    {
        private readonly SmsStore smsStore;
        private readonly TargitoStore targitoStore;

        public FakeGatewaysApiController(SmsStore smsStore, TargitoStore targitoStore)
        {
            this.smsStore = smsStore;
            this.targitoStore = targitoStore;
        }

        // ---------------------------------------------------------------- SMS

        /// <summary>Lists received SMS, newest first.</summary>
        /// <param name="to">Recipient phone number (normalised before comparison).</param>
        /// <param name="since">ISO-8601 time, only items received at or after it are returned.</param>
        /// <param name="textContains">Case-insensitive text fragment.</param>
        /// <param name="codeRegex">.NET regex, group 1 (or the whole match) is returned in the code field.</param>
        [HttpGet("sms")]
        public ActionResult<SmsMessage[]> GetSms(string? to = null, string? since = null, string? textContains = null, string? codeRegex = null)
        {
            if (!TryParseSince(since, false, out DateTime? sinceValue, out ActionResult? error)) return error!;
            if (!TryCompileRegex(codeRegex, out Regex? regex, out error)) return error!;

            var items = smsStore.Query(SmsPredicate(to, sinceValue, textContains));
            try
            {
                return items.Reverse().Select(i => WithCode(i, regex)).ToArray();
            }
            catch (RegexMatchTimeoutException)
            {
                // Stop at the first timeout instead of spending 1 s per stored item.
                return BadRequest(new FakeGatewaysError("codeRegex timed out"));
            }
        }

        /// <summary>Waits for the oldest matching SMS received at or after since. 404 with {"error":"timeout"} when none arrives.</summary>
        [HttpGet("sms/wait")]
        public async Task<ActionResult<SmsMessage>> WaitForSms(string? to = null, string? since = null, string? textContains = null, string? codeRegex = null, string? timeoutSeconds = null)
        {
            if (!TryParseSince(since, true, out DateTime? sinceValue, out ActionResult? error)) return error!;
            if (!TryCompileRegex(codeRegex, out Regex? regex, out error)) return error!;
            if (!TryParseTimeout(timeoutSeconds, out TimeSpan timeout, out error)) return error!;

            try
            {
                SmsMessage? found = await WaitHelper.WaitForOldestAsync(smsStore, SmsPredicate(to, sinceValue, textContains), timeout, HttpContext.RequestAborted);
                if (found == null)
                {
                    return NotFound(new FakeGatewaysError("timeout"));
                }
                return WithCode(found, regex);
            }
            catch (RegexMatchTimeoutException)
            {
                return BadRequest(new FakeGatewaysError("codeRegex timed out"));
            }
            catch (OperationCanceledException)
            {
                return new EmptyResult();
            }
        }

        /// <summary>Gets one SMS.</summary>
        [HttpGet("sms/{id}")]
        public ActionResult<SmsMessage> GetSmsById(string id)
        {
            SmsMessage? item = smsStore.Get(id);
            if (item == null)
            {
                return NotFound(new FakeGatewaysError("not found"));
            }
            return item;
        }

        /// <summary>Deletes SMS for the given recipient, or all SMS when to is not specified.</summary>
        [HttpDelete("sms")]
        public IActionResult DeleteSms(string? to = null)
        {
            smsStore.RemoveWhere(string.IsNullOrWhiteSpace(to) ? null : SmsPredicate(to, null, null));
            return NoContent();
        }

        /// <summary>Deletes one SMS.</summary>
        [HttpDelete("sms/{id}")]
        public IActionResult DeleteSmsById(string id)
        {
            return smsStore.Remove(id) ? NoContent() : NotFound(new FakeGatewaysError("not found"));
        }

        // ------------------------------------------------------------ Targito

        /// <summary>Lists received Targito calls, newest first.</summary>
        /// <param name="email">Recipient e-mail (case-insensitive, newEmail for ChangeEmailAddress).</param>
        /// <param name="templateId">Template id.</param>
        /// <param name="method">JSON-RPC method. All methods when not specified.</param>
        /// <param name="dataKey">Only items having this key in data.</param>
        /// <param name="since">ISO-8601 time, only items received at or after it are returned.</param>
        [HttpGet("targito")]
        public ActionResult<TargitoRecord[]> GetTargito(string? email = null, string? templateId = null, string? method = null, string? dataKey = null, string? since = null)
        {
            if (!TryParseSince(since, false, out DateTime? sinceValue, out ActionResult? error)) return error!;

            return targitoStore.Query(TargitoPredicate(email, templateId, method, dataKey, sinceValue)).Reverse().ToArray();
        }

        /// <summary>Waits for the oldest matching Targito call (method defaults to SendMailingToRecipient).</summary>
        [HttpGet("targito/wait")]
        public async Task<ActionResult<TargitoRecord>> WaitForTargito(string? email = null, string? templateId = null, string? method = null, string? dataKey = null, string? since = null, string? timeoutSeconds = null)
        {
            if (!TryParseSince(since, true, out DateTime? sinceValue, out ActionResult? error)) return error!;
            if (!TryParseTimeout(timeoutSeconds, out TimeSpan timeout, out error)) return error!;

            string effectiveMethod = string.IsNullOrWhiteSpace(method) ? TargitoMethods.SendMailingToRecipient : method;

            try
            {
                TargitoRecord? found = await WaitHelper.WaitForOldestAsync(targitoStore, TargitoPredicate(email, templateId, effectiveMethod, dataKey, sinceValue), timeout, HttpContext.RequestAborted);
                if (found == null)
                {
                    return NotFound(new FakeGatewaysError("timeout"));
                }
                return found;
            }
            catch (OperationCanceledException)
            {
                return new EmptyResult();
            }
        }

        /// <summary>Gets one Targito call.</summary>
        [HttpGet("targito/{id}")]
        public ActionResult<TargitoRecord> GetTargitoById(string id)
        {
            TargitoRecord? item = targitoStore.Get(id);
            if (item == null)
            {
                return NotFound(new FakeGatewaysError("not found"));
            }
            return item;
        }

        /// <summary>Deletes Targito calls for the given e-mail, or all when email is not specified.</summary>
        [HttpDelete("targito")]
        public IActionResult DeleteTargito(string? email = null)
        {
            targitoStore.RemoveWhere(string.IsNullOrWhiteSpace(email) ? null : TargitoPredicate(email, null, null, null, null));
            return NoContent();
        }

        /// <summary>Deletes one Targito call.</summary>
        [HttpDelete("targito/{id}")]
        public IActionResult DeleteTargitoById(string id)
        {
            return targitoStore.Remove(id) ? NoContent() : NotFound(new FakeGatewaysError("not found"));
        }

        // ------------------------------------------------------------ helpers

        private static Func<SmsMessage, bool> SmsPredicate(string? to, DateTime? since, string? textContains)
        {
            string? normalizedTo = string.IsNullOrWhiteSpace(to) ? null : PhoneNormalizer.Normalize(to);
            return i =>
                (normalizedTo == null || PhoneNormalizer.Normalize(i.To) == normalizedTo)
                && (since == null || i.ReceivedAt >= since)
                && (string.IsNullOrEmpty(textContains) || i.Text.Contains(textContains, StringComparison.OrdinalIgnoreCase));
        }

        private static Func<TargitoRecord, bool> TargitoPredicate(string? email, string? templateId, string? method, string? dataKey, DateTime? since)
        {
            return i =>
                (string.IsNullOrWhiteSpace(email) || string.Equals(i.Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(templateId) || string.Equals(i.TemplateId, templateId.Trim(), StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(method) || string.Equals(i.Method, method.Trim(), StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(dataKey) || i.Data.Keys.Any(k => string.Equals(k, dataKey.Trim(), StringComparison.OrdinalIgnoreCase)))
                && (since == null || i.ReceivedAt >= since);
        }

        private static SmsMessage WithCode(SmsMessage item, Regex? regex)
        {
            return regex == null ? item : item with { Code = CodeExtractor.Extract(regex, item.Text) };
        }

        private bool TryParseSince(string? since, bool required, out DateTime? value, out ActionResult? error)
        {
            value = null;
            error = null;
            if (string.IsNullOrWhiteSpace(since))
            {
                if (required)
                {
                    error = BadRequest(new FakeGatewaysError("since is required (ISO-8601, e.g. 2026-11-17T10:15:30.000Z)"));
                    return false;
                }
                return true;
            }

            if (DateTimeOffset.TryParse(since.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
            {
                value = parsed.UtcDateTime;
                return true;
            }

            error = BadRequest(new FakeGatewaysError("since is not a valid ISO-8601 date and time"));
            return false;
        }

        private bool TryParseTimeout(string? timeoutSeconds, out TimeSpan timeout, out ActionResult? error)
        {
            error = null;
            int seconds = WaitHelper.DefaultTimeoutSeconds;
            bool valid = string.IsNullOrWhiteSpace(timeoutSeconds)
                || int.TryParse(timeoutSeconds.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds);
            timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, WaitHelper.MaxTimeoutSeconds));
            if (!valid || seconds < 1 || seconds > WaitHelper.MaxTimeoutSeconds)
            {
                error = BadRequest(new FakeGatewaysError($"timeoutSeconds must be between 1 and {WaitHelper.MaxTimeoutSeconds}"));
                return false;
            }
            return true;
        }

        private bool TryCompileRegex(string? pattern, out Regex? regex, out ActionResult? error)
        {
            regex = null;
            error = null;
            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }

            try
            {
                regex = CodeExtractor.Compile(pattern);
                return true;
            }
            catch (ArgumentException ex)
            {
                error = BadRequest(new FakeGatewaysError("codeRegex is not a valid regular expression: " + ex.Message));
                return false;
            }
        }
    }
}
