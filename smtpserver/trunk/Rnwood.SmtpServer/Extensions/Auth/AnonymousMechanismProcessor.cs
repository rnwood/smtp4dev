namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AnonymousMechanismProcessor : IAuthMechanismProcessor
    {
        public AnonymousMechanismProcessor(IConnection connection)
        {
            Connection = connection;
        }

        protected IConnection Connection { get; private set; }

        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            Credentials = new AnonymousAuthenticationRequest();

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

        public IAuthenticationRequest Credentials
        {
            get;
            private set;
        }

        #endregion
    }
}