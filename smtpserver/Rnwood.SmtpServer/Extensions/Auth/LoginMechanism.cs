#region

#endregion

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class LoginMechanism : IAuthMechanism
    {
        #region IAuthMechanism Members

        public string Identifier
        {
            get { return "LOGIN"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection)
        {
            return new LoginMechanismProcessor(connection);
        }

        public bool IsPlainText
        {
            get { return true; }
        }

        #endregion
    }
}