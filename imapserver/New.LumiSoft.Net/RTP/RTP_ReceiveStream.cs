using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// Implements RTP session receive stream.
    /// </summary>
    public class RTP_ReceiveStream
    {
        private bool                   m_IsDisposed      = false;
        private RTP_Session            m_pSession        = null;
        private RTP_Source             m_pSSRC           = null;
        private RTP_Participant_Remote m_pParticipant    = null;
        private int                    m_SeqNoWrapCount  = 0;
        private ushort                 m_MaxSeqNo        = 0;
        private long                   m_PacketsReceived = 0;
        private long                   m_PacketsMisorder = 0;
        private long                   m_BytesReceived   = 0;
        private double                 m_Jitter          = 0;
        private RTCP_Report_Sender     m_pLastSR         = null;
        private uint                   m_BaseSeq         = 0;
        private long                   m_ReceivedPrior   = 0;
        private long                   m_ExpectedPrior   = 0;
        private int                    m_Transit         = 0;
        private uint                   m_LastBadSeqPlus1 = 0;
        private int                    m_Probation       = 0;
        private DateTime               m_LastSRTime      = DateTime.MinValue;
        private int                    MAX_DROPOUT       = 3000;
        private int                    MAX_MISORDER      = 100;
        private int                    MIN_SEQUENTIAL    = 2;
        private uint                   RTP_SEQ_MOD       = (1 << 16);

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner RTP session.</param>
        /// <param name="ssrc">Onwer synchronization source.</param>
        /// <param name="packetSeqNo">RTP packet <b>SeqNo</b> value.</param>
        /// <exception cref="ArgumentNullException">Is riased when <b>session</b> or <b>ssrc</b> is null reference.</exception>
        internal RTP_ReceiveStream(RTP_Session session,RTP_Source ssrc,ushort packetSeqNo)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }
            if(ssrc == null){
                throw new ArgumentNullException("ssrc");
            }

            m_pSession = session;
            m_pSSRC = ssrc;

            // RFC 3550 A.1.
            InitSeq(packetSeqNo);
            m_MaxSeqNo = (ushort)(packetSeqNo - 1);
            m_Probation = MIN_SEQUENTIAL;
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        internal void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            m_pSession = null;
            m_pParticipant = null;

            this.Closed = null;
            this.Timeout = null;
            this.SenderReport = null;
            this.PacketReceived = null;
        }

        #endregion
                        

        #region method Process

        /// <summary>
        /// Processes specified RTP packet thorugh this stream.
        /// </summary>
        /// <param name="packet">RTP packet.</param>
        /// <param name="size">RTP packet size in bytes.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>packet</b> is null reference.</exception>
        internal void Process(RTP_Packet packet,int size)
        {
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            m_BytesReceived += size;

            if(UpdateSeq(packet.SeqNo)){
                OnPacketReceived(packet);

                /* RFC 3550 A.8 Estimating the Interarrival Jitter.
                    The code fragments below implement the algorithm given in Section
                    6.4.1 for calculating an estimate of the statistical variance of the
                    RTP data interarrival time to be inserted in the interarrival jitter
                    field of reception reports.  The inputs are r->ts, the timestamp from
                    the incoming packet, and arrival, the current time in the same units.
                    Here s points to state for the source; s->transit holds the relative
                    transit time for the previous packet, and s->jitter holds the
                    estimated jitter.  The jitter field of the reception report is
                    measured in timestamp units and expressed as an unsigned integer, but
                    the jitter estimate is kept in a floating point.  As each data packet
                    arrives, the jitter estimate is updated:

                        int transit = arrival - r->ts;
                        int d = transit - s->transit;
                        s->transit = transit;
                        if (d < 0) d = -d;
                        s->jitter += (1./16.) * ((double)d - s->jitter);

                    When a reception report block (to which rr points) is generated for
                    this member, the current jitter estimate is returned:

                        rr->jitter = (u_int32) s->jitter;

                */
                uint arrival = RTP_Utils.DateTimeToNTP32(DateTime.Now);
                int transit  = (int)(arrival - packet.Timestamp);
                int d = transit - m_Transit;
                m_Transit = transit;
                if(d < 0){
                    d = -d;
                }
                m_Jitter += (1.0/16.0) * ((double)d - m_Jitter);

            }
            // else Packet not valid, skip it.
        }

        #endregion


        #region method InitSeq

        /// <summary>
        /// Initializes new sequence number.
        /// </summary>
        /// <param name="seqNo">Sequence number.</param>
        private void InitSeq(ushort seqNo)
        {
            // For more info see RFC 3550 A.1.

            m_BaseSeq         = seqNo;
            m_MaxSeqNo        = seqNo;
            m_LastBadSeqPlus1 = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
            m_SeqNoWrapCount  = 0;
            m_PacketsReceived = 0;
            m_ReceivedPrior   = 0;
            m_ExpectedPrior   = 0;
        }

        #endregion

        #region method UpdateSeq

        /// <summary>
        /// Updates sequence number.
        /// </summary>
        /// <param name="seqNo">RTP packet sequence number.</param>
        /// <returns>Returns true if sequence is valid, otherwise false.</returns>
        private bool UpdateSeq(ushort seqNo)
        {
            // For more info see RFC 3550 A.1.

            ushort udelta = (ushort)(seqNo - m_MaxSeqNo);

            /*
             * Source is not valid until MIN_SEQUENTIAL packets with
             * sequential sequence numbers have been received.
            */

            // Stream not validated yet.
            if(m_Probation > 0){
                // The seqNo is in sequence.
                if(seqNo == m_MaxSeqNo + 1){
                    m_Probation--;
                    m_MaxSeqNo = seqNo;

                    // The receive stream has validated ok.
                    if(m_Probation == 0){
                        InitSeq(seqNo);
                        m_PacketsReceived++;

                        // Raise NewReceiveStream event.
                        m_pSession.OnNewReceiveStream(this);

                        return true;
                    }
                }
                else{
                    m_Probation = MIN_SEQUENTIAL - 1;
                    m_MaxSeqNo = seqNo;
                }

                return false;
            }
            // The seqNo is order, with permissible gap.
            else if (udelta < MAX_DROPOUT){
                // The seqNo has wrapped around.
                if(seqNo < m_MaxSeqNo){
                    m_SeqNoWrapCount++;
                }
                m_MaxSeqNo = seqNo;
            }
            // The seqNo made a very large jump.
            else if (udelta <= RTP_SEQ_MOD - MAX_MISORDER){
                if(seqNo == m_LastBadSeqPlus1){
                    /*
                     * Two sequential packets -- assume that the other side
                     * restarted without telling us so just re-sync
                     * (i.e., pretend this was the first packet).
                    */
                    InitSeq(seqNo);
                }
                else{
                    m_LastBadSeqPlus1 = (uint)((long)(seqNo + 1) & (RTP_SEQ_MOD-1));

                    return false;
                }
            }
            else{
                /* duplicate or reordered packet */
                m_PacketsMisorder++;
            }

            m_PacketsReceived++;

            return true;
        }

        #endregion


        #region method SetSR

        /// <summary>
        /// Sets property <b>LastSR</b> value.
        /// </summary>
        /// <param name="report">Sender report.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>report</b> is null reference.</exception>
        internal void SetSR(RTCP_Report_Sender report)
        {
            if(report == null){
                throw new ArgumentNullException("report");
            }

            m_LastSRTime = DateTime.Now;
            m_pLastSR = report;

            OnSenderReport();
        }

        #endregion

        #region method CreateReceiverReport

        /// <summary>
        /// Creates receiver report.
        /// </summary>
        /// <returns>Returns created receiver report.</returns>
        internal RTCP_Packet_ReportBlock CreateReceiverReport()
        {
            /* RFC 3550 A.3 Determining Number of Packets Expected and Lost.
                In order to compute packet loss rates, the number of RTP packets
                expected and actually received from each source needs to be known,
                using per-source state information defined in struct source
                referenced via pointer s in the code below.  The number of packets
                received is simply the count of packets as they arrive, including any
                late or duplicate packets.  The number of packets expected can be
                computed by the receiver as the difference between the highest
                sequence number received (s->max_seq) and the first sequence number
                received (s->base_seq).  Since the sequence number is only 16 bits
                and will wrap around, it is necessary to extend the highest sequence
                number with the (shifted) count of sequence number wraparounds
                (s->cycles).  Both the received packet count and the count of cycles
                are maintained the RTP header validity check routine in Appendix A.1.

                    extended_max = s->cycles + s->max_seq;
                    expected = extended_max - s->base_seq + 1;

                The number of packets lost is defined to be the number of packets
                expected less the number of packets actually received:

                    lost = expected - s->received;

                Since this signed number is carried in 24 bits, it should be clamped
                at 0x7fffff for positive loss or 0x800000 for negative loss rather
                than wrapping around.

                The fraction of packets lost during the last reporting interval
                (since the previous SR or RR packet was sent) is calculated from
                differences in the expected and received packet counts across the
                interval, where expected_prior and received_prior are the values
                saved when the previous reception report was generated:

                    expected_interval = expected - s->expected_prior;
                    s->expected_prior = expected;
                    received_interval = s->received - s->received_prior;
                    s->received_prior = s->received;
                    lost_interval = expected_interval - received_interval;
                    if(expected_interval == 0 || lost_interval <= 0)
                        fraction = 0;
                    else
                        fraction = (lost_interval << 8) / expected_interval;

                The resulting fraction is an 8-bit fixed point number with the binary
                point at the left edge.
            */

            uint extHighestSeqNo = (uint)(m_SeqNoWrapCount << 16 + m_MaxSeqNo);
            uint expected        = extHighestSeqNo - m_BaseSeq + 1;  
         
            int expected_interval = (int)(expected - m_ExpectedPrior);
            m_ExpectedPrior = expected;
            int received_interval = (int)(m_PacketsReceived - m_ReceivedPrior);
            m_ReceivedPrior = m_PacketsReceived;
            int lost_interval = expected_interval - received_interval;
            int fraction = 0;
            if(expected_interval == 0 || lost_interval <= 0){
                fraction = 0;
            }
            else{
                fraction = (lost_interval << 8) / expected_interval;
            }

            RTCP_Packet_ReportBlock rr = new RTCP_Packet_ReportBlock(this.SSRC.SSRC);
            rr.FractionLost            = (uint)fraction;
            rr.CumulativePacketsLost   = (uint)this.PacketsLost;
            rr.ExtendedHighestSeqNo    = extHighestSeqNo;
            rr.Jitter                  = (uint)m_Jitter;
            rr.LastSR                  = (m_pLastSR == null ? 0 : ((uint)((long)m_pLastSR.NtpTimestamp >> 8) & 0xFFFF));
            rr.DelaySinceLastSR        = (uint)Math.Max(0,this.DelaySinceLastSR / 65.536);

            return rr;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets stream owner RTP session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Session Session
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSession; 
            }
        }

        /// <summary>
        /// Gets stream owner synchronization source.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Source SSRC
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSSRC; 
            }
        }

        /// <summary>
        /// Gets remote participant who is owner of this stream. Returns null if this stream is not yet received RTCP SDES.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Participant_Remote Participant
        {            
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pParticipant; 
            }
        }

        /// <summary>
        /// Gets number of times <b>SeqNo</b> has wrapped around.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int SeqNoWrapCount
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SeqNoWrapCount; 
            }
        }

        /// <summary>
        /// Gets first sequence number what this stream got.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int FirstSeqNo
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (int)m_BaseSeq;
            }
        }

        /// <summary>
        /// Gets maximum sequnce number that stream has got.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int MaxSeqNo
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MaxSeqNo; 
            }
        }

        /// <summary>
        /// Gets how many RTP packets has received by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long PacketsReceived
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_PacketsReceived; 
            }
        }

        /// <summary>
        /// Gets how many RTP misorder packets has received by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long PacketsMisorder
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_PacketsMisorder; 
            }
        }

        /// <summary>
        /// Gets how many RTP packets has lost during transmission.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long PacketsLost
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                // RFC 3550 A.3 Determining Number of Packets Expected and Lost.                
                uint extHighestSeqNo = (uint)((65536 * m_SeqNoWrapCount) + m_MaxSeqNo);
                uint expected        = extHighestSeqNo - m_BaseSeq + 1;
                long lost            = expected - m_PacketsReceived;

                return lost;
            }
        }

        /// <summary>
        /// Gets how many RTP data has received by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long BytesReceived
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_BytesReceived; 
            }
        }

        /// <summary>
        /// Gets inter arrival jitter.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public double Jitter
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_Jitter; 
            }
        }

        /// <summary>
        /// Gets delay between las SR(sender report) and now in milliseconds. Returns -1 if no SR received.
        /// </summary>
        public int DelaySinceLastSR
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
               
                return (int)(m_LastSRTime == DateTime.MinValue ? -1 : ((TimeSpan)(DateTime.Now - m_LastSRTime)).TotalMilliseconds); 
            }
        }

        /// <summary>
        /// Gets time when last SR(sender report) was received. Returns <b>DateTime.MinValue</b> if no SR received.
        /// </summary>
        public DateTime LastSRTime
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastSRTime; 
            }
        }

        /// <summary>
        /// Gets last received RTCP SR(sender report). Value null means no  SR received.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTCP_Report_Sender LastSR
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pLastSR; 
            }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when stream is closed by remote party (remote party sent BYE).
        /// </summary>
        public event EventHandler Closed = null;

        #region method OnClosed

        /// <summary>
        /// Raises <b>Closed</b> event.
        /// </summary>
        private void OnClosed()
        {
            if(this.Closed != null){
                this.Closed(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// Is raised when receive stream has timed out by RTP session.
        /// </summary>
        /// <remarks>After <b>Timeout</b> event stream will be disposed and has no longer accessible.</remarks>
        public event EventHandler Timeout = null;

        #region method OnTimeout

        /// <summary>
        /// Raised <b>Timeout</b> event.
        /// </summary>
        internal void OnTimeout()
        {
            if(this.Timeout != null){
                this.Timeout(this,new EventArgs());
            }
        }

        #endregion
                
        /// <summary>
        /// Is raised when steam gets new sender report from remote party.
        /// </summary>
        public event EventHandler SenderReport = null;
        
        #region method OnSenderReport

        /// <summary>
        /// Raises <b>SenderReport</b> event.
        /// </summary>
        private void OnSenderReport()
        {
            if(this.SenderReport != null){
                this.SenderReport(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// Is raised when new RTP packet received.
        /// </summary>
        public event EventHandler<RTP_PacketEventArgs> PacketReceived = null;

        #region method OnPacketReceived

        /// <summary>
        /// Raises <b>PacketReceived</b> event.
        /// </summary>
        /// <param name="packet">RTP packet.</param>
        private void OnPacketReceived(RTP_Packet packet)
        {
            if(this.PacketReceived != null){
                this.PacketReceived(this,new RTP_PacketEventArgs(packet));
            }
        }

        #endregion

        #endregion
    }
}
