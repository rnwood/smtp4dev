using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class holds receiver report info.
    /// </summary>
    public class RTCP_Report_Receiver
    {
        private uint m_FractionLost          = 0;
        private uint  m_CumulativePacketsLost = 0;
        private uint m_ExtHigestSeqNumber    = 0;
        private uint m_Jitter                = 0;
        private uint m_LastSR                = 0;
        private uint m_DelaySinceLastSR      = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="rr">RTCP RR report.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>rr</b> is null reference.</exception>
        internal RTCP_Report_Receiver(RTCP_Packet_ReportBlock rr)
        {
            if(rr == null){
                throw new ArgumentNullException("rr");
            }

            m_FractionLost          = rr.FractionLost;
            m_CumulativePacketsLost = rr.CumulativePacketsLost;
            m_ExtHigestSeqNumber    = rr.ExtendedHighestSeqNo;
            m_Jitter                = rr.Jitter;
            m_LastSR                = rr.LastSR;
            m_DelaySinceLastSR      = rr.DelaySinceLastSR;
        }

        #region Properties implementation

        /// <summary>
        /// Gets the fraction of RTP data packets from source SSRC lost since the previous SR or 
        /// RR packet was sent.
        /// </summary>
        public uint FractionLost
        {
            get{ return m_FractionLost; }
        }

        /// <summary>
        /// Gets total number of RTP data packets from source SSRC that have
        /// been lost since the beginning of reception.
        /// </summary>
        public uint CumulativePacketsLost
        {
            get{ return m_CumulativePacketsLost; }
        }

        /// <summary>
        /// Gets extended highest sequence number received.
        /// </summary>
        public uint ExtendedSequenceNumber
        {
            get{ return m_ExtHigestSeqNumber; }
        }

        /// <summary>
        /// Gets an estimate of the statistical variance of the RTP data packet
        /// interarrival time, measured in timestamp units and expressed as an
        /// unsigned integer.
        /// </summary>
        public uint Jitter
        {
            get{ return m_Jitter; }
        }

        /// <summary>
        /// Gets when last sender report(SR) was recieved.
        /// </summary>
        public uint LastSR
        {
            get{ return m_LastSR; }
        }

        /// <summary>
        /// Gets delay since last sender report(SR) was received.
        /// </summary>
        public uint DelaySinceLastSR
        {
            get{ return m_DelaySinceLastSR; }
        }

        #endregion
    }
}
