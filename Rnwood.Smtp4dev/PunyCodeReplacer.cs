using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rnwood.Smtp4dev
{
    internal class PunyCodeReplacer
    {

        private static Regex punycodeRegex = new Regex(@"(?<![a-z0-9-])xn--[a-z0-9-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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