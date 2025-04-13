using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// Implements "LOGIN" authenticaiton.
    /// </summary>
    public class AUTH_SASL_Client_Login : AUTH_SASL_Client
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
        public AUTH_SASL_Client_Login(string userName,string password)
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
        /// <exception cref="ArgumentNullException">Is raised when <b>serverResponse</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is called when authentication is completed.</exception>
        public override byte[] Continue(byte[] serverResponse)
        {
            if(serverResponse == null){
                throw new ArgumentNullException("serverResponse");
            }
            if(m_IsCompleted){
                throw new InvalidOperationException("Authentication is completed.");
            }

            /* RFC none.
                S: "Username:"
                C: userName
                S: "Password:"
                C: password
             
                NOTE: UserName may be included in initial client response.
            */

            if(m_State == 0){
                m_State++;

                return Encoding.UTF8.GetBytes(m_UserName);
            }
            else if(m_State == 1){
                m_State++;
                m_IsCompleted = true;

                return Encoding.UTF8.GetBytes(m_Password);
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
        /// Returns always "LOGIN".
        /// </summary>
        public override string Name
        {
            get { return "LOGIN"; }
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public override string UserName
        {
            get{ return m_UserName; }
        }

        #endregion
    }
}
