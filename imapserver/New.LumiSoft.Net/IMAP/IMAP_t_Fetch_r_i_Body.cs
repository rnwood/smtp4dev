using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response BODY[] data-item. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_t_Fetch_r_i_Body : IMAP_t_Fetch_r_i
    {
        private string m_Section = null;
        private int    m_Offset  = -1;
        private Stream m_pStream = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="section">Body section value. Value null means not specified(full message).</param>
        /// <param name="offset">Data starting offset. Value -1 means not specified.</param>
        /// <param name="stream">Data stream.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public IMAP_t_Fetch_r_i_Body(string section,int offset,Stream stream)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_Section = section;
            m_Offset  = offset;
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
        /// Gets BODY section value. Value null means not specified(full message).
        /// </summary>
        public string BodySection
        {
            get{ return m_Section; }
        }

        /// <summary>
        /// Gets BODY data returning start offset. Value -1 means not specified.
        /// </summary>
        public int Offset
        {
            get{ return m_Offset; }
        }

        /// <summary>
        /// Gets data stream.
        /// </summary>
        public Stream Stream
        {
            get{ return m_pStream; }
        }

        #endregion
    }
}
