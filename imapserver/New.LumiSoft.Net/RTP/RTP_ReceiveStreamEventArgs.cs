using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This method provides data for RTP receive stream related events and methods.
    /// </summary>
    public class RTP_ReceiveStreamEventArgs : EventArgs
    {
        private RTP_ReceiveStream m_pStream = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">RTP stream.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public RTP_ReceiveStreamEventArgs(RTP_ReceiveStream stream)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_pStream = stream;
        }


        #region Properties implementation

        /// <summary>
        /// Gets RTP stream.
        /// </summary>
        public RTP_ReceiveStream Stream
        {
            get{ return m_pStream; }
        }

        #endregion

    }
}
