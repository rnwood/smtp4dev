using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class PlainMechanism : AuthMechanism
    {
        public override string Identifier
        {
            get { return "PLAIN"; }
        }

        public override AuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor)
        {
            return new PlainMechanismProcessor(connectionProcessor);
        }
    }

    public class PlainMechanismProcessor : AuthMechanismProcessor
    {
        public PlainMechanismProcessor(IConnectionProcessor connectionProcessor)
        {
            ConnectionProcessor = connectionProcessor;
        }

        protected IConnectionProcessor ConnectionProcessor { get; private set; }

        public enum States
        {
            Initial,
            AwaitingResponse
        }

        States State { get; set; }

        public override AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                if (State == States.AwaitingResponse)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                               "Missing auth data"));
                }

                ConnectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,""));
                State = States.AwaitingResponse;
                return AuthMechanismProcessorStatus.Continue;
            }
            
            string decodedData = DecodeBase64(data);
            string[] decodedDataParts = decodedData.Split('\0');

            if (decodedDataParts.Length != 3)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                           "Auth data in incorrect format"));
            }

            string username = decodedDataParts[1];
            string password = decodedDataParts[2];

            AuthenticationResult result = ConnectionProcessor.Server.Behaviour.ValidateAuthenticationRequest(ConnectionProcessor,
                                                                               new UsernameAndPasswordAuthenticationRequest
                                                                                   (username, password));

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
