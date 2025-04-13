using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response X-GM-THRID data-item. Defined in <see href="http://code.google.com/intl/et/apis/gmail/imap">GMail API</see>.
    /// </summary>
    public class IMAP_t_Fetch_r_i_X_GM_THRID : IMAP_t_Fetch_r_i
    {
        private ulong m_ThreadID = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="threadID">Thread ID.</param>
        public IMAP_t_Fetch_r_i_X_GM_THRID(ulong threadID)
        {
            m_ThreadID = threadID;
        }


        #region Properties implementation

        /// <summary>
        /// Gets thread ID.
        /// </summary>
        public ulong ThreadID
        {
            get{ return m_ThreadID; }
        }

        #endregion
    }
}
