#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rnwood.Smtp4dev.FakeGateways
{
    public static class SmsEncodingCalculator
    {
        // GSM 03.38 basic character set (without the escape code).
        private const string GsmBasic =
            "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";

        // GSM 03.38 extension table: each char takes two septets.
        private const string GsmExtension = "\f^{}\\[~]|€";

        public const string Gsm7 = "GSM7";
        public const string Ucs2 = "UCS2";

        public static (string Encoding, int Segments) Calculate(string? text, string? contentType)
        {
            text ??= "";
            bool forceUnicode = string.Equals(contentType?.Trim(), "Unicode", StringComparison.OrdinalIgnoreCase);

            var gsmUnits = new List<int>();
            bool gsm = !forceUnicode;
            if (gsm)
            {
                foreach (char c in text)
                {
                    if (GsmBasic.IndexOf(c) >= 0)
                    {
                        gsmUnits.Add(1);
                    }
                    else if (GsmExtension.IndexOf(c) >= 0)
                    {
                        gsmUnits.Add(2); // escape + char, never split across segments
                    }
                    else
                    {
                        gsm = false;
                        break;
                    }
                }
            }

            if (gsm)
            {
                return (Gsm7, CountSegments(gsmUnits, 160, 153));
            }

            // UCS2: a surrogate pair (2 UTF-16 units) is never split across segments.
            var ucsUnits = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    ucsUnits.Add(2);
                    i++;
                }
                else
                {
                    ucsUnits.Add(1);
                }
            }
            return (Ucs2, CountSegments(ucsUnits, 70, 67));
        }

        /// <summary>
        /// A single segment holds up to singleLimit units; longer messages use multipart segments of multipartLimit units,
        /// filled segment by segment so that an indivisible character (2 units) always stays in one segment.
        /// </summary>
        private static int CountSegments(List<int> charUnits, int singleLimit, int multipartLimit)
        {
            if (charUnits.Sum() <= singleLimit)
            {
                return 1;
            }

            int segments = 1;
            int current = 0;
            foreach (int units in charUnits)
            {
                if (current + units > multipartLimit)
                {
                    segments++;
                    current = 0;
                }
                current += units;
            }
            return segments;
        }
    }
}
