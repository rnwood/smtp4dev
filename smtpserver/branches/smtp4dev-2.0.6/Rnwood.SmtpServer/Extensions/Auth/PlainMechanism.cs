#region

using System;
using System.Text;

#endregion

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class PlainMechanism : IAuthMechanism
    {
        #region IAuthMechanism Members

        public string Identifier
        {
            get { return "PLAIN"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection)
        {
            return new PlainMechanismProcessor(connection);
        }

        public bool IsPlainText
        {
            get { return true; }
        }

        #endregion
    }

    public class PlainMechanismProcessor : IAuthMechanismProcessor
    {
        #region States enum

        public enum States
        {
            Initial,
            AwaitingResponse
        }

        #endregion

        public PlainMechanismProcessor(IConnection connection)
        {
            Connection = connection;
        }

        protected IConnection Connection { get; private set; }

        private States State { get; set; }

        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                if (State == States.AwaitingResponse)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                                   "Missing auth data"));
                }

                Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, ""));
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

            Credentials = new UsernameAndPasswordAuthenticationRequest(username, password);

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
    }
}