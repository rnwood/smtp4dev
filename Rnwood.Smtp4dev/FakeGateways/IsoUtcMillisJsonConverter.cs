#nullable enable
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>
    /// Writes DateTime as ISO-8601 UTC with exactly 3 decimals and a Z suffix (2026-11-17T10:15:30.123Z).
    /// </summary>
    public class IsoUtcMillisJsonConverter : JsonConverter<DateTime>
    {
        public const string Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}
