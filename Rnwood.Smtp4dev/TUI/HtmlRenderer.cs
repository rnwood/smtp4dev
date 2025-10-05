using System;
using System.Linq;
using Html2Markdown;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Converts HTML content to readable terminal format
    /// </summary>
    public class HtmlRenderer
    {
        public HtmlRenderer()
        {
        }

        public string ConvertHtmlToText(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                // Convert HTML to Markdown-like text for better terminal display
                var converter = new Converter();
                var text = converter.Convert(html);
                
                // Clean up excessive line breaks
                while (text.Contains("\n\n\n"))
                {
                    text = text.Replace("\n\n\n", "\n\n");
                }

                return text.Trim();
            }
            catch (Exception ex)
            {
                return $"[HTML Content - Conversion Error: {ex.Message}]\n\n{StripHtmlTags(html)}";
            }
        }

        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Simple HTML tag removal
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            text = System.Net.WebUtility.HtmlDecode(text);
            return text;
        }

        public bool IsHtmlContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            content = content.TrimStart();
            return content.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                   content.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("<body", StringComparison.OrdinalIgnoreCase);
        }
    }
}
