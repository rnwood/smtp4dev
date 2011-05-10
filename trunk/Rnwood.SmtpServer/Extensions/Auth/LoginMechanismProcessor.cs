using System;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class LoginMechanismProcessor : IAuthMechanismProcessor
    {
        public LoginMechanismProcessor(IConnection connection)
        {
            Connection = connection;
        }

        protected IConnection Connection { get; private set; }

        private States State { get; set; }
        private string _username;

        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            switch (State)
            {
                case States.Initial:
                    Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                              Convert.ToBase64String(
                                                                  Encoding.ASCII.GetBytes("Username:"))));
                    State = States.WaitingForUsername;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForUsername:

                    _username = DecodeBase64(data);

                    Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                              Convert.ToBase64String(
                                                                  Encoding.ASCII.GetBytes("Password:"))));
                    State = States.WaitingForPassword;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForPassword:
                    string password = DecodeBase64(data);
                    State = States.Completed;

                    Credentials = new LoginAuthenticationCredentials(_username, password);

                    AuthenticationResult result =
                        Connection.Server.Behaviour.ValidateAuthenticationCredentials(Connection,
                                                                                      Credentials);

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

        public IAuthenticationCredentials Credentials { get; set; }

        #endregion

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

        #region Nested type: States

        private enum States
        {
            Initial,
            WaitingForUsername,
            WaitingForPassword,
            Completed
        }

        #endregion
    }
}