using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Started">IMAP_Session.Started</b> event.
    /// </summary>
    public class IMAP_e_Started : EventArgs
    {
        private IMAP_r_u_ServerStatus m_pResponse = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="response">IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        internal IMAP_e_Started(IMAP_r_u_ServerStatus response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_pResponse = response;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets IMAP server response.
        /// </summary>
        /// <remarks>Response should be OK,NO with human readable text."</remarks>
        public IMAP_r_u_ServerStatus Response
        {
            get{ return m_pResponse; }

            set{ m_pResponse = value; }
        }

        #endregion
    }
}
