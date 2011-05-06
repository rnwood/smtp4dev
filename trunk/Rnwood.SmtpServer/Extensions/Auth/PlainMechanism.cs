#region



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
}