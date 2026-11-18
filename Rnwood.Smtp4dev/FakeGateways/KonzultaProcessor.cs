#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DotNet.Globbing;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>Thrown when the request body is not a usable Konzulta XML document.</summary>
    public class KonzultaRequestException : Exception
    {
        public KonzultaRequestException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>One DataItem as parsed from the request.</summary>
    public class KonzultaItem
    {
        public string? SendTime { get; set; }
        public string? MobileOriginate { get; set; }
        public string? MobileTerminate { get; set; }
        public string? Text { get; set; }
        public string? ContentType { get; set; }
        public string? SmsId { get; set; }
    }

    public class KonzultaRequest
    {
        public string? UserName { get; set; }
        public List<KonzultaItem> Items { get; } = new List<KonzultaItem>();
    }

    /// <summary>Parsing, validation and response generation for the fake Konzulta gateway. The password is never read.</summary>
    public static class KonzultaProcessor
    {
        public static KonzultaRequest Parse(string body)
        {
            XDocument doc;
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersFromEntities = 1024
                };
                using var stringReader = new StringReader(body ?? "");
                using var reader = XmlReader.Create(stringReader, settings);
                doc = XDocument.Load(reader);
            }
            catch (XmlException ex)
            {
                // ex.Message may quote a fragment of the document (e.g. part of the password), so only the position is reported.
                throw new KonzultaRequestException($"Invalid XML at line {ex.LineNumber}, position {ex.LinePosition}");
            }

            if (doc.Root == null || doc.Root.Name.LocalName != "SmsServices")
            {
                throw new KonzultaRequestException("Invalid request: root element SmsServices expected");
            }

            var request = new KonzultaRequest();

            // Only UserName is read from the header. Password is deliberately ignored.
            XElement? header = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "DataHeader");
            request.UserName = Child(header, "UserName");

            foreach (XElement item in doc.Descendants().Where(e => e.Name.LocalName == "DataItem"))
            {
                request.Items.Add(new KonzultaItem
                {
                    SendTime = Child(item, "SendTime"),
                    MobileOriginate = Child(item, "MobileOriginate"),
                    MobileTerminate = Child(item, "MobileTerminate"),
                    Text = Child(item, "Text"),
                    ContentType = Child(item, "ContentType"),
                    SmsId = Child(item, "SmsId")
                });
            }

            return request;
        }

        private static string? Child(XElement? parent, string name)
        {
            return parent?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
        }

        /// <summary>True when the item cannot be processed (SmsId, MobileTerminate or Text is missing).</summary>
        public static bool IsIncomplete(KonzultaItem item)
        {
            return string.IsNullOrWhiteSpace(item.SmsId) || string.IsNullOrWhiteSpace(item.MobileTerminate) || string.IsNullOrEmpty(item.Text);
        }

        public static bool IsRefusedNumber(string? phone, IEnumerable<string>? patterns)
        {
            if (patterns == null || string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            string normalized = PhoneNormalizer.Normalize(phone);
            foreach (string pattern in patterns.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                string normalizedPattern = PhoneNormalizer.Normalize(pattern);
                if (string.Equals(normalizedPattern, normalized, StringComparison.Ordinal))
                {
                    return true;
                }

                try
                {
                    if (Glob.Parse(normalizedPattern).IsMatch(normalized))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Invalid glob - only the literal comparison above applies.
                }
            }

            return false;
        }

        /// <summary>Builds the response XML (one DataItem per input item with the same SmsId).</summary>
        public static string BuildResponse(IEnumerable<(string SmsId, int Status, string MsgId)> results)
        {
            var dataArray = new XElement("DataArray");
            foreach (var r in results)
            {
                dataArray.Add(new XElement("DataItem",
                    new XElement("SmsId", r.SmsId),
                    new XElement("Status", r.Status),
                    new XElement("MsgId", r.MsgId)));
            }

            var root = new XElement("SmsServices",
                new XElement("DataHeader", new XElement("DataType", "SMS")),
                dataArray);

            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + root.ToString(SaveOptions.DisableFormatting);
        }
    }
}
