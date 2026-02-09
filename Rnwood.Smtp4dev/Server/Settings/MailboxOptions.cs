using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Mail;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public record HeaderFilterOptions
    {
        /// <summary>
        /// The header name to match (e.g., "X-Application", "X-Mailer")
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// The pattern to match against the header value.
        /// - For exact match: "value"
        /// - For regex: "/pattern/" (case-insensitive)
        /// - For existence check: ".*" (header exists with any value)
        /// </summary>
        public string Pattern { get; set; }
    }

    public record SourceFilterOptions
    {
        /// <summary>
        /// The pattern to match against the client hostname (from EHLO/HELO) or IP address.
        /// - For exact match: "hostname.example.com" or "192.168.1.1"
        /// - For wildcard: "*.example.com" or "192.168.*"
        /// - For regex: "/pattern/" (case-insensitive)
        /// </summary>
        public string Pattern { get; set; }
    }

    [TypeConverter(typeof(MailboxFromStringConverter))]
    public record MailboxOptions
    {
        public string Name { get; set; }
        public string Recipients { get; set; }

        /// <summary>
        /// Optional header-based filters for routing messages to this mailbox.
        /// If specified, all filters must match for the message to be routed to this mailbox.
        /// Header filters are checked before recipient patterns.
        /// </summary>
        public HeaderFilterOptions[] HeaderFilters { get; set; }

        /// <summary>
        /// Optional source-based filters for routing messages to this mailbox.
        /// If specified, all filters must match for the message to be routed to this mailbox.
        /// Source filters are checked before header filters and recipient patterns.
        /// Matches against the client hostname (from EHLO/HELO) or IP address.
        /// </summary>
        public SourceFilterOptions[] SourceFilters { get; set; }

        internal const string DEFAULTNAME = "Default";
    }

    public class MailboxFromStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue =  value as string;

            // Check if it's JSON format (for advanced configuration with header filters)
            if (stringValue.TrimStart().StartsWith("{"))
            {
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    };
                    return System.Text.Json.JsonSerializer.Deserialize<MailboxOptions>(stringValue, options);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    throw new FormatException($"Mailbox JSON format is invalid: {ex.Message}");
                }
            }

            // Legacy format: "Name=Recipients"
            string[] values = stringValue.Split('=', 2);

            if (values.Length != 2)
            {
                throw new FormatException("Mailbox must be in format \"Name=Recipients\" or valid JSON");
            }

            return new MailboxOptions { Name = values[0], Recipients = values[1]};
        }
    }
}
