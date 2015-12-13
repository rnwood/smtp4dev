using System;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5MechanismProcessor : IAuthMechanismProcessor
    {
        private readonly IRandomIntegerGenerator _random;
        private readonly ICurrentDateTimeProvider _dateTimeProvider;

        private string _challenge;

        public CramMd5MechanismProcessor(IConnection connection, IRandomIntegerGenerator random, ICurrentDateTimeProvider dateTimeProvider)
        {
            Connection = connection;
            _random = random;
            _dateTimeProvider = dateTimeProvider;
        }

        protected IConnection Connection { get; set; }

        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            if (_challenge == null)
            {
                StringBuilder challenge = new StringBuilder();
                challenge.Append(_random.GenerateRandomInteger(0, Int16.MaxValue));
                challenge.Append(".");
                challenge.Append(_dateTimeProvider.GetCurrentDateTime().Ticks.ToString());
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
                string response = ServerUtility.DecodeBase64(data);
                string[] responseparts = response.Split(' ');

                if (responseparts.Length != 2)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                                   "Response in incorrect format - should be USERNAME RESPONSE"));
                }

                string username = responseparts[0];
                string hash = responseparts[1];

                Credentials = new CramMd5AuthenticationCredentials(username, _challenge, hash);

                AuthenticationResult result =
                    Connection.Server.Behaviour.ValidateAuthenticationCredentials(Connection, Credentials);

                switch (result)
                {
                    case AuthenticationResult.Success:
                        return AuthMechanismProcessorStatus.Success;
                    default:
                        return AuthMechanismProcessorStatus.Failed;
                }
            }
        }

        public IAuthenticationCredentials Credentials { get; private set; }

        #endregion

        #region Nested type: States

        private enum States
        {
            Initial,
            AwaitingResponse
        }

        #endregion
    }
}