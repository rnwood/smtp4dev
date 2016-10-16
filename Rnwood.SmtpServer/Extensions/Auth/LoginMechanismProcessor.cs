using System;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class LoginMechanismProcessor : AuthMechanismProcessor
    {
        public LoginMechanismProcessor(IConnection connection) : base(connection)
        {
            State = States.Initial;
        }

        private States State { get; set; }
        private string _username;

        #region IAuthMechanismProcessor Members

        public async override Task<AuthMechanismProcessorStatus> ProcessResponseAsync(string data)
        {
            if (data != null)
            {
                State = States.WaitingForUsername;
            }

            switch (State)
            {
                case States.Initial:
                    await Connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                              Convert.ToBase64String(
                                                                  Encoding.ASCII.GetBytes("Username:"))));
                    State = States.WaitingForUsername;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForUsername:

                    _username = DecodeBase64(data);

                    await Connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue,
                                                              Convert.ToBase64String(
                                                                  Encoding.ASCII.GetBytes("Password:"))));
                    State = States.WaitingForPassword;
                    return AuthMechanismProcessorStatus.Continue;

                case States.WaitingForPassword:
                    string password = DecodeBase64(data);
                    State = States.Completed;

                    Credentials = new LoginAuthenticationCredentials(_username, password);

                    AuthenticationResult result =
                        await Connection.Server.Behaviour.ValidateAuthenticationCredentialsAsync(Connection,
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

        #endregion IAuthMechanismProcessor Members

        #region Nested type: States

        private enum States
        {
            Initial,
            WaitingForUsername,
            WaitingForPassword,
            Completed
        }

        #endregion Nested type: States
    }
}