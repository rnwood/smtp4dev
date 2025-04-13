using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.TCP
{
    /// <summary>
    /// This class provides data to .... .
    /// </summary>
    public class TCP_ServerSessionEventArgs<T> : EventArgs where T : TCP_ServerSession,new()
    {
        private TCP_Server<T> m_pServer  = null;
        private T             m_pSession = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="server">TCP server.</param>
        /// <param name="session">TCP server session.</param>
        internal TCP_ServerSessionEventArgs(TCP_Server<T> server,T session)
        {
            m_pServer  = server;
            m_pSession = session;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets TCP server.
        /// </summary>
        public TCP_Server<T> Server
        {
            get{ return m_pServer; }
        }

        /// <summary>
        /// Gets TCP server session.
        /// </summary>
        public T Session
        {
            get{ return m_pSession; }
        }

        #endregion

    }
}
