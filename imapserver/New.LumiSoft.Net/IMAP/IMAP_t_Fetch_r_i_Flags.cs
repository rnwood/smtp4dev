using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response FALGS data-item. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_t_Fetch_r_i_Flags : IMAP_t_Fetch_r_i
    {
        private IMAP_t_MsgFlags m_pFlags = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="flags">Message flags.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>flags</b> is null reference.</exception>
        public IMAP_t_Fetch_r_i_Flags(IMAP_t_MsgFlags flags)
        {
            if(flags == null){
                throw new ArgumentNullException("flags");
            }

            m_pFlags = flags;
        }


        #region Properties implementation

        /// <summary>
        /// Gets message flags.
        /// </summary>
        public IMAP_t_MsgFlags Flags
        {
            get{ return m_pFlags; }
        }

        #endregion
    }
}
