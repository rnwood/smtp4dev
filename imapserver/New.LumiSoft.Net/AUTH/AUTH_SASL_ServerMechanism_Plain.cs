using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// Implements "PLAIN" authenticaiton. Defined in RFC 4616.
    /// </summary>
    public class AUTH_SASL_ServerMechanism_Plain : AUTH_SASL_ServerMechanism
    {
        private bool   m_IsCompleted     = false;
        private bool   m_IsAuthenticated = false;
        private bool   m_RequireSSL      = false;
        private string m_UserName        = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="requireSSL">Specifies if this mechanism is available to SSL connections only.</param>
        public AUTH_SASL_ServerMechanism_Plain(bool requireSSL)
        {
            m_RequireSSL = requireSSL;
        }


        #region override method Reset

        /// <summary>
        /// Resets any authentication state data.
        /// </summary>
        public override void Reset()
        {
            m_IsCompleted     = false;
            m_IsAuthenticated = false;
            m_UserName        = "";
        }

        #endregion

        #region override method Continue

        /// <summary>
        /// Continues authentication process.
        /// </summary>
        /// <param name="clientResponse">Client sent SASL response.</param>
        /// <returns>Retunrns challange response what must be sent to client or null if authentication has completed.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>clientResponse</b> is null reference.</exception>
        public override byte[] Continue(byte[] clientResponse)
        {
            if(clientResponse == null){
                throw new ArgumentNullException("clientResponse");
            }

            /* RFC 4616.2. PLAIN SASL Mechanism.                
                The mechanism consists of a single message, a string of [UTF-8]
                encoded [Unicode] characters, from the client to the server.  The
                client presents the authorization identity (identity to act as),
                followed by a NUL (U+0000) character, followed by the authentication
                identity (identity whose password will be used), followed by a NUL
                (U+0000) character, followed by the clear-text password.  As with
                other SASL mechanisms, the client does not provide an authorization
                identity when it wishes the server to derive an identity from the
                credentials and use that as the authorization identity.
             
                message   = [authzid] UTF8NUL authcid UTF8NUL passwd
             
                Example:
                    C: a002 AUTHENTICATE "PLAIN"
                    S: + ""
                    C: {21}
                    C: <NUL>tim<NUL>tanstaaftanstaaf
                    S: a002 OK "Authenticated"
            */

            if(clientResponse.Length == 0){
                return new byte[0];
            }
            // Parse response
            else{
                string[] authzid_authcid_passwd = Encoding.UTF8.GetString(clientResponse).Split('\0');
                if(authzid_authcid_passwd.Length == 3 && !string.IsNullOrEmpty(authzid_authcid_passwd[1])){  
                    m_UserName = authzid_authcid_passwd[1];
                    AUTH_e_Authenticate result = OnAuthenticate(authzid_authcid_passwd[0],authzid_authcid_passwd[1],authzid_authcid_passwd[2]);
                    m_IsAuthenticated = result.IsAuthenticated;
                }

                m_IsCompleted = true;
            }

            return null;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if the authentication exchange has completed.
        /// </summary>
        public override bool IsCompleted
        {
            get{ return m_IsCompleted; }
        }

        /// <summary>
        /// Gets if user has authenticated sucessfully.
        /// </summary>
        public override bool IsAuthenticated
        {
            get{ return m_IsAuthenticated; }
        }

        /// <summary>
        /// Returns always "PLAIN".
        /// </summary>
        public override string Name
        {
            get { return "PLAIN"; }
        }

        /// <summary>
        /// Gets if specified SASL mechanism is available only to SSL connection.
        /// </summary>
        public override bool RequireSSL
        {
            get{ return m_RequireSSL; }
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public override string UserName
        {
            get{ return m_UserName; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is called when authentication mechanism needs to authenticate specified user.
        /// </summary>
        public event EventHandler<AUTH_e_Authenticate> Authenticate = null;

        #region method OnAuthenticate

        /// <summary>
        /// Raises <b>Authenticate</b> event.
        /// </summary>
        /// <param name="authorizationID">Authorization ID.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns authentication result.</returns>
        private AUTH_e_Authenticate OnAuthenticate(string authorizationID,string userName,string password)
        {
            AUTH_e_Authenticate retVal = new AUTH_e_Authenticate(authorizationID,userName,password);

            if(this.Authenticate != null){
                this.Authenticate(this,retVal);
            }

            return retVal;
        }

        #endregion

        #endregion
    }
}
