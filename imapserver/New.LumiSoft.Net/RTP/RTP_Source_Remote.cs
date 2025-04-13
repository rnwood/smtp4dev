using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class represents RTP remote source what we receive.
    /// </summary>
    /// <remarks>Source indicates an entity sending packets, either RTP and/or RTCP.
    /// Sources what send RTP packets are called "active", only RTCP sending ones are "passive".
    /// </remarks>
    public class RTP_Source_Remote : RTP_Source
    {
        private RTP_Participant_Remote m_pParticipant = null;
        private RTP_ReceiveStream      m_pStream      = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner RTP session.</param>
        /// <param name="ssrc">Synchronization source ID.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null reference.</exception>
        internal RTP_Source_Remote(RTP_Session session,uint ssrc) : base(session,ssrc)
        {
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        internal override void Dispose()
        {
            m_pParticipant = null;
            if(m_pStream != null){
                m_pStream.Dispose();
            }

            this.ApplicationPacket = null;

            base.Dispose();
        }

        #endregion


        #region method SetParticipant

        /// <summary>
        /// Sets source owner participant.
        /// </summary>
        /// <param name="participant">RTP participant.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>participant</b> is null reference.</exception>
        internal void SetParticipant(RTP_Participant_Remote participant)
        {
            if(participant == null){
                throw new ArgumentNullException("participant");
            }

            m_pParticipant = participant;
        }

        #endregion

        #region method OnRtpPacketReceived

        /// <summary>
        /// Is called when RTP session receives new RTP packet.
        /// </summary>
        /// <param name="packet">RTP packet.</param>
        /// <param name="size">Packet size in bytes.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>packet</b> is null reference.</exception>
        internal void OnRtpPacketReceived(RTP_Packet packet,int size)
        {
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            SetLastRtpPacket(DateTime.Now);

            // Passive source and first RTP packet.
            if(m_pStream == null){
                m_pStream = new RTP_ReceiveStream(this.Session,this,packet.SeqNo);

                SetState(RTP_SourceState.Active);
            }

            m_pStream.Process(packet,size);
        }

        #endregion

        #region method OnSenderReport

        /// <summary>
        /// This method is called when this source got sender report.
        /// </summary>
        /// <param name="report">Sender report.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>report</b> is null reference.</exception>
        internal void OnSenderReport(RTCP_Report_Sender report)
        {
            if(report == null){
                throw new ArgumentNullException("report");
            }
            
            if(m_pStream != null){
                m_pStream.SetSR(report);
            }
        }

        #endregion

        #region method OnAppPacket

        /// <summary>
        /// This method is called when this source got RTCP APP apcket.
        /// </summary>
        /// <param name="packet">RTCP APP packet.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>packet</b> is null reference.</exception>
        internal void OnAppPacket(RTCP_Packet_APP packet)
        {
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            OnApplicationPacket(packet);
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Returns false.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public override bool IsLocal
        {
            get{ 
                if(this.State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return false; 
            }
        }
        
        /// <summary>
        /// Gets remote participant. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Participant_Remote Participant
        {
            get{
                if(this.State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pParticipant; 
            }
        }

        /// <summary>
        /// Gets the stream we receive. Value null means that source is passive and doesn't send any RTP data.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_ReceiveStream Stream
        {
            get{ 
                if(this.State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pStream; 
            }
        }


        /// <summary>
        /// Gets source CNAME. Value null means that source not binded to participant.
        /// </summary>
        internal override string CName
        {
            get{
                if(this.Participant != null){
                    return null;
                }
                else{
                    return this.Participant.CNAME;
                }
            }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when source sends RTCP APP packet.
        /// </summary>
        public event EventHandler<EventArgs<RTCP_Packet_APP>> ApplicationPacket = null;

        #region method OnApplicationPacket

        /// <summary>
        /// Raises <b>ApplicationPacket</b> event.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when <b>packet</b> is null reference.</exception>
        private void OnApplicationPacket(RTCP_Packet_APP packet)
        {
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            if(this.ApplicationPacket != null){
                this.ApplicationPacket(this,new EventArgs<RTCP_Packet_APP>(packet));
            }
        }

        #endregion

        #endregion
    }
}
