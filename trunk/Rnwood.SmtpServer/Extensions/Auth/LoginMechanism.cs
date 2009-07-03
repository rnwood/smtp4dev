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

        public override AuthMechanismProcessor CreateAuthMechanismProcessor(ConnectionProcessor connectionProcessor)
        {
            return new LoginMechanismProcessor(connectionProcessor);
        }
    }

    public class LoginMechanismProcessor : AuthMechanismProcessor
    {
        public LoginMechanismProcessor(ConnectionProcessor processor)
        {
            ConnectionProcessor = processor;
        }

        protected ConnectionProcessor ConnectionProcessor { get; private set; }

        enum States
        {
            Initial,
            WaitingForUsername,
            WaitingForPassword,
            Completed
        }

        States State
        { get; set;
        }

        public override AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            switch (State)
            {
                case States.Initial:
                    ConnectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,Convert.ToBase64String(Encoding.ASCII.GetBytes("Username:"))));
                    State = States.WaitingForUsername;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForUsername:

                    string username= DecodeBase64(data);

                    ConnectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, Convert.ToBase64String(Encoding.ASCII.GetBytes("Password:"))));
                    State = States.WaitingForPassword;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForPassword:
                    string password = DecodeBase64(data);
                    State = States.Completed;
                    return AuthMechanismProcessorStatus.Success;

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
