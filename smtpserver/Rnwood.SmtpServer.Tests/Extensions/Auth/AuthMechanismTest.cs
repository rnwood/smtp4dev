using System;
using System.Text;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class AuthMechanismTest
    {
        protected static bool VerifyBase64Response(string base64, string expectedString)
        {
            string decodedString = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
            return decodedString.Equals(expectedString);
        }

        protected static string EncodeBase64(string asciiString)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(asciiString));
        }
    }
}