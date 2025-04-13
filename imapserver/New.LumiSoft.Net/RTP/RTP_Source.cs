using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.RTP
{    
    /// <summary>
    /// This class represents RTP source.
    /// </summary>
    /// <remarks>Source indicates an entity sending packets, either RTP and/or RTCP.
    /// Sources what send RTP packets are called "active", only RTCP sending ones are "passive".
    /// Source can be local(we send RTP and/or RTCP remote party) or remote(remote party sends RTP and/or RTCP to us).
    /// </remarks>
    public abstract class RTP_Source
    {        
        private RTP_SourceState      m_State          = RTP_SourceState.Passive;
        private RTP_Session          m_pSession       = null;
        private uint                 m_SSRC           = 0;
        private IPEndPoint           m_pRtcpEP        = null;
        private IPEndPoint           m_pRtpEP         = null;
        private DateTime             m_LastRtcpPacket = DateTime.MinValue;
        private DateTime             m_LastRtpPacket  = DateTime.MinValue;
        private DateTime             m_LastActivity   = DateTime.Now;
        private DateTime             m_LastRRTime     = DateTime.MinValue;
        private string               m_CloseReason    = null;
        private object               m_pTag           = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner RTP session.</param>
        /// <param name="ssrc">Synchronization source ID.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null reference.</exception>
        internal RTP_Source(RTP_Session session,uint ssrc)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }

            m_pSession = session;
            m_SSRC     = ssrc;
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        internal virtual void Dispose()
        {   
            if(m_State == RTP_SourceState.Disposed){
                return;
            }
            OnDisposing();
            SetState(RTP_SourceState.Disposed);

            m_pSession = null;
            m_pRtcpEP = null;
            m_pRtpEP = null;

            this.Closed = null;
            this.Disposing = null;
            this.StateChanged = null;
        }

        #endregion


        #region method Close

        /// <summary>
        /// Closes specified source.
        /// </summary>
        /// <param name="closeReason">Closing reason. Value null means not specified.</param>
        internal virtual void Close(string closeReason)
        {
            m_CloseReason = closeReason;

            OnClosed();
            Dispose();
        }

        #endregion

        #region method SetRtcpEP

        /// <summary>
        /// Sets property <b>RtcpEP</b> value.
        /// </summary>
        /// <param name="ep">IP end point.</param>
        internal void SetRtcpEP(IPEndPoint ep)
        {
            m_pRtcpEP = ep;
        }

        #endregion

        #region method SetRtpEP

        /// <summary>
        /// Sets property <b>RtpEP</b> value.
        /// </summary>
        /// <param name="ep">IP end point.</param>
        internal void SetRtpEP(IPEndPoint ep)
        {
            m_pRtpEP = ep;
        }

        #endregion

        #region method SetActivePassive

        /// <summary>
        /// Sets source active/passive state.
        /// </summary>
        /// <param name="active">If true, source switches to active, otherwise to passive.</param>
        internal void SetActivePassive(bool active)
        {            
            if(active){

            }
            else{
            }

            // TODO:
        }

        #endregion

        #region method SetLastRtcpPacket

        /// <summary>
        /// Sets <b>LastRtcpPacket</b> property value.
        /// </summary>
        /// <param name="time">Time.</param>
        internal void SetLastRtcpPacket(DateTime time)
        {
            m_LastRtcpPacket = time;
            m_LastActivity = time;
        }

        #endregion

        #region method SetLastRtpPacket

        /// <summary>
        /// Sets <b>LastRtpPacket</b> property value.
        /// </summary>
        /// <param name="time">Time.</param>
        internal void SetLastRtpPacket(DateTime time)
        {
            m_LastRtpPacket = time;
            m_LastActivity = time;
        }

        #endregion

        #region method SetRR

        /// <summary>
        /// Sets property LastRR value.
        /// </summary>
        /// <param name="rr">RTCP RR report.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>rr</b> is null reference.</exception>
        internal void SetRR(RTCP_Packet_ReportBlock rr)
        {
            if(rr == null){
                throw new ArgumentNullException("rr");
            }

        }

        #endregion


        #region method GenerateNewSSRC

        /// <summary>
        /// Generates new SSRC value. This must be called only if SSRC collision of local source.
        /// </summary>
        internal void GenerateNewSSRC()
        {
            m_SSRC = RTP_Utils.GenerateSSRC();
        }

        #endregion


        #region method SetState

        /// <summary>
        /// Sets source state.
        /// </summary>
        /// <param name="state">New source state.</param>
        protected void SetState(RTP_SourceState state)
        {
            if(m_State == RTP_SourceState.Disposed){
                return;
            }

            if(m_State != state){
                m_State = state;

                OnStateChaged();
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets source state.
        /// </summary>
        public RTP_SourceState State
        {
            get{ return m_State; }
        }

        /// <summary>
        /// Gets owner RTP session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Session Session
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSession; 
            }
        }

        /// <summary>
        /// Gets synchronization source ID.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public uint SSRC
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SSRC; 
            }
        }

        /// <summary>
        /// Gets source RTCP end point. Value null means source haven't sent any RTCP packet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public IPEndPoint RtcpEP
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRtcpEP; 
            }
        }

        /// <summary>
        /// Gets source RTP end point. Value null means source haven't sent any RTCP packet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public IPEndPoint RtpEP
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRtpEP; 
            }
        }

        /// <summary>
        /// Gets if source is local or remote source.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public abstract bool IsLocal
        {
            get;
        }

        /// <summary>
        /// Gets last time when source sent RTP or RCTP packet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public DateTime LastActivity
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastActivity; 
            }
        }

        /// <summary>
        /// Gets last time when source sent RTCP packet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public DateTime LastRtcpPacket
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastRtcpPacket; 
            }
        }

        /// <summary>
        /// Gets last time when source sent RTP packet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public DateTime LastRtpPacket
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastRtpPacket; 
            }
        }

        /// <summary>
        /// Gets last time when source sent RTCP RR report.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public DateTime LastRRTime
        {
            get{
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastRRTime; 
            }
        }

        /// <summary>
        /// Gets source closing reason. Value null means not specified.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public string CloseReason
        {
            get{ 
                if(m_State == RTP_SourceState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_CloseReason; 
            }
        }

        /// <summary>
        /// Gets or sets user data.
        /// </summary>
        public object Tag
        {
            get{ return m_pTag; }

            set{ m_pTag = value; }
        }


        /// <summary>
        /// Gets source CNAME. Value null means that source not binded to participant.
        /// </summary>
        internal abstract string CName
        {
            get;
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when source is closed (by BYE).
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
        /// Is raised when source is disposing.
        /// </summary>
        public event EventHandler Disposing = null;

        #region method OnDisposing

        /// <summary>
        /// Raises <b>Disposing</b> event.
        /// </summary>
        private void OnDisposing()
        {
            if(this.Disposing != null){
                this.Disposing(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// Is raised when source state has changed.
        /// </summary>
        public event EventHandler StateChanged = null;

        #region method OnStateChaged

        /// <summary>
        /// Raises <b>StateChanged</b> event.
        /// </summary>
        private void OnStateChaged()
        {
            if(this.StateChanged != null){
                this.StateChanged(this,new EventArgs());
            }
        }

        #endregion

        #endregion

    }
}
