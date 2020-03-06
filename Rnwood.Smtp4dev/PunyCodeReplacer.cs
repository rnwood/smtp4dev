using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rnwood.Smtp4dev
{
    internal class PunyCodeReplacer
    {

        private static Regex punycodeRegex = new Regex("xn--[a-z0-9]+", RegexOptions.Compiled);

        public static string DecodePunycode(string value)
        {
            if (value == null)
            {
                return null;
            }

            return punycodeRegex.Replace(value, (m) =>
            {
                try
                {
                    return new IdnMapping().GetUnicode(m.Value);
                }
                catch (ArgumentException)
                {
                    return m.Value;
                }
            })
            ;
        }
    }
}