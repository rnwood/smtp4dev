using System.Threading.Tasks;

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

        public async Task<AuthMechanismProcessorStatus> ProcessResponseAsync(string data)
        {
            Credentials = new AnonymousAuthenticationCredentials();

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

        public IAuthenticationCredentials Credentials
        {
            get;
            private set;
        }

        #endregion IAuthMechanismProcessor Members
    }
}