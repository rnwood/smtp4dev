using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Started">IMAP_Session.Login</b> event.
    /// </summary>
    public class IMAP_e_Login : EventArgs
    {
        private bool   m_IsAuthenticated = false;
        private string m_User            = "";
        private string m_Password        = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>user</b> or <b>password</b> is null reference.</exception>
        internal IMAP_e_Login(string user,string password)
        {
            if(user == null){
                throw new ArgumentNullException("user");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }

            m_User     = user;
            m_Password = password;
        }


        #region Properties implementataion

        /// <summary>
        /// Gets or sets if specified user is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get{ return m_IsAuthenticated; }

            set{ m_IsAuthenticated = value; }
        }

        /// <summary>
        /// Gets user name.
        /// </summary>
        public string UserName
        {
            get{ return m_User; }
        }

        /// <summary>
        /// Gets user password.
        /// </summary>
        public string Password
        {
            get{ return m_Password; }
        }

        #endregion
    }
}
