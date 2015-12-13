using System;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class LoginMechanismProcessor : IAuthMechanismProcessor
    {
        public LoginMechanismProcessor(IConnection connection)
        {
            Connection = connection;
            State = States.Initial;
        }

        protected IConnection Connection { get; private set; }

        private States State { get; set; }
        private string _username;

        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            if (data != null)
            {
                State = States.WaitingForUsername;
            }

            switch (State)
            {
                case States.Initial:
                    Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                              Convert.ToBase64String(
                                                                  Encoding.ASCII.GetBytes("Username:"))));
                    State = States.WaitingForUsername;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForUsername:

                    _username = ServerUtility.DecodeBase64(data);

                    Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                              Convert.ToBase64String(
                                                                  Encoding.ASCII.GetBytes("Password:"))));
                    State = States.WaitingForPassword;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForPassword:
                    string password = ServerUtility.DecodeBase64(data);
                    State = States.Completed;

                    Credentials = new LoginAuthenticationCredentials(_username, password);

                    AuthenticationResult result =
                        Connection.Server.Behaviour.ValidateAuthenticationCredentials(Connection,
                                                                                      Credentials);

                    switch (result)
                    {
                        case AuthenticationResult.Success:
                            return AuthMechanismProcessorStatus.Success;
                        default:
                            return AuthMechanismProcessorStatus.Failed;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public IAuthenticationCredentials Credentials { get; set; }

        #endregion

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