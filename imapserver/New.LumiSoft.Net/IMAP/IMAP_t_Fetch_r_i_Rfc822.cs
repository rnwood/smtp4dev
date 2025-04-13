using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response RFC822 data-item. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_t_Fetch_r_i_Rfc822 : IMAP_t_Fetch_r_i
    {
        private Stream m_pStream = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">Message stream.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public IMAP_t_Fetch_r_i_Rfc822(Stream stream)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_pStream = stream;
        }


        #region method SetStream

        /// <summary>
        /// Sets Stream property value.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        internal void SetStream(Stream stream)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_pStream = stream;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets message stream.
        /// </summary>
        public Stream Stream
        {
            get{ return m_pStream; }
        }

        #endregion
    }
}
