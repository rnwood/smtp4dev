using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response INTERNALDATE data-item. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_t_Fetch_r_i_InternalDate : IMAP_t_Fetch_r_i
    {
        private DateTime m_Date;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="date">IMAP server internal assigned date-time.</param>
        public IMAP_t_Fetch_r_i_InternalDate(DateTime date)
        {
            m_Date = date;
        }


        #region Properties implementation

        /// <summary>
        /// Gets message IMAP server internal assigned date-time.
        /// </summary>
        public DateTime Date
        {
            get{ return m_Date; }
        }

        #endregion
    }
}
