#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>An SMS received by the fake Konzulta gateway. Never contains the gateway password.</summary>
    public record SmsMessage : IStoredItem
    {
        public string Id { get; init; } = "";

        [JsonConverter(typeof(IsoUtcMillisJsonConverter))]
        public DateTime ReceivedAt { get; init; }

        public string To { get; init; } = "";
        public string? Sender { get; init; }
        public string Text { get; init; } = "";
        public string? ContentType { get; init; }
        public string? SendTime { get; init; }
        public string? ExternalSmsId { get; init; }
        public string? GatewayUser { get; init; }

        /// <summary>ACCEPTED or REFUSED.</summary>
        public string Status { get; init; } = SmsStatus.Accepted;
        public string MsgId { get; init; } = "";

        /// <summary>GSM7 or UCS2.</summary>
        public string Encoding { get; init; } = "GSM7";
        public int Segments { get; init; } = 1;

        /// <summary>Filled by the API only, when codeRegex was requested.</summary>
        public string? Code { get; init; }
    }

    public static class SmsStatus
    {
        public const string Accepted = "ACCEPTED";
        public const string Refused = "REFUSED";
    }

    /// <summary>A JSON-RPC call received by the fake Targito gateway. Never contains the auth password.</summary>
    public record TargitoRecord : IStoredItem
    {
        public string Id { get; init; } = "";

        [JsonConverter(typeof(IsoUtcMillisJsonConverter))]
        public DateTime ReceivedAt { get; init; }

        public string Method { get; init; } = "";

        /// <summary>Recipient e-mail (newEmail for ChangeEmailAddress).</summary>
        public string? Email { get; init; }
        public string? OldEmail { get; init; }
        public string? TemplateId { get; init; }
        public string? TemplateName { get; init; }
        public string? SenderDomain { get; init; }
        public string? SendDateTime { get; init; }
        public string? AccountUser { get; init; }
        public Dictionary<string, string?> Data { get; init; } = new Dictionary<string, string?>();

        [JsonPropertyName("params")]
        public JsonElement Params { get; init; }

        /// <summary>The returned mailingId / recipientId.</summary>
        public string? ResultId { get; init; }
    }

    public static class TargitoMethods
    {
        public const string SendMailingToRecipient = "SendMailingToRecipient";
        public const string ChangeEmailAddress = "ChangeEmailAddress";
        public const string GetMailingTemplates = "GetMailingTemplates";
        public const string GetMailingStatus = "GetMailingStatus";
        public const string SendMailingToRecipients = "SendMailingToRecipients";
    }

    public class SmsStore : RingBufferStore<SmsMessage>
    {
        public SmsStore(int capacity) : base(capacity) { }
    }

    public class TargitoStore : RingBufferStore<TargitoRecord>
    {
        public TargitoStore(int capacity) : base(capacity) { }
    }
}
