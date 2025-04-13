using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response RFC822.SIZE data-item. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_t_Fetch_r_i_Rfc822Size : IMAP_t_Fetch_r_i
    {
        private int m_Size = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="size">Message size in bytes.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_t_Fetch_r_i_Rfc822Size(int size)
        {
            if(size < 0){
                throw new ArgumentException("Argument 'size' value must be >= 0.","size");
            }

            m_Size = size;
        }


        #region Properties implementation

        /// <summary>
        /// Gets message size in bytes.
        /// </summary>
        public int Size
        {
            get{ return m_Size; }
        }

        #endregion
    }
}
