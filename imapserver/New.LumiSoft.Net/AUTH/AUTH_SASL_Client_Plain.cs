using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// Implements "PLAIN" authenticaiton.
    /// </summary>
    public class AUTH_SASL_Client_Plain : AUTH_SASL_Client
    {
        private bool   m_IsCompleted = false;
        private int    m_State       = 0;
        private string m_UserName    = null;
        private string m_Password    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="userName">User login name.</param>
        /// <param name="password">User password.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> or <b>password</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public AUTH_SASL_Client_Plain(string userName,string password)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(userName == string.Empty){
                throw new ArgumentException("Argument 'username' value must be specified.","userName");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }

            m_UserName = userName;
            m_Password = password;
        }


        #region method Continue

        /// <summary>
        /// Continues authentication process.
        /// </summary>
        /// <param name="serverResponse">Server sent SASL response.</param>
        /// <returns>Returns challange request what must be sent to server or null if authentication has completed.</returns>
        /// <exception cref="InvalidOperationException">Is raised when this method is called when authentication is completed.</exception>
        public override byte[] Continue(byte[] serverResponse)
        {
            if(m_IsCompleted){
                throw new InvalidOperationException("Authentication is completed.");
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

            if(m_State == 0){
                m_State++;
                m_IsCompleted = true;

                return Encoding.UTF8.GetBytes("\0" + m_UserName + "\0" + m_Password);
            }
            else{
                throw new InvalidOperationException("Authentication is completed.");
            }
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
        /// Returns always "PLAIN".
        /// </summary>
        public override string Name
        {
            get { return "PLAIN"; }
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public override string UserName
        {
            get{ return m_UserName; }
        }

        /// <summary>
        /// Gets if the authentication method supports SASL client "inital response".
        /// </summary>
        public override bool SupportsInitialResponse
        {
            get{ return true; }
        }

        #endregion
    }
}
