using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5Mechanism : IAuthMechanism
    {
        public string Identifier
        {
            get { return "CRAM-MD5"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor)
        {
            return new CramMd5MechanismProcessor(connectionProcessor);
        }

        public bool IsPlainText
        {
            get { return false; }
        }
    }

    public class CramMd5MechanismProcessor : IAuthMechanismProcessor
    {
        public CramMd5MechanismProcessor(IConnectionProcessor processor)
        {
            ConnectionProcessor = processor;
        }

        protected IConnectionProcessor ConnectionProcessor { get; set; }
        private Random _random = new Random();

        enum States
        {
            Initial,
            AwaitingResponse
        }

        private string _challenge;


        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            if (_challenge == null)
            {
                StringBuilder challenge = new StringBuilder();
                challenge.Append(_random.Next(Int16.MaxValue));
                challenge.Append(".");
                challenge.Append(DateTime.Now.Ticks.ToString());
                challenge.Append("@");
                challenge.Append(ConnectionProcessor.Server.Behaviour.DomainName);
                _challenge = challenge.ToString();

                string base64Challenge = Convert.ToBase64String(Encoding.ASCII.GetBytes(challenge.ToString()));
                ConnectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                                   base64Challenge));
                return AuthMechanismProcessorStatus.Continue;
            } else
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

                AuthenticationResult result = ConnectionProcessor.Server.Behaviour.ValidateAuthenticationRequest(ConnectionProcessor,
                                                                   new CramMd5AuthenticationRequest(username, _challenge, hash));

                switch (result)
                {
                    case AuthenticationResult.Success:
                        return AuthMechanismProcessorStatus.Success;
                        break;
                    default:
                        return AuthMechanismProcessorStatus.Failed;
                        break;
                }

                return AuthMechanismProcessorStatus.Success;
            }
        }

        private static string DecodeBase64(string data)
        {
            try
            {
                return Encoding.ASCII.GetString(Convert.FromBase64String(data));
            }
            catch (FormatException)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                               "Bad Base64 data"));
            }
        }
    }
}
