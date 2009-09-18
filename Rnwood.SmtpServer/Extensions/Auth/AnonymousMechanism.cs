namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AnonymousMechanism : IAuthMechanism
    {
        #region IAuthMechanism Members

        public string Identifier
        {
            get { return "ANONYMOUS"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnectionProcessor connectionProcessor)
        {
            return new AnonymousMechanismProcessor();
        }

        public bool IsPlainText
        {
            get { return false; }
        }

        #endregion
    }

    public class AnonymousMechanismProcessor : IAuthMechanismProcessor
    {
        #region IAuthMechanismProcessor Members

        public AuthMechanismProcessorStatus ProcessResponse(string data)
        {
            return AuthMechanismProcessorStatus.Success;
        }

        #endregion
    }
}