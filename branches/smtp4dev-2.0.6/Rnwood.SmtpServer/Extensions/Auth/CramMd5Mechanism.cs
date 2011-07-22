#region

using System;
using System.Text;

#endregion

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5Mechanism : IAuthMechanism
    {
        #region IAuthMechanism Members

        public string Identifier
        {
            get { return "CRAM-MD5"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection)
        {
            return new CramMd5MechanismProcessor(connection);
        }

        public bool IsPlainText
        {
            get { return false; }
        }

        #endregion
    }

    public class CramMd5MechanismProcessor : IAuthMechanismProcessor
    {
        private readonly Random _random = new Random();

        private string _challenge;

        public CramMd5MechanismProcessor(IConnection connection)
        {
            Connection = connection;
        }

        protected IConnection Connection { get; set; }

        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            if (_challenge == null)
            {
                StringBuilder challenge = new StringBuilder();
                challenge.Append(_random.Next(Int16.MaxValue));
                challenge.Append(".");
                challenge.Append(DateTime.Now.Ticks.ToString());
                challenge.Append("@");
                challenge.Append(Connection.Server.Behaviour.DomainName);
                _challenge = challenge.ToString();

                string base64Challenge = Convert.ToBase64String(Encoding.ASCII.GetBytes(challenge.ToString()));
                Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                                   base64Challenge));
                return AuthMechanismProcessorStatus.Continue;
            }
            else
            {
                string response = DecodeBase64(data);
                string[] responseparts = response.Split(' ');

                if (responseparts.Length != 2)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                                   "Response in incorrect format - should be USERNAME RESPONSE"));
                }

                string username = responseparts[0];
                string hash = responseparts[1];

                Credentials = new CramMd5AuthenticationRequest(username, _challenge, hash);

                AuthenticationResult result =
                    Connection.Server.Behaviour.ValidateAuthenticationCredentials(Connection, Credentials);

                switch (result)
                {
                    case AuthenticationResult.Success:
                        return AuthMechanismProcessorStatus.Success;
                        break;
                    default:
                        return AuthMechanismProcessorStatus.Failed;
                        break;
                }
            }
        }

        public IAuthenticationRequest Credentials { get; private set; }

        #endregion

        private static string DecodeBase64(string data)
        {
            try
            {
                return Encoding.ASCII.GetString(Convert.FromBase64String(data ?? ""));
            }
            catch (FormatException)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                               "Bad Base64 data"));
            }
        }

        #region Nested type: States

        private enum States
        {
            Initial,
            AwaitingResponse
        }

        #endregion
    }
}