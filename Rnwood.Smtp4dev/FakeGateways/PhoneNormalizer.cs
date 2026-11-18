#nullable enable
using System.Linq;

namespace Rnwood.Smtp4dev.FakeGateways
{
    public static class PhoneNormalizer
    {
        /// <summary>Removes whitespace and replaces a leading 00 with +, so 00420700000001 equals +420700000001.</summary>
        public static string Normalize(string? phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return "";
            }

            string result = new string(phone.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (result.StartsWith("00"))
            {
                result = "+" + result.Substring(2);
            }
            return result;
        }
    }
}
