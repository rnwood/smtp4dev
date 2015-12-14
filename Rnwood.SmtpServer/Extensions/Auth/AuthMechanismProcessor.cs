using System;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public abstract class AuthMechanismProcessor : IAuthMechanismProcessor
    {
        public AuthMechanismProcessor(IConnection connection)
        {
            this.Connection = connection;
        }

        public IAuthenticationCredentials Credentials { get; protected set; }
        public IConnection Connection { get; private set; }

        private static string EncodeBase64(string asciiString)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(asciiString));
        }

        protected static string DecodeBase64(string data)
        {
            try
            {
                return Encoding.ASCII.GetString(Convert.FromBase64String(data));
            }
            catch (FormatException)
            {
                throw new BadBase64Exception(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                               "Bad Base64 data"));
            }
        }

        public abstract AuthMechanismProcessorStatus ProcessResponse(string data);
    }
}