using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response UID data-item. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_t_Fetch_r_i_Uid : IMAP_t_Fetch_r_i
    {
        private long m_UID = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="uid">Message UID value.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_t_Fetch_r_i_Uid(long uid)
        {
            if(uid < 0){
                throw new ArgumentException("Argument 'uid' value must be >= 0.","uid");
            }

            m_UID = uid;
        }


        #region Properties implementation

        /// <summary>
        /// Gets message UID value.
        /// </summary>
        public long UID
        {
            get{ return m_UID; }
        }

        #endregion
    }
}
