using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

using LumiSoft.Net.RTP;
using LumiSoft.Net.Media.Codec.Audio;

namespace LumiSoft.Net.Media
{
    /// <summary>
    /// This class implements audio-in (eg. microphone,line-in device) device RTP audio sending.
    /// </summary>
    public class AudioIn_RTP : IDisposable
    {
        private bool                       m_IsDisposed     = false;
        private bool                       m_IsRunning      = false;
        private AudioInDevice              m_pAudioInDevice = null;
        private int                        m_AudioFrameSize = 20;
        private Dictionary<int,AudioCodec> m_pAudioCodecs   = null;
        private RTP_SendStream             m_pRTP_Stream    = null;
        private AudioCodec                 m_pActiveCodec   = null;
        private _WaveIn                    m_pWaveIn        = null;
        private uint                       m_RtpTimeStamp   = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="audioInDevice">Audio-in device to capture.</param>
        /// <param name="audioFrameSize">Audio frame size in milliseconds.</param>
        /// <param name="codecs">Audio codecs with RTP payload number. For example: 0-PCMU,8-PCMA.</param>
        /// <param name="stream">RTP stream to use for audio sending.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>audioInDevice</b>,<b>codecs</b> or <b>stream</b> is null reference.</exception>
        public AudioIn_RTP(AudioInDevice audioInDevice,int audioFrameSize,Dictionary<int,AudioCodec> codecs,RTP_SendStream stream)
        {
            if(audioInDevice == null){
                throw new ArgumentNullException("audioInDevice");
            }
            if(codecs == null){
                throw new ArgumentNullException("codecs");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_pAudioInDevice = audioInDevice;
            m_AudioFrameSize = audioFrameSize;
            m_pAudioCodecs   = codecs;
            m_pRTP_Stream    = stream;

            m_pRTP_Stream.Session.PayloadChanged += new EventHandler(m_pRTP_Stream_PayloadChanged);
            m_pAudioCodecs.TryGetValue(m_pRTP_Stream.Session.Payload,out m_pActiveCodec);
        }
                
        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            if(m_IsDisposed){
                return;
            }

            Stop();

            m_IsDisposed = true;

            this.Error        = null;
            m_pAudioInDevice  = null;
            m_pAudioCodecs    = null;
            m_pRTP_Stream.Session.PayloadChanged -= new EventHandler(m_pRTP_Stream_PayloadChanged);
            m_pRTP_Stream     = null;
            m_pActiveCodec    = null;
        }

        #endregion


        #region Events handling

        #region method m_pRTP_Stream_PayloadChanged

        /// <summary>
        /// Is called when RTP session sending payload has changed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pRTP_Stream_PayloadChanged(object sender,EventArgs e)
        {
            if(m_IsRunning){
                Stop();

                m_pActiveCodec = null;
                m_pAudioCodecs.TryGetValue(m_pRTP_Stream.Session.Payload,out m_pActiveCodec);

                Start();
            }
        }

        #endregion

        #region method m_pWaveIn_AudioFrameReceived

        /// <summary>
        /// Is called when wave-in has received new audio frame.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pWaveIn_AudioFrameReceived(object sender,EventArgs<byte[]> e)
        {
            try{                
                // We don't have RTP timestamp base or time stamp recycled.
                if(m_RtpTimeStamp == 0 || m_RtpTimeStamp > m_pRTP_Stream.Session.RtpClock.RtpTimestamp){
                    m_RtpTimeStamp = m_pRTP_Stream.Session.RtpClock.RtpTimestamp;
                }
                // Some sample block missing or silence suppression.
                // Don't work ... need some more investigation.
                //else if((m_pRTP_Stream.Session.RtpClock.RtpTimestamp - m_RtpTimeStamp) > 2 * m_pRTP_Stream.Session.RtpClock.MillisecondsToRtpTicks(m_AudioFrameSize)){
                //    m_RtpTimeStamp = m_pRTP_Stream.Session.RtpClock.RtpTimestamp;
                //}
                else{
                    m_RtpTimeStamp += (uint)m_pRTP_Stream.Session.RtpClock.MillisecondsToRtpTicks(m_AudioFrameSize);
                }

                if(m_pActiveCodec != null){
                    RTP_Packet rtpPacket = new RTP_Packet();
                    rtpPacket.Data = m_pActiveCodec.Encode(e.Value,0,e.Value.Length);
                    rtpPacket.Timestamp = m_RtpTimeStamp;
 	        
                    m_pRTP_Stream.Send(rtpPacket);
                }
            }
            catch(Exception x){
                if(!this.IsDisposed){
                    // Raise error event(We can't throw expection directly, we are on threadpool thread).
                    OnError(x);
                }
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts capturing from audio-in device and sending it to RTP stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void Start()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsRunning){
                return;
            }

            m_IsRunning = true;

            if(m_pActiveCodec != null){
                // Calculate buffer size.
                int bufferSize = (m_pActiveCodec.AudioFormat.SamplesPerSecond / (1000 / m_AudioFrameSize)) * (m_pActiveCodec.AudioFormat.BitsPerSample / 8);

                m_pWaveIn = new _WaveIn(m_pAudioInDevice,m_pActiveCodec.AudioFormat.SamplesPerSecond,m_pActiveCodec.AudioFormat.BitsPerSample,1,bufferSize);
                m_pWaveIn.AudioFrameReceived += new EventHandler<EventArgs<byte[]>>(m_pWaveIn_AudioFrameReceived);
                m_pWaveIn.Start();
            }
        }
                
        #endregion

        #region method Stop

        /// <summary>
        /// Stops capturing from audio-in device and sending it to RTP stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void Stop()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!m_IsRunning){
                return;
            }

            if(m_pWaveIn != null){
                m_pWaveIn.Dispose();
                m_pWaveIn = null;
            }
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
        /// Gets if currently audio is sent.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public bool IsRunning
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_IsRunning; 
            }
        }

        /// <summary>
        /// Gets audio-in device is used to capture sound.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null reference is passed.</exception>
        public AudioInDevice AudioInDevice
        {
            get{   
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pAudioInDevice; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value == null){
                    throw new ArgumentNullException("AudioInDevice");
                }

                m_pAudioInDevice = value;

                if(this.IsRunning){
                    Stop();
                    Start();
                }
            }
        }

        // TODO:
        // public int Volume ?

        /// <summary>
        /// Gets RTP stream used for audio sending.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public RTP_SendStream RTP_Stream
        {
            get{  
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pRTP_Stream; 
            }
        }

        /// <summary>
        /// Gets current audio codec what is used for sending.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public AudioCodec AudioCodec
        {
            get{  
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pActiveCodec; 
            }
        }

        /// <summary>
        /// Gets or sets audio codecs.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        public Dictionary<int,AudioCodec> AudioCodecs
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pAudioCodecs; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value == null){
                    throw new ArgumentNullException("AudioCodecs");
                }

                m_pAudioCodecs = value;
            }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This method is raised when asynchronous thread Exception happens.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> Error = null;

        #region method OnError

        /// <summary>
        /// Raises <b>Error</b> event.
        /// </summary>
        /// <param name="x">Error what happened.</param>
        private void OnError(Exception x)
        {
            if(this.Error != null){
                this.Error(this,new ExceptionEventArgs(x));
            }
        }

        #endregion

        #endregion
    }
}
