using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// This class provides data for server userName/password authentications.
    /// </summary>
    public class AUTH_e_Authenticate : EventArgs
    {
        private bool   m_IsAuthenticated = false;
        private string m_AuthorizationID = "";
        private string m_UserName        = "";
        private string m_Password        = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="authorizationID">Authorization ID.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the argumnets has invalid value.</exception>
        public AUTH_e_Authenticate(string authorizationID,string userName,string password)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(userName == string.Empty){
                throw new ArgumentException("Argument 'userName' value must be specified.","userName");
            }

            m_AuthorizationID = authorizationID;
            m_UserName        = userName;
            m_Password        = password;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets if specified user is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get{ return m_IsAuthenticated; }

            set{ m_IsAuthenticated = value; }
        }

        /// <summary>
        /// Gets authorization ID.
        /// </summary>
        public string AuthorizationID
        {
            get{ return m_AuthorizationID; }
        }

        /// <summary>
        /// Gets user name.
        /// </summary>
        public string UserName
        {
            get{ return m_UserName; }
        }

        /// <summary>
        /// Gets password.
        /// </summary>
        public string Password
        {
            get{ return m_Password; }
        }

        #endregion
    }
}
