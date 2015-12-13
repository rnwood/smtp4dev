#region

using System;
using System.Text;

#endregion

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class CramMd5Mechanism : IAuthMechanism
    {
        #region IAuthMechanism Members

        public string Identifier
        {
            get { return "CRAM-MD5"; }
        }

        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection)
        {
            return new CramMd5MechanismProcessor(connection, new RandomIntegerGenerator(), new CurrentDateTimeProvider());
        }

        public bool IsPlainText
        {
            get { return false; }
        }

        #endregion
    }

}