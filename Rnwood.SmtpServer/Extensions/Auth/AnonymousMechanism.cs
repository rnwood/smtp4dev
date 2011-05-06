namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AnonymousMechanism : IAuthMechanism
    {
        #region IAuthMechanism Members

        public string Identifier
        {
            get { return "ANONYMOUS"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection)
        {
            return new AnonymousMechanismProcessor(connection);
        }

        public bool IsPlainText
        {
            get { return false; }
        }

        #endregion
    }
}