using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class provides data for RTP source related evetns.
    /// </summary>
    public class RTP_SourceEventArgs : EventArgs
    {
        private RTP_Source m_pSource = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="source">RTP source.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>source</b> is null reference.</exception>
        public RTP_SourceEventArgs(RTP_Source source)
        {
            if(source == null){
                throw new ArgumentNullException("source");
            }

            m_pSource = source;
        }


        #region Properties implementation

        /// <summary>
        /// Gets RTP source.
        /// </summary>
        public RTP_Source Source
        {
            get{ return m_pSource; }
        }

        #endregion

    }
}
