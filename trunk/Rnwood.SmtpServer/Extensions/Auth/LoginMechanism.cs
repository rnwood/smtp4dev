using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class LoginMechanism : AuthMechanism
    {
        public override string Identifier
        {
            get { return "LOGIN"; }
        }

        public override AuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor)
        {
            return new LoginMechanismProcessor(connectionProcessor);
        }
    }

    public class LoginMechanismProcessor : AuthMechanismProcessor
    {
        public LoginMechanismProcessor(IConnectionProcessor processor)
        {
            ConnectionProcessor = processor;
        }

        protected IConnectionProcessor ConnectionProcessor { get; private set; }

        enum States
        {
            Initial,
            WaitingForUsername,
            WaitingForPassword,
            Completed
        }

        States State
        {
            get;
            set;
        }

        public override AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            string username = null;

            switch (State)
            {
                case States.Initial:
                    ConnectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, Convert.ToBase64String(Encoding.ASCII.GetBytes("Username:"))));
                    State = States.WaitingForUsername;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForUsername:

                    username = DecodeBase64(data);

                    ConnectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, Convert.ToBase64String(Encoding.ASCII.GetBytes("Password:"))));
                    State = States.WaitingForPassword;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForPassword:
                    string password = DecodeBase64(data);
                    State = States.Completed;

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

                default:
                    throw new NotImplementedException();

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
