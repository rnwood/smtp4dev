using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// Implements RTP session send stream.
    /// </summary>
    public class RTP_SendStream
    {
        private bool             m_IsDisposed             = false;
        private RTP_Source_Local m_pSource                = null;
        private int              m_SeqNoWrapCount         = 0;
        private int              m_SeqNo                  = 0;
        private DateTime         m_LastPacketTime;
        private uint             m_LastPacketRtpTimestamp = 0;
        private long             m_RtpPacketsSent         = 0;
        private long             m_RtpBytesSent           = 0;
        private long             m_RtpDataBytesSent       = 0;
        private int              m_RtcpCyclesSinceWeSent  = 9999;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="source">Owner RTP source.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>source</b> is null reference.</exception>
        internal RTP_SendStream(RTP_Source_Local source)
        {
            if(source == null){
                throw new ArgumentNullException("source");
            }

            m_pSource = source;

            /* RFC 3550 4.
                The initial value of the sequence number SHOULD be random (unpredictable) to make known-plaintext 
                attacks on encryption more difficult.
            */
            m_SeqNo = new Random().Next(1,32000);
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        private void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            m_pSource = null;
                        
            OnDisposed();

            this.Disposed = null;
            this.Closed = null;
        }

        #endregion


        #region method Close

        /// <summary>
        /// Closes this sending stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void Close()
        {
            Close(null);
        }

        /// <summary>
        /// Closes this sending stream.
        /// </summary>
        /// <param name="closeReason">Stream closing reason text what is reported to the remote party. Value null means not specified.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void Close(string closeReason)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            
            m_pSource.Close(closeReason);
                        
            OnClosed();
            Dispose();
        }

        #endregion


        #region method Send

        /// <summary>
        /// Sends specified packet to the RTP session remote party.
        /// </summary>
        /// <param name="packet">RTP packet.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>packet</b> is null reference.</exception>
        /// <remarks>Properties <b>packet.SSRC</b>,<b>packet.SeqNo</b>,<b>packet.PayloadType</b> filled by this method automatically.</remarks>
        public void Send(RTP_Packet packet)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(packet == null){
                throw new ArgumentNullException("packet");
            }
            if(this.Session.StreamMode == RTP_StreamMode.Inactive || this.Session.StreamMode == RTP_StreamMode.Receive){
                return;
            }

            // RTP was designed around the concept of Application Level Framing (ALF), 
            // because of it we only allow to send packets and don't deal with breaking frames into packets.

            packet.SSRC  = this.Source.SSRC;
            packet.SeqNo = NextSeqNo();
            packet.PayloadType = this.Session.Payload;
            
            // Send RTP packet.
            m_RtpBytesSent += m_pSource.SendRtpPacket(packet);

            m_RtpPacketsSent++;
            m_RtpDataBytesSent += packet.Data.Length;
            m_LastPacketTime = DateTime.Now;
            m_LastPacketRtpTimestamp = packet.Timestamp;
            m_RtcpCyclesSinceWeSent = 0;
        }

        #endregion


        #region method RtcpCycle

        /// <summary>
        /// Is called by RTP session if RTCP cycle compled.
        /// </summary>
        internal void RtcpCycle()
        {
            m_RtcpCyclesSinceWeSent++;
        }

        #endregion


        #region mehtod NextSeqNo

        /// <summary>
        /// Gets next packet sequence number.
        /// </summary>
        /// <returns>Returns next packet sequence number.</returns>
        private ushort NextSeqNo()
        {
            // Wrap around sequence number.
            if(m_SeqNo >= ushort.MaxValue){
                m_SeqNo = 0;
                m_SeqNoWrapCount++;
            }
            
            return (ushort)m_SeqNo++;
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

                return m_pSource.Session; 
            }
        }

        /// <summary>
        /// Gets stream owner source.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Source Source
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSource; 
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
        /// Gets next packet sequence number.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int SeqNo
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SeqNo; 
            }
        }

        /// <summary>
        /// Gets last packet send time.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public DateTime LastPacketTime
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastPacketTime;
            }
        }

        /// <summary>
        /// Gets last sent RTP packet RTP timestamp header value.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public uint LastPacketRtpTimestamp
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LastPacketRtpTimestamp; 
            }
        }

        /// <summary>
        /// Gets how many RTP packets has sent by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtpPacketsSent
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtpPacketsSent; 
            }
        }

        /// <summary>
        /// Gets how many RTP bytes has sent by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtpBytesSent
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtpBytesSent; 
            }
        }

        /// <summary>
        /// Gets how many RTP data(no RTP header included) bytes has sent by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtpDataBytesSent
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtpDataBytesSent; 
            }
        }

     
        /// <summary>
        /// Gets how many RTCP cycles has passed since we sent data.
        /// </summary>
        internal int RtcpCyclesSinceWeSent
        {
            get{ return m_RtcpCyclesSinceWeSent; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when stream has disposed.
        /// </summary>
        public event EventHandler Disposed = null;

        #region method OnDisposed

        /// <summary>
        /// Raises <b>Disposed</b> event.
        /// </summary>
        private void OnDisposed()
        {
            if(this.Disposed != null){
                this.Disposed(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// Is raised when stream is closed.
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

        #endregion

    }
}
