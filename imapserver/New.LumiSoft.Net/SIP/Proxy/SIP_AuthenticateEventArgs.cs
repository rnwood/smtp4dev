using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// This class provides data for SIP_ProxyCore.Authenticate event.
    /// </summary>
    public class SIP_AuthenticateEventArgs
    {
        private Auth_HttpDigest m_pAuth         = null;
        private bool            m_Authenticated = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="auth">Authentication context.</param>
        public SIP_AuthenticateEventArgs(Auth_HttpDigest auth)
        {
            m_pAuth = auth;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets authentication context.
        /// </summary>
        public Auth_HttpDigest AuthContext
        {
            get{ return m_pAuth; }
        }

        /// <summary>
        /// Gets or sets if specified request is authenticated.
        /// </summary>
        public bool Authenticated
        {
            get{ return m_Authenticated; }

            set{ m_Authenticated = value; }
        }

        #endregion

    }
}
