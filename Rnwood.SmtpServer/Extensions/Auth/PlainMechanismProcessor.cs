using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class PlainMechanismProcessor : AuthMechanismProcessor, IAuthMechanismProcessor
    {
        #region States enum

        public enum States
        {
            Initial,
            AwaitingResponse
        }

        #endregion States enum

        public PlainMechanismProcessor(IConnection connection) : base(connection)
        {
        }

        private States State { get; set; }

        #region IAuthMechanismProcessor Members

        public async override Task<AuthMechanismProcessorStatus> ProcessResponseAsync(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                if (State == States.AwaitingResponse)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                                   "Missing auth data"));
                }

                await Connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, ""));
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

            Credentials = new PlainAuthenticationCredentials(username, password);

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

        #endregion IAuthMechanismProcessor Members
    }
}