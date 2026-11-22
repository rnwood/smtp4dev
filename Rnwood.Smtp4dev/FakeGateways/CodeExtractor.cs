#nullable enable
using System;
using System.Text.RegularExpressions;

namespace Rnwood.Smtp4dev.FakeGateways
{
    public static class CodeExtractor
    {
        public static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

        /// <summary>Compiles the pattern with a 1 s match timeout. Throws ArgumentException when the pattern is invalid.</summary>
        public static Regex Compile(string pattern)
        {
            return new Regex(pattern, RegexOptions.CultureInvariant, RegexTimeout);
        }

        /// <summary>Returns group 1 when the pattern has it, otherwise the whole match. Null when there is no match. Throws RegexMatchTimeoutException when the 1 s timeout is exceeded.</summary>
        public static string? Extract(Regex regex, string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            Match match = regex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            if (match.Groups.Count > 1 && match.Groups[1].Success)
            {
                return match.Groups[1].Value;
            }
            return match.Value;
        }
    }
}
