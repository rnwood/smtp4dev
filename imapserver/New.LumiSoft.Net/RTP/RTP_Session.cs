using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using LumiSoft.Net.UDP;
using LumiSoft.Net.Media.Codec;
using LumiSoft.Net.STUN.Client;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// Implements RTP session. Defined in RFC 3550.
    /// </summary>
    /// <remarks>RTP session can exchange 1 payload type at time.
    /// For example if application wants to send audio and video, it must create two different RTP sessions.
    /// Though RTP session can send multiple streams of same payload.</remarks>
    public class RTP_Session : IDisposable
    {
        private object                             m_pLock                      = new object();
        private bool                               m_IsDisposed                 = false;
        private bool                               m_IsStarted                  = false;
        private RTP_MultimediaSession              m_pSession                   = null;
        private RTP_Address                        m_pLocalEP                   = null;
        private RTP_Clock                          m_pRtpClock                  = null;
        private RTP_StreamMode                     m_StreamMode                 = RTP_StreamMode.SendReceive;
        private List<RTP_Address>                  m_pTargets                   = null;
        private int                                m_Payload                    = 0;
        private int                                m_Bandwidth                  = 64000;
        private List<RTP_Source_Local>             m_pLocalSources              = null;
        private RTP_Source                         m_pRtcpSource                = null;
        private Dictionary<uint,RTP_Source>        m_pMembers                   = null;
        private int                                m_PMembersCount              = 0;
        private Dictionary<uint,RTP_Source>        m_pSenders                   = null;
        private Dictionary<string,DateTime>        m_pConflictingEPs            = null;
        private List<UDP_DataReceiver>             m_pUdpDataReceivers          = null;
        private Socket                             m_pRtpSocket                 = null;
        private Socket                             m_pRtcpSocket                = null;
        private long                               m_RtpPacketsSent             = 0;
        private long                               m_RtpBytesSent               = 0;
        private long                               m_RtpPacketsReceived         = 0;
        private long                               m_RtpBytesReceived           = 0;
        private long                               m_RtpFailedTransmissions     = 0;
        private long                               m_RtcpPacketsSent            = 0;
        private long                               m_RtcpBytesSent              = 0;
        private long                               m_RtcpPacketsReceived        = 0;
        private long                               m_RtcpBytesReceived          = 0;
        private double                             m_RtcpAvgPacketSize          = 0;
        private long                               m_RtcpFailedTransmissions    = 0;
        private long                               m_RtcpUnknownPacketsReceived = 0;
        private DateTime                           m_RtcpLastTransmission       = DateTime.MinValue;
        private long                               m_LocalCollisions            = 0;
        private long                               m_RemoteCollisions           = 0;
        private long                               m_LocalPacketsLooped         = 0;
        private long                               m_RemotePacketsLooped        = 0;
        private int                                m_MTU                        = 1400;
        private TimerEx                            m_pRtcpTimer                 = null;        
        private KeyValueCollection<int,Codec>      m_pPayloads                  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner RTP multimedia session.</param>
        /// <param name="localEP">Local RTP end point.</param>
        /// <param name="clock">RTP media clock.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>localEP</b>, <b>localEP</b> or <b>clock</b> is null reference.</exception>
        internal RTP_Session(RTP_MultimediaSession session,RTP_Address localEP,RTP_Clock clock)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }
            if(localEP == null){
                throw new ArgumentNullException("localEP");
            }
            if(clock == null){
                throw new ArgumentNullException("clock");
            }

            m_pSession  = session;
            m_pLocalEP  = localEP;
            m_pRtpClock = clock;

            m_pLocalSources = new List<RTP_Source_Local>();
            m_pTargets = new List<RTP_Address>();
            m_pMembers = new Dictionary<uint,RTP_Source>();
            m_pSenders = new Dictionary<uint,RTP_Source>();
            m_pConflictingEPs = new Dictionary<string,DateTime>();
            m_pPayloads = new KeyValueCollection<int,Codec>();
            
            m_pUdpDataReceivers = new List<UDP_DataReceiver>();
            m_pRtpSocket = new Socket(localEP.IP.AddressFamily,SocketType.Dgram,ProtocolType.Udp);
            m_pRtpSocket.Bind(localEP.RtpEP);
            m_pRtcpSocket = new Socket(localEP.IP.AddressFamily,SocketType.Dgram,ProtocolType.Udp);
            m_pRtcpSocket.Bind(localEP.RtcpEP);
                        
            m_pRtcpTimer = new TimerEx();
            m_pRtcpTimer.Elapsed += new System.Timers.ElapsedEventHandler(delegate(object sender,System.Timers.ElapsedEventArgs e){
                SendRtcp();
            });
            m_pRtcpTimer.AutoReset = false;
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
            m_IsDisposed = true;

            foreach(UDP_DataReceiver receiver in m_pUdpDataReceivers){
                receiver.Dispose();
            }
            m_pUdpDataReceivers = null;
            if(m_pRtcpTimer != null){
                m_pRtcpTimer.Dispose();
                m_pRtcpTimer = null;
            }
            m_pSession = null;
            m_pLocalEP = null;
            m_pTargets = null;
            foreach(RTP_Source_Local source in m_pLocalSources.ToArray()){
                source.Dispose();
            }
            m_pLocalSources = null;
            m_pRtcpSource = null;
            foreach(RTP_Source source in m_pMembers.Values){
                source.Dispose();
            }
            m_pMembers = null;
            m_pSenders = null;
            m_pConflictingEPs = null;
            m_pRtpSocket.Close();
            m_pRtpSocket = null;
            m_pRtcpSocket.Close();
            m_pRtcpSocket = null;
            m_pUdpDataReceivers = null;

            OnDisposed();

            this.Disposed = null;
            this.Closed = null;
            this.NewSendStream = null;
            this.NewReceiveStream = null;
        }

        #endregion


        #region method Close

        /// <summary>
        /// Closes RTP session, sends BYE with optional reason text to remote targets.
        /// </summary>
        /// <param name="closeReason">Close reason. Value null means not specified.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void Close(string closeReason)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            // Generate BYE packet(s).
            RTCP_CompoundPacket compundPacket = new RTCP_CompoundPacket();
            RTCP_Packet_RR rr = new RTCP_Packet_RR();
            rr.SSRC = m_pRtcpSource.SSRC;
            compundPacket.Packets.Add(rr);
            int sourcesProcessed = 0;
            while(sourcesProcessed < m_pLocalSources.Count){
                uint[] sources = new uint[Math.Min(m_pLocalSources.Count - sourcesProcessed,31)];
                for(int i=0;i<sources.Length;i++){
                    sources[i] = m_pLocalSources[sourcesProcessed].SSRC;
                    sourcesProcessed++;
                }

                RTCP_Packet_BYE bye = new RTCP_Packet_BYE();
                bye.Sources = sources;
                compundPacket.Packets.Add(bye);
            }

            // Send BYE.
            SendRtcpPacket(compundPacket);

            OnClosed();
            Dispose();
        }

        #endregion

        #region method Start

        /// <summary>
        /// Starts RTP session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void Start()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsStarted){
                return;
            }
            m_IsStarted = true;

            /* RFC 3550 6.3.2 Initialization
                Upon joining the session, the participant initializes tp to 0, tc to
                0, senders to 0, pmembers to 1, members to 1, we_sent to false,
                rtcp_bw to the specified fraction of the session bandwidth, initial
                to true, and avg_rtcp_size to the probable size of the first RTCP
                packet that the application will later construct.  The calculated
                interval T is then computed, and the first packet is scheduled for
                time tn = T.  This means that a transmission timer is set which
                expires at time T.  Note that an application MAY use any desired
                approach for implementing this timer.
    
                The participant adds its own SSRC to the member table.
            */

            m_PMembersCount = 1;
            m_RtcpAvgPacketSize = 100;

            // Add ourself to members list.
            m_pRtcpSource = CreateLocalSource();
            m_pMembers.Add(m_pRtcpSource.SSRC,m_pRtcpSource);

            // Create RTP data receiver.
            UDP_DataReceiver rtpDataReceiver = new UDP_DataReceiver(m_pRtpSocket);
            rtpDataReceiver.PacketReceived += delegate(object s1,UDP_e_PacketReceived e1){
                ProcessRtp(e1.Buffer,e1.Count,e1.RemoteEP);
            };
            // rtpDataReceiver.Error // We don't care about receiving errors here.
            m_pUdpDataReceivers.Add(rtpDataReceiver);
            rtpDataReceiver.Start();
            // Create RTCP data receiver.
            UDP_DataReceiver rtcpDataReceiver = new UDP_DataReceiver(m_pRtcpSocket);
            rtcpDataReceiver.PacketReceived += delegate(object s1,UDP_e_PacketReceived e1){
                ProcessRtcp(e1.Buffer,e1.Count,e1.RemoteEP);
            };
            // rtcpDataReceiver.Error // We don't care about receiving errors here.
            m_pUdpDataReceivers.Add(rtcpDataReceiver);
            rtcpDataReceiver.Start();           
                   
            // Start RTCP reporting.
            Schedule(ComputeRtcpTransmissionInterval(m_pMembers.Count,m_pSenders.Count,m_Bandwidth * 0.25,false,m_RtcpAvgPacketSize,true));
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stops RTP session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void Stop()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            // TODO:

            throw new NotImplementedException();
        }

        #endregion


        #region method CreateSendStream

        /// <summary>
        /// Creates new send stream.
        /// </summary>
        /// <returns>Returns new created send stream.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public RTP_SendStream CreateSendStream()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            RTP_Source_Local source = CreateLocalSource();
            source.CreateStream();

            OnNewSendStream(source.Stream);

            return source.Stream;
        }

        #endregion

        #region method AddTarget

        /// <summary>
        /// Opens RTP session to the specified remote target.
        /// </summary>
        /// <remarks>Once RTP session opened, RTCP reports sent to that target and also each local sending stream data.</remarks>
        /// <param name="target">Session remote target.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>target</b> is null reference.</exception>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid values.</exception>
        public void AddTarget(RTP_Address target)
        {
            if(target == null){
                throw new ArgumentNullException("target");
            }
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_pLocalEP.Equals(target)){
                throw new ArgumentException("Argument 'target' value collapses with property 'LocalEP'.","target");
            }

            foreach(RTP_Address t in this.Targets){
                if(t.Equals(target)){
                    throw new ArgumentException("Specified target already exists.","target");
                }
            }

            m_pTargets.Add(target);
        }

        #endregion

        #region method RemoveTarget

        /// <summary>
        /// Removes specified target.
        /// </summary>
        /// <param name="target">Session remote target.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>target</b> is null reference.</exception>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void RemoveTarget(RTP_Address target)
        {
            if(target == null){
                throw new ArgumentNullException("target");
            }
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            m_pTargets.Remove(target);
        }

        #endregion

        #region method RemoveTargets

        /// <summary>
        /// Removes all targets.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void RemoveTargets()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            m_pTargets.Clear();
        }

        #endregion

        #region method StunPublicEndPoints

        /// <summary>
        /// Gets RTP and RTCP public end points.
        /// </summary>
        /// <param name="server">STUN server name.</param>
        /// <param name="port">STUN server port.</param>
        /// <param name="rtpEP">RTP public end point.</param>
        /// <param name="rtcpEP">RTCP public end point.</param>
        /// <returns>Returns true if public end points allocated, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>server</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when RTP session is in invalid state and this method is called.</exception>
        public bool StunPublicEndPoints(string server,int port,out IPEndPoint rtpEP,out IPEndPoint rtcpEP)
        {
            if(server == null){
                throw new ArgumentNullException("server");
            }
            if(this.m_IsStarted){
                throw new InvalidOperationException("Method 'StunPublicEndPoints' may be called only if RTP session has not started.");
            }

            rtpEP  = null;
            rtcpEP = null;

            try{
                STUN_Result rtpResult = STUN_Client.Query(server,port,m_pRtpSocket);
                if(rtpResult.NetType == STUN_NetType.FullCone || rtpResult.NetType == STUN_NetType.PortRestrictedCone || rtpResult.NetType == STUN_NetType.RestrictedCone){                                        
                    rtpEP  = rtpResult.PublicEndPoint;
                    rtcpEP = STUN_Client.GetPublicEP(server,port,m_pRtcpSocket);
                                               
                    return true;
                }                
            }
            catch{
            }

            return false;
        }

        #endregion


        #region method SendRtcpPacket

        /// <summary>
        /// Sends specified RTCP packet to the session remote party.
        /// </summary>
        /// <param name="packet">RTCP compound packet.</param>
        /// <returns>Returns packet size in bytes.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>packet</b> is null reference.</exception>
        internal int SendRtcpPacket(RTCP_CompoundPacket packet)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(packet == null){
                throw new ArgumentNullException("packet");
            }
                        
            byte[] packetBytes = packet.ToByte();

            // Send packet to each remote target.
            foreach(RTP_Address target in this.Targets){
                try{
                    m_pRtcpSocket.SendTo(packetBytes,packetBytes.Length,SocketFlags.None,target.RtcpEP);

                    m_RtcpPacketsSent++;
                    m_RtcpBytesSent += packetBytes.Length;
                    // RFC requires IP header counted too, we just don't do it.
                    m_RtcpAvgPacketSize = (1/16) * packetBytes.Length + (15/16) * m_RtcpAvgPacketSize;
                }
                catch{
                    m_RtcpFailedTransmissions++;
                }
            }

            return packetBytes.Length;
        }

        #endregion

        #region method SendRtpPacket

        /// <summary>
        /// Sends specified RTP packet to the session remote party.
        /// </summary>
        /// <param name="stream">RTP packet sending stream.</param>
        /// <param name="packet">RTP packet.</param>
        /// <returns>Returns packet size in bytes.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> or <b>packet</b> is null reference.</exception>
        internal int SendRtpPacket(RTP_SendStream stream,RTP_Packet packet)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            // Check that we are in members table (because SSRC has timed out), add itself to senders table.
            lock(m_pMembers){
                if(!m_pMembers.ContainsKey(stream.Source.SSRC)){
                    m_pMembers.Add(stream.Source.SSRC,stream.Source);
                }
            }

            // If we are not in sender table (because SSRC has timed out), add itself to senders table.
            lock(m_pSenders){
                if(!m_pSenders.ContainsKey(stream.Source.SSRC)){
                    m_pSenders.Add(stream.Source.SSRC,stream.Source);
                }
            }
                        
            byte[] packetBytes = new byte[m_MTU];
            int count = 0;
            packet.ToByte(packetBytes,ref count);

            // Send packet to each remote target.
            foreach(RTP_Address target in this.Targets){
                try{
                    m_pRtpSocket.BeginSendTo(packetBytes,0,count,SocketFlags.None,target.RtpEP,this.RtpAsyncSocketSendCompleted,null);
                }
                catch{
                    m_RtpFailedTransmissions++;
                }
            }
                        
            return count;
        }

        #endregion


        #region method ProcessRtcp

        /// <summary>
        /// Processes specified RTCP data.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="count">Number of bytes in data buffer.</param>
        /// <param name="remoteEP">IP end point what sent RTCP packet.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> or <b>remoteEP</b> is null reference.</exception>
        private void ProcessRtcp(byte[] buffer,int count,IPEndPoint remoteEP)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(remoteEP == null){
                throw new ArgumentNullException("remoteEP");
            }
            
            /* RFC 3550 6.3.3 Receiving an RTP or Non-BYE RTCP Packet
                When an RTP or RTCP packet is received from a participant whose SSRC
                is not in the member table, the SSRC is added to the table, and the
                value for members is updated once the participant has been validated
                as described in Section 6.2.1.  The same processing occurs for each
                CSRC in a validated RTP packet.

                For each compound RTCP packet received, the value of avg_rtcp_size is
                updated:
                    avg_rtcp_size = (1/16) * packet_size + (15/16) * avg_rtcp_size

                where packet_size is the size of the RTCP packet just received.
              
               6.3.4 Receiving an RTCP BYE Packet
                Except as described in Section 6.3.7 for the case when an RTCP BYE is
                to be transmitted, if the received packet is an RTCP BYE packet, the
                SSRC is checked against the member table.  If present, the entry is
                removed from the table, and the value for members is updated.  The
                SSRC is then checked against the sender table.  If present, the entry
                is removed from the table, and the value for senders is updated.

                Furthermore, to make the transmission rate of RTCP packets more
                adaptive to changes in group membership, the following "reverse
                reconsideration" algorithm SHOULD be executed when a BYE packet is
                received.
            */

            m_RtcpPacketsReceived++;
            m_RtcpBytesReceived += count;
            // RFC requires IP header counted too, we just don't do it.
            m_RtcpAvgPacketSize = (1/16) * count + (15/16) * m_RtcpAvgPacketSize;

            try{
                RTCP_CompoundPacket compoundPacket = RTCP_CompoundPacket.Parse(buffer,count);
                // Process each RTCP packet.
                foreach(RTCP_Packet packet in compoundPacket.Packets){

                    #region APP

                    if(packet.Type == RTCP_PacketType.APP){
                        RTCP_Packet_APP app = ((RTCP_Packet_APP)packet);

                        RTP_Source_Remote source = GetOrCreateSource(true,app.Source,null,remoteEP);
                        if(source != null){
                            source.SetLastRtcpPacket(DateTime.Now);
                            source.OnAppPacket(app);
                        }
                    }

                    #endregion

                    #region BYE

                    else if(packet.Type == RTCP_PacketType.BYE){
                        RTCP_Packet_BYE bye = ((RTCP_Packet_BYE)packet);
                        
                        bool membersChanges = false;
                        foreach(uint src in bye.Sources){
                            RTP_Source source = GetOrCreateSource(true,src,null,remoteEP);
                            if(source != null){
                                membersChanges = true;
                                m_pMembers.Remove(src);
                                source.Close(bye.LeavingReason);
                                // Closing source will take care of closing it's underlaying stream, if source is "active".
                            }

                            m_pSenders.Remove(src);
                        }
                        if(membersChanges){
                            DoReverseReconsideration();
                        }
                    }

                    #endregion

                    #region RR

                    else if(packet.Type == RTCP_PacketType.RR){
                        RTCP_Packet_RR rr = ((RTCP_Packet_RR)packet);
                            
                        RTP_Source source = GetOrCreateSource(true,rr.SSRC,null,remoteEP);
                        if(source != null){
                            source.SetLastRtcpPacket(DateTime.Now);

                            foreach(RTCP_Packet_ReportBlock reportBlock in rr.ReportBlocks){
                                source = GetOrCreateSource(true,rr.SSRC,null,remoteEP);
                                if(source != null){
                                    source.SetLastRtcpPacket(DateTime.Now);
                                    source.SetRR(reportBlock);
                                }
                            }
                        }                        
                    }

                    #endregion

                    #region SDES

                    else if(packet.Type == RTCP_PacketType.SDES){ 
                        foreach(RTCP_Packet_SDES_Chunk sdes in ((RTCP_Packet_SDES)packet).Chunks){
                            RTP_Source source = GetOrCreateSource(true,sdes.Source,sdes.CName,remoteEP);
                            if(source != null){
                                source.SetLastRtcpPacket(DateTime.Now);

                                RTP_Participant_Remote participant = m_pSession.GetOrCreateParticipant(string.IsNullOrEmpty(sdes.CName) ? "null" : sdes.CName);

                                // Map participant to source.
                                ((RTP_Source_Remote)source).SetParticipant(participant);

                                // Map source to participant.
                                participant.EnsureSource(source);
                            
                                // Update participant SDES items.
                                participant.Update(sdes);                                
                            }                            
                        }
                    }

                    #endregion

                    #region SR

                    else if(packet.Type == RTCP_PacketType.SR){
                        RTCP_Packet_SR sr = ((RTCP_Packet_SR)packet);

                        RTP_Source_Remote source = GetOrCreateSource(true,sr.SSRC,null,remoteEP);
                        if(source != null){
                            source.SetLastRtcpPacket(DateTime.Now);
                            source.OnSenderReport(new RTCP_Report_Sender(sr));

                            foreach(RTCP_Packet_ReportBlock reportBlock in sr.ReportBlocks){
                                source = GetOrCreateSource(true,sr.SSRC,null,remoteEP);
                                if(source != null){
                                    source.SetLastRtcpPacket(DateTime.Now);
                                    source.SetRR(reportBlock);
                                }
                            }
                        }                        
                    }

                    #endregion

                    // Unknown packet.
                    else{
                        m_RtcpUnknownPacketsReceived++;
                    }
                }
            }
            catch(Exception x){
                m_pSession.OnError(x);
            }
        }

        #endregion

        #region method ProcessRtp

        /// <summary>
        /// Processes specified RTP data.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="count">Number of bytes in data buffer.</param>
        /// <param name="remoteEP">IP end point what sent RTCP packet.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> or <b>remoteEP</b> is null reference.</exception>
        private void ProcessRtp(byte[] buffer,int count,IPEndPoint remoteEP)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(remoteEP == null){
                throw new ArgumentNullException("remoteEP");
            }

            /* RFC 3550 6.3.3 Receiving an RTP or Non-BYE RTCP Packet
                When an RTP or RTCP packet is received from a participant whose SSRC
                is not in the member table, the SSRC is added to the table, and the
                value for members is updated once the participant has been validated
                as described in Section 6.2.1.  The same processing occurs for each
                CSRC in a validated RTP packet.

                When an RTP packet is received from a participant whose SSRC is not
                in the sender table, the SSRC is added to the table, and the value
                for senders is updated.
            */

            m_RtpPacketsReceived++;
            m_RtpBytesReceived += count;

            try{
                RTP_Packet packet = RTP_Packet.Parse(buffer,count);

                RTP_Source source = GetOrCreateSource(false,packet.SSRC,null,remoteEP);
                if(source != null){                    
                    // Process CSRC.
                    foreach(uint csrc in packet.CSRC){
                        RTP_Source dummy = GetOrCreateSource(false,packet.SSRC,null,remoteEP);
                    }

                    lock(m_pSenders){
                        if(!m_pSenders.ContainsKey(source.SSRC)){
                            m_pSenders.Add(source.SSRC,source);
                        }
                    }

                    // Let source to process RTP packet.
                    ((RTP_Source_Remote)source).OnRtpPacketReceived(packet,count);
                }                
            }
            catch(Exception x){
                m_pSession.OnError(x);
            }
        }

        #endregion


        #region method CreateLocalSource
        
        /// <summary>
        /// Creates local source.
        /// </summary>
        /// <returns>Returns new local source.</returns>
        internal RTP_Source_Local CreateLocalSource()
        {
            uint ssrc = RTP_Utils.GenerateSSRC();
            // Ensure that any member don't have such SSRC.
            while(m_pMembers.ContainsKey(ssrc)){
                ssrc = RTP_Utils.GenerateSSRC();
            }

            RTP_Source_Local source = new RTP_Source_Local(this,ssrc,m_pLocalEP.RtcpEP,m_pLocalEP.RtpEP);
            source.Disposing += new EventHandler(delegate(object s,EventArgs e){
                m_pSenders.Remove(source.SSRC);
                m_pMembers.Remove(source.SSRC);    
                m_pLocalSources.Remove(source);
            });
            m_pLocalSources.Add(source);
            m_pSession.LocalParticipant.EnsureSource(source);

            return source;
        }

        #endregion

        #region method GetOrCreateSource

        /// <summary>
        /// Gets or creates source. This method also does RFC 3550 8.2 "Collision Resolution and Loop Detection".
        /// </summary>
        /// <param name="rtcp_rtp">If true <b>src</b> is RTCP identifier, otherwise RTP identifier.</param>
        /// <param name="src">Source SSRC or CSRC identifier.</param>
        /// <param name="cname">RTCP SDES chunk CNAME. Must be passed only if <b>src</b> if from RTCP SDES chunk.</param>
        /// <param name="packetEP">Packet sender end point.</param>
        /// <returns>Returns specified source. Returns null if source has "collision or loop".</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>packetEP</b> is null reference.</exception>
        private RTP_Source_Remote GetOrCreateSource(bool rtcp_rtp,uint src,string cname,IPEndPoint packetEP)
        {
            if(packetEP == null){
                throw new ArgumentNullException("packetEP");
            }

            /* RFC 3550 8.2.
                if(SSRC or CSRC identifier is not found in the source identifier table){
                    create a new entry storing the data or control source transport address, the SSRC or CSRC and other state;
                }
                else if(table entry was created on receipt of a control packet and this is the first data packet or vice versa){
                    store the source transport address from this packet;
                }
                else if(source transport address from the packet does not match the one saved in the table entry for this identifier){
                    // An identifier collision or a loop is indicated 
                    if(source identifier is not the participant's own){
                        // OPTIONAL error counter step
                        if(source identifier is from an RTCP SDES chunk containing a CNAME item that differs from the CNAME in the table entry){
                            count a third-party collision;
                        }
                        else{
                            count a third-party loop;
                        }
             
                        abort processing of data packet or control element;
                        // MAY choose a different policy to keep new source
                    }
                    // A collision or loop of the participant's own packets
                    else if(source transport address is found in the list of conflicting data or control source transport addresses){
                        // OPTIONAL error counter step
                        if(source identifier is not from an RTCP SDES chunk containing a CNAME item or CNAME is the participant's own){
                            count occurrence of own traffic looped;
                        }
             
                        mark current time in conflicting address list entry;
                        abort processing of data packet or control element;
                    }
                    // New collision, change SSRC identifier
                    else{
                        log occurrence of a collision;
                        create a new entry in the conflicting data or control source transport address list and mark current time;
                        send an RTCP BYE packet with the old SSRC identifier;
                        choose a new SSRC identifier;
                        create a new entry in the source identifier table with the old SSRC plus the source transport address from
                            the data or control packet being processed;
                    }
                }
            */

            RTP_Source source = null;

            lock(m_pMembers){                
                m_pMembers.TryGetValue(src,out source);

                // SSRC or CSRC identifier is not found in the source identifier table.
                if(source == null){
                    source = new RTP_Source_Remote(this,src);
                    if(rtcp_rtp){
                        source.SetRtcpEP(packetEP);
                    }
                    else{
                        source.SetRtpEP(packetEP);
                    }
                    m_pMembers.Add(src,source);
                }
                // Table entry was created on receipt of a control packet and this is the first data packet or vice versa.
                else if((rtcp_rtp ? source.RtcpEP : source.RtpEP) == null){
                    if(rtcp_rtp){
                        source.SetRtcpEP(packetEP);
                    }
                    else{
                        source.SetRtpEP(packetEP);
                    }
                }
                // Source transport address from the packet does not match the one saved in the table entry for this identifier.
                else if(!packetEP.Equals((rtcp_rtp ? source.RtcpEP : source.RtpEP))){
                    // Source identifier is not the participant's own.
                    if(!source.IsLocal){
                        if(cname != null && cname != source.CName){
                            m_RemoteCollisions++;
                        }
                        else{
                            m_RemotePacketsLooped++;
                        }

                        return null;
                    }
                    // A collision or loop of the participant's own packets.
                    else if(m_pConflictingEPs.ContainsKey(packetEP.ToString())){
                        if(cname == null || cname == source.CName){
                            m_LocalPacketsLooped++;
                        }

                        m_pConflictingEPs[packetEP.ToString()] = DateTime.Now;

                        return null;
                    }
                    // New collision, change SSRC identifier.
                    else{
                        m_LocalCollisions++;
                        m_pConflictingEPs.Add(packetEP.ToString(),DateTime.Now);

                        // Remove SSRC from members,senders. Choose new SSRC, CNAME new and BYE old.
                        m_pMembers.Remove(source.SSRC);
                        m_pSenders.Remove(source.SSRC);
                        uint oldSSRC = source.SSRC;                        
                        source.GenerateNewSSRC();
                        // Ensure that new SSRC is not in use, if so repaeat while not conflicting SSRC.
                        while(m_pMembers.ContainsKey(source.SSRC)){
                            source.GenerateNewSSRC();
                        }
                        m_pMembers.Add(source.SSRC,source);

                        RTCP_CompoundPacket compoundPacket = new RTCP_CompoundPacket();
                        RTCP_Packet_RR rr = new RTCP_Packet_RR();
                        rr.SSRC = m_pRtcpSource.SSRC;
                        compoundPacket.Packets.Add(rr);
                        RTCP_Packet_SDES sdes = new RTCP_Packet_SDES();
                        RTCP_Packet_SDES_Chunk sdes_chunk = new RTCP_Packet_SDES_Chunk(source.SSRC,m_pSession.LocalParticipant.CNAME);
                        sdes.Chunks.Add(sdes_chunk);
                        compoundPacket.Packets.Add(sdes);
                        RTCP_Packet_BYE bye = new RTCP_Packet_BYE();
                        bye.Sources = new uint[]{oldSSRC};
                        bye.LeavingReason = "Collision, changing SSRC.";
                        compoundPacket.Packets.Add(bye);

                        SendRtcpPacket(compoundPacket);
                        //----------------------------------------------------------------------

                        // Add new source to members, it's not conflicting any more, we changed SSRC.
                        source = new RTP_Source_Remote(this,src);
                        if(rtcp_rtp){
                            source.SetRtcpEP(packetEP);
                        }
                        else{
                            source.SetRtpEP(packetEP);
                        }
                        m_pMembers.Add(src,source);
                    }
                }
            }

            return (RTP_Source_Remote)source;
        }

        #endregion

        #region method Schedule

        /// <summary>
        /// Schedules RTCP transmission.
        /// </summary>
        /// <param name="seconds">After number of seconds to transmit next RTCP.</param>
        private void Schedule(int seconds)
        {
            m_pRtcpTimer.Stop();
            m_pRtcpTimer.Interval = seconds * 1000;
            m_pRtcpTimer.Enabled = true;
        }

        #endregion

        #region method ComputeRtcpTransmissionInterval

        /// <summary>
        /// Computes RTCP transmission interval. Defined in RFC 3550 6.3.1.
        /// </summary>
        /// <param name="members">Current mebers count.</param>
        /// <param name="senders">Current sender count.</param>
        /// <param name="rtcp_bw">RTCP bandwidth.</param>
        /// <param name="we_sent">Specifies if we have sent data after last 2 RTCP interval.</param>
        /// <param name="avg_rtcp_size">Average RTCP raw packet size, IP headers included.</param>
        /// <param name="initial">Specifies if we ever have sent data to target.</param>
        /// <returns>Returns transmission interval in seconds.</returns>
        private int ComputeRtcpTransmissionInterval(int members,int senders,double rtcp_bw,bool we_sent,double avg_rtcp_size,bool initial)
        {
            // RFC 3550 A.7.

            /*
                Minimum average time between RTCP packets from this site (in
                seconds).  This time prevents the reports from `clumping' when
                sessions are small and the law of large numbers isn't helping
                to smooth out the traffic.  It also keeps the report interval
                from becoming ridiculously small during transient outages like
                a network partition.
            */
            double RTCP_MIN_TIME = 5;
            /*
                Fraction of the RTCP bandwidth to be shared among active
                senders.  (This fraction was chosen so that in a typical
                session with one or two active senders, the computed report
                time would be roughly equal to the minimum report time so that
                we don't unnecessarily slow down receiver reports.)  The
                receiver fraction must be 1 - the sender fraction.
            */
            double RTCP_SENDER_BW_FRACTION = 0.25;
            double RTCP_RCVR_BW_FRACTION = (1-RTCP_SENDER_BW_FRACTION);            
            /* 
                To compensate for "timer reconsideration" converging to a
                value below the intended average.
            */
            double COMPENSATION = 2.71828 - 1.5;

            double t;                   /* interval */
            double rtcp_min_time = RTCP_MIN_TIME;
            int n;                      /* no. of members for computation */

            /*
                Very first call at application start-up uses half the min
                delay for quicker notification while still allowing some time
                before reporting for randomization and to learn about other
                sources so the report interval will converge to the correct
                interval more quickly.
            */
            if(initial){
                rtcp_min_time /= 2;
            }
            /*
                Dedicate a fraction of the RTCP bandwidth to senders unless
                the number of senders is large enough that their share is
                more than that fraction.
            */
            n = members;
            if(senders <= (members * RTCP_SENDER_BW_FRACTION)){
                if(we_sent){
                    rtcp_bw = (rtcp_bw * RTCP_SENDER_BW_FRACTION);
                    n = senders;
                }
                else{
                    rtcp_bw = (rtcp_bw * RTCP_SENDER_BW_FRACTION);
                    n -= senders;
                }
            }

            /*
                The effective number of sites times the average packet size is
                the total number of octets sent when each site sends a report.
                Dividing this by the effective bandwidth gives the time
                interval over which those packets must be sent in order to
                meet the bandwidth target, with a minimum enforced.  In that
                time interval we send one report so this time is also our
                average time between reports.
            */
            t = avg_rtcp_size * n / rtcp_bw;
            if(t < rtcp_min_time){
                t = rtcp_min_time;
            }

            /*
                To avoid traffic bursts from unintended synchronization with
                other sites, we then pick our actual next report interval as a
                random number uniformly distributed between 0.5*t and 1.5*t.
            */
            t = t * (new Random().Next(5,15) / 10.0);
            t = t / COMPENSATION;

            return (int)Math.Max(t,2.0);
        }

        #endregion

        #region method DoReverseReconsideration

        /// <summary>
        /// Does "reverse reconsideration" algorithm. Defined in RFC 3550 6.3.4.
        /// </summary>
        private void DoReverseReconsideration()
        {
            /* RFC 3550 6.3.4. "reverse reconsideration"
                o  The value for tn is updated according to the following formula:
                   tn = tc + (members/pmembers) * (tn - tc)

                o  The value for tp is updated according the following formula:
                   tp = tc - (members/pmembers) * (tc - tp).
              
                o  The next RTCP packet is rescheduled for transmission at time tn,
                   which is now earlier.

                o  The value of pmembers is set equal to members.

                This algorithm does not prevent the group size estimate from
                incorrectly dropping to zero for a short time due to premature
                timeouts when most participants of a large session leave at once but
                some remain.  The algorithm does make the estimate return to the
                correct value more rapidly.  This situation is unusual enough and the
                consequences are sufficiently harmless that this problem is deemed
                only a secondary concern.
            */
            
            DateTime timeNext = m_RtcpLastTransmission == DateTime.MinValue ? DateTime.Now : m_RtcpLastTransmission.AddMilliseconds(m_pRtcpTimer.Interval);

            Schedule((int)Math.Max((m_pMembers.Count / m_PMembersCount) * ((TimeSpan)(timeNext - DateTime.Now)).TotalSeconds,2));
            
            m_PMembersCount = m_pMembers.Count;
        }

        #endregion

        #region method TimeOutSsrc

        /// <summary>
        /// Does RFC 3550 6.3.5 Timing Out an SSRC.
        /// </summary>
        private void TimeOutSsrc()
        {
            /* RFC 3550 6.3.5 Timing Out an SSRC.
                At occasional intervals, the participant MUST check to see if any of
                the other participants time out.  To do this, the participant
                computes the deterministic (without the randomization factor)
                calculated interval Td for a receiver, that is, with we_sent false.
                Any other session member who has not sent an RTP or RTCP packet since
                time tc - MTd (M is the timeout multiplier, and defaults to 5) is
                timed out.  This means that its SSRC is removed from the member list,
                and members is updated.  A similar check is performed on the sender
                list.  Any member on the sender list who has not sent an RTP packet
                since time tc - 2T (within the last two RTCP report intervals) is
                removed from the sender list, and senders is updated.

                If any members time out, the reverse reconsideration algorithm
                described in Section 6.3.4 SHOULD be performed.

                The participant MUST perform this check at least once per RTCP
                transmission interval.
            */

            bool membersUpdated = false;
                         
            // Senders check.
            RTP_Source[] senders = new RTP_Source[m_pSenders.Count];
            m_pSenders.Values.CopyTo(senders,0);
            foreach(RTP_Source sender in senders){
                // Sender has not sent RTP data since last two RTCP intervals.
                if(sender.LastRtpPacket.AddMilliseconds(2 * m_pRtcpTimer.Interval) < DateTime.Now){
                    m_pSenders.Remove(sender.SSRC);

                    // Mark source "passive".
                    sender.SetActivePassive(false);
                }
            }

            int Td = ComputeRtcpTransmissionInterval(m_pMembers.Count,m_pSenders.Count,m_Bandwidth * 0.25,false,m_RtcpAvgPacketSize,false);

            // Members check.
            foreach(RTP_Source member in this.Members){                
                // Source timed out.
                if(member.LastActivity.AddSeconds(5 * Td) < DateTime.Now){
                    m_pMembers.Remove(member.SSRC);
                    // Don't dispose local source, just remove only from members.
                    if(!member.IsLocal){
                        member.Dispose();
                    }
                    membersUpdated = true;
                }
            }          
            
            if(membersUpdated){
                DoReverseReconsideration();
            }            
        }

        #endregion

        #region method SendRtcp

        /// <summary>
        /// Sends RTCP report.
        /// </summary>
        private void SendRtcp()
        {
            /* RFC 3550 6.4 Sender and Receiver Reports
                RTP receivers provide reception quality feedback using RTCP report
                packets which may take one of two forms depending upon whether or not
                the receiver is also a sender.  The only difference between the
                sender report (SR) and receiver report (RR) forms, besides the packet
                type code, is that the sender report includes a 20-byte sender
                information section for use by active senders.  The SR is issued if a
                site has sent any data packets during the interval since issuing the
                last report or the previous one, otherwise the RR is issued.

                Both the SR and RR forms include zero or more reception report
                blocks, one for each of the synchronization sources from which this
                receiver has received RTP data packets since the last report.
                Reports are not issued for contributing sources listed in the CSRC
                list.  Each reception report block provides statistics about the data
                received from the particular source indicated in that block.  Since a
                maximum of 31 reception report blocks will fit in an SR or RR packet,
                additional RR packets SHOULD be stacked after the initial SR or RR
                packet as needed to contain the reception reports for all sources
                heard during the interval since the last report.  If there are too
                many sources to fit all the necessary RR packets into one compound
                RTCP packet without exceeding the MTU of the network path, then only
                the subset that will fit into one MTU SHOULD be included in each
                interval.  The subsets SHOULD be selected round-robin across multiple
                intervals so that all sources are reported.                
            */

            bool we_sent = false;

            try{
                m_pRtcpSource.SetLastRtcpPacket(DateTime.Now);
                                
                RTCP_CompoundPacket compundPacket = new RTCP_CompoundPacket();

                RTCP_Packet_RR rr = null;

                // Find active send streams.
                List<RTP_SendStream> activeSendStreams = new List<RTP_SendStream>();
                foreach(RTP_SendStream stream in this.SendStreams){
                    if(stream.RtcpCyclesSinceWeSent < 2){
                        activeSendStreams.Add(stream);
                        we_sent = true;
                    }
                    // Notify stream about RTCP cycle.
                    stream.RtcpCycle();
                }

                #region SR(s) / RR

                // We are sender.
                if(we_sent){
                    // Create SR for each active send stream.
                    for(int i=0;i<activeSendStreams.Count;i++){
                        RTP_SendStream sendStream = activeSendStreams[i];

                        RTCP_Packet_SR sr = new RTCP_Packet_SR(sendStream.Source.SSRC);
                        sr.NtpTimestamp      = RTP_Utils.DateTimeToNTP64(DateTime.Now);
                        sr.RtpTimestamp      = m_pRtpClock.RtpTimestamp;
                        sr.SenderPacketCount = (uint)sendStream.RtpPacketsSent;
                        sr.SenderOctetCount  = (uint)sendStream.RtpBytesSent;

                        compundPacket.Packets.Add(sr);
                    }
                }
                // We are receiver.
                else{
                    rr = new RTCP_Packet_RR();
                    rr.SSRC = m_pRtcpSource.SSRC;
                    compundPacket.Packets.Add(rr);

                    // Report blocks added later.                
                }

                #endregion

                #region SDES

                RTCP_Packet_SDES sdes = new RTCP_Packet_SDES();
                // Add default SSRC.
                RTCP_Packet_SDES_Chunk sdesChunk = new RTCP_Packet_SDES_Chunk(m_pRtcpSource.SSRC,m_pSession.LocalParticipant.CNAME);
                // Add next optional SDES item, if any. (We round-robin optional items)
                m_pSession.LocalParticipant.AddNextOptionalSdesItem(sdesChunk);
                sdes.Chunks.Add(sdesChunk);   
                // Add all active send streams SSRC -> CNAME. This enusres that all send streams will be mapped to participant.
                foreach(RTP_SendStream stream in activeSendStreams){
                    sdes.Chunks.Add(new RTCP_Packet_SDES_Chunk(stream.Source.SSRC,m_pSession.LocalParticipant.CNAME));
                }
                compundPacket.Packets.Add(sdes);

                #endregion

                #region RR filling

                /* RR reporting:
                    Report up to 31 active senders, if more senders, reoprt next with next interval.
                    Report oldest not reported first,then ventually all sources will be reported with this algorythm.
                */
                RTP_Source[]        senders             = this.Senders;
                DateTime[]          acitveSourceRRTimes = new DateTime[senders.Length];
                RTP_ReceiveStream[] activeSenders       = new RTP_ReceiveStream[senders.Length];
                int                 activeSenderCount   = 0;
                foreach(RTP_Source sender in senders){
                    // Remote sender sent RTP data during last RTCP interval.
                    if(!sender.IsLocal && sender.LastRtpPacket > m_RtcpLastTransmission){
                        acitveSourceRRTimes[activeSenderCount] = sender.LastRRTime;
                        activeSenders[activeSenderCount]       = ((RTP_Source_Remote)sender).Stream;
                        activeSenderCount++;
                    }
                }                
                // Create RR is SR report and no RR created yet.
                if(rr == null){
                    rr = new RTCP_Packet_RR();
                    rr.SSRC = m_pRtcpSource.SSRC;
                    compundPacket.Packets.Add(rr);
                }
                // Sort ASC.
                Array.Sort(acitveSourceRRTimes,activeSenders,0,activeSenderCount);
                // Add up to 31 oldest not reported sources to report.
                for(int i=1;i<31;i++){
                    if((activeSenderCount - i) < 0){
                        break;
                    }
                    rr.ReportBlocks.Add(activeSenders[activeSenderCount - i].CreateReceiverReport());
                }

                #endregion

                // Send RTPC packet.
                SendRtcpPacket(compundPacket);

                // Timeout conflicting transport addresses, if not conflicting any more.
                lock(m_pConflictingEPs){
                    string[] keys = new string[m_pConflictingEPs.Count];
                    m_pConflictingEPs.Keys.CopyTo(keys,0);
                    foreach(string key in keys){
                        if(m_pConflictingEPs[key].AddMinutes(3) < DateTime.Now){
                            m_pConflictingEPs.Remove(key);
                        }
                    }
                }

                // Since we must check timing out sources at least once per RTCP interval, so we
                // check this before sending RTCP.
                TimeOutSsrc();
            }
            catch(Exception x){
                if(this.IsDisposed){
                    return;
                }

                m_pSession.OnError(x);
            }

            m_RtcpLastTransmission = DateTime.Now;

            // Schedule next RTCP sending.
            Schedule(ComputeRtcpTransmissionInterval(m_pMembers.Count,m_pSenders.Count,m_Bandwidth * 0.25,we_sent,m_RtcpAvgPacketSize,false));
        }

        #endregion


        #region method RtpAsyncSocketSendCompleted

        /// <summary>
        /// Is called when RTP socket has finisehd data sending.
        /// </summary>
        /// <param name="ar">The result of the asynchronous operation.</param>
        private void RtpAsyncSocketSendCompleted(IAsyncResult ar)
        {
            try{
                m_RtpBytesSent += m_pRtpSocket.EndSendTo(ar);
                m_RtpPacketsSent++;
            }
            catch{
                m_RtpFailedTransmissions++;
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
        /// Gets owner RTP multimedia session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_MultimediaSession Session
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSession; 
            }
        }

        /// <summary>
        /// Gets local RTP end point.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Address LocalEP
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pLocalEP; 
            }
        }

        /// <summary>
        /// Gets RTP media clock.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Clock RtpClock
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRtpClock; 
            }
        }

        /// <summary>
        /// Gets or sets stream mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_StreamMode StreamMode
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_StreamMode; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_StreamMode = value;
            }
        }

        /// <summary>
        /// Gets RTP session remote targets.
        /// </summary>
        /// <remarks>Normally RTP session has only 1 remote target, for multi-unicast session, there may be more than 1 target.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Address[] Targets
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pTargets.ToArray(); 
            }
        }

        /// <summary>
        /// Gets maximum transfet unit size in bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int MTU
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MTU; 
            }
        }

        /// <summary>
        /// Gets or sets sending payload.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int Payload
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_Payload; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                if(m_Payload != value){
                    m_Payload = value;

                    OnPayloadChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets session bandwidth in bits per second.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int Bandwidth
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_Bandwidth; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 8){
                    throw new ArgumentException("Property 'Bandwidth' value must be >= 8.");
                }

                m_Bandwidth = value;
            }
        }

        /// <summary>
        /// Gets session members. Session member is local/remote source what sends RTCP,RTP or RTCP-RTP data.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Source[] Members
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                lock(m_pMembers){
                    RTP_Source[] sources = new RTP_Source[m_pMembers.Count];
                    m_pMembers.Values.CopyTo(sources,0);

                    return sources;
                }
            }
        }

        /// <summary>
        /// Gets session senders. Sender is local/remote source what sends RTP data.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_Source[] Senders
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                lock(m_pSenders){
                    RTP_Source[] sources = new RTP_Source[m_pSenders.Count];
                    m_pSenders.Values.CopyTo(sources,0);

                    return sources;
                }
            }
        }

        /// <summary>
        /// Gets the RTP streams what we send.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_SendStream[] SendStreams
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                lock(m_pLocalSources){
                    List<RTP_SendStream> retVal = new List<RTP_SendStream>();
                    foreach(RTP_Source_Local source in m_pLocalSources){
                        if(source.Stream != null){
                            retVal.Add(source.Stream);
                        }
                    }

                    return retVal.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the RTP streams what we receive.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public RTP_ReceiveStream[] ReceiveStreams
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                lock(m_pSenders){
                    List<RTP_ReceiveStream> retVal = new List<RTP_ReceiveStream>();
                    foreach(RTP_Source source in m_pSenders.Values){
                        if(!source.IsLocal){
                            retVal.Add(((RTP_Source_Remote)source).Stream);
                        }
                    }

                    return retVal.ToArray();
                }
            }
        }
     
        /// <summary>
        /// Gets total of RTP packets sent by this session.
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
        /// Gets total of RTP bytes(RTP headers included) sent by this session.
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
        /// Gets total of RTP packets received by this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtpPacketsReceived
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtpPacketsReceived; 
            }
        }

        /// <summary>
        /// Gets total of RTP bytes(RTP headers included) received by this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtpBytesReceived
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtpBytesReceived; 
            }
        }

        /// <summary>
        /// Gets number of times RTP packet sending has failed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtpFailedTransmissions
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtpFailedTransmissions; 
            }
        }

        /// <summary>
        /// Gets total of RTCP packets sent by this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtcpPacketsSent
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtcpPacketsSent; 
            }
        }

        /// <summary>
        /// Gets total of RTCP bytes(RTCP headers included) sent by this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtcpBytesSent
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtcpBytesSent; 
            }
        }

        /// <summary>
        /// Gets total of RTCP packets received by this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtcpPacketsReceived
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtcpPacketsReceived; 
            }
        }

        /// <summary>
        /// Gets total of RTCP bytes(RTCP headers included) received by this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtcpBytesReceived
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtcpBytesReceived; 
            }
        }

        /// <summary>
        /// Gets number of times RTCP packet sending has failed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RtcpFailedTransmissions
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtcpFailedTransmissions; 
            }
        }

        /// <summary>
        /// Current RTCP reporting interval in seconds.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int RtcpInterval
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (int)(m_pRtcpTimer.Interval / 1000); 
            }
        }

        /// <summary>
        /// Gets time when last RTCP report was sent.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public DateTime RtcpLastTransmission
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RtcpLastTransmission; 
            }
        }

        /// <summary>
        /// Gets number of times local SSRC collision dedected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long LocalCollisions
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LocalCollisions; 
            }
        }

        /// <summary>
        /// Gets number of times remote SSRC collision dedected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RemoteCollisions
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RemoteCollisions; 
            }
        }

        /// <summary>
        /// Gets number of times local packets loop dedected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long LocalPacketsLooped
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_LocalPacketsLooped; 
            }
        }

        /// <summary>
        /// Gets number of times remote packets loop dedected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public long RemotePacketsLooped
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RemotePacketsLooped; 
            }
        }

        /// <summary>
        /// Gets RTP payloads.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public KeyValueCollection<int,Codec> Payloads
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pPayloads; 
            }
        }
                
        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when RTP session has disposed.
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
        /// Is raised when RTP session has closed.
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
        /// Is raised when new send stream created.
        /// </summary>
        public event EventHandler<RTP_SendStreamEventArgs> NewSendStream = null;

        #region method OnNewSendStream

        /// <summary>
        /// Raises <b>NewSendStream</b> event.
        /// </summary>
        /// <param name="stream">New send stream.</param>
        private void OnNewSendStream(RTP_SendStream stream)
        {
            if(this.NewSendStream != null){
                this.NewSendStream(this,new RTP_SendStreamEventArgs(stream));
            }
        }

        #endregion

        /// <summary>
        /// Is raised when new recieve stream received from remote target.
        /// </summary>
        public event EventHandler<RTP_ReceiveStreamEventArgs> NewReceiveStream = null;

        #region mehtod OnNewReceiveStream

        /// <summary>
        /// Raises <b>NewReceiveStream</b> event.
        /// </summary>
        /// <param name="stream">New receive stream.</param>
        internal void OnNewReceiveStream(RTP_ReceiveStream stream)
        {
            if(this.NewReceiveStream != null){
                this.NewReceiveStream(this,new RTP_ReceiveStreamEventArgs(stream));
            }
        }

        #endregion

        /// <summary>
        /// Is raised when session sending payload has changed.
        /// </summary>
        public event EventHandler PayloadChanged = null;

        #region method OnPayloadChanged

        /// <summary>
        /// Raises <b>PayloadChanged</b> event.
        /// </summary>
        private void OnPayloadChanged()
        {
            if(this.PayloadChanged != null){
                this.PayloadChanged(this,new EventArgs());
            }
        }

        #endregion

        #endregion

    }
}
