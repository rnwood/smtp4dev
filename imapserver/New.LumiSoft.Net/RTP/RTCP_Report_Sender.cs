using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class holds sender report info.
    /// </summary>
    public class RTCP_Report_Sender
    {
        private ulong m_NtpTimestamp      = 0;
        private uint  m_RtpTimestamp      = 0;
        private uint  m_SenderPacketCount = 0;
        private uint  m_SenderOctetCount  = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="sr">RTCP SR report.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sr</b> is null reference.</exception>
        internal RTCP_Report_Sender(RTCP_Packet_SR sr)
        {
            if(sr == null){
                throw new ArgumentNullException("sr");
            }

            m_NtpTimestamp      = sr.NtpTimestamp;
            m_RtpTimestamp      = sr.RtpTimestamp;
            m_SenderPacketCount = sr.SenderPacketCount;
            m_SenderOctetCount  = sr.SenderOctetCount;
        }


        #region Properties implementation

        /// <summary>
        /// Gets the wallclock time (see Section 4) when this report was sent.
        /// </summary>
        public ulong NtpTimestamp
        {
            get{ return m_NtpTimestamp; }
        }

        /// <summary>
        /// Gets RTP timestamp.
        /// </summary>
        public uint RtpTimestamp
        {
            get{ return m_RtpTimestamp; }
        }

        /// <summary>
        /// Gets how many packets sender has sent.
        /// </summary>
        public uint SenderPacketCount
        {
            get{ return m_SenderPacketCount; }
        }

        /// <summary>
        /// Gets how many bytes sender has sent.
        /// </summary>
        public uint SenderOctetCount
        {
            get{ return m_SenderOctetCount; }
        }

        #endregion

    }
}
