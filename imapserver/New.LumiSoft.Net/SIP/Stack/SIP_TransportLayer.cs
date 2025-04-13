using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

using LumiSoft.Net.DNS;
using LumiSoft.Net.DNS.Client;
using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.UDP;
using LumiSoft.Net.TCP;
using LumiSoft.Net.STUN.Client;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Implements SIP transport layer. Defined in RFC 3261.
    /// </summary>
    public class SIP_TransportLayer
    {
        #region class SIP_FlowManager

        /// <summary>
        /// Implements SIP flow manager.
        /// </summary>
        private class SIP_FlowManager : IDisposable
        {
            private bool                        m_IsDisposed    = false;
            private SIP_TransportLayer          m_pOwner        = null;
            private Dictionary<string,SIP_Flow> m_pFlows        = null;
            private TimerEx                     m_pTimeoutTimer = null;
            private int                         m_IdelTimeout   = 60 * 5;
            private object                      m_pLock         = new object();

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner transport layer.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal SIP_FlowManager(SIP_TransportLayer owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pOwner = owner;

                m_pFlows = new Dictionary<string,SIP_Flow>();

                m_pTimeoutTimer = new TimerEx(15000);
                m_pTimeoutTimer.AutoReset = true;
                m_pTimeoutTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimeoutTimer_Elapsed);
                m_pTimeoutTimer.Enabled = true;
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                lock(m_pLock){
                    if(m_IsDisposed){
                        return;
                    }
                    m_IsDisposed = true;

                    foreach(SIP_Flow flow in this.Flows){
                        flow.Dispose();
                    }

                    m_pOwner = null;
                    m_pFlows = null;
                    m_pTimeoutTimer.Dispose();
                    m_pTimeoutTimer = null;
                }
            }

            #endregion


            #region Events handling

            #region method m_pTimeoutTimer_Elapsed

            private void m_pTimeoutTimer_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
            {
                lock(m_pLock){
                    if(m_IsDisposed){
                        return;
                    }
                
                    foreach(SIP_Flow flow in this.Flows){
                        try{
                            if(flow.LastActivity.AddSeconds(m_IdelTimeout) < DateTime.Now){
                                flow.Dispose();
                            }
                        }
                        catch(ObjectDisposedException x){
                            string dummy = x.Message;
                        }
                    }
                }
            }

            #endregion

            #endregion


            #region method GetOrCreateFlow

            /// <summary>
            /// Returns existing flow if exists, otherwise new created flow.
            /// </summary>
            /// <param name="isServer">Specifies if created flow is server or client flow. This has effect only if flow is created.</param>
            /// <param name="localEP">Local end point.</param>
            /// <param name="remoteEP">Remote end point.</param>
            /// <param name="transport">SIP transport.</param>
            /// <returns>Returns existing flow if exists, otherwise new created flow.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>localEP</b>,<b>remoteEP</b> or <b>transport</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            internal SIP_Flow GetOrCreateFlow(bool isServer,IPEndPoint localEP,IPEndPoint remoteEP,string transport)
            {
                if(localEP == null){
                    throw new ArgumentNullException("localEP");
                }
                if(remoteEP == null){
                    throw new ArgumentNullException("remoteEP");
                }
                if(transport == null){
                    throw new ArgumentNullException("transport");
                }
                                                            
                string flowID = localEP.ToString() + "-" + remoteEP.ToString() + "-" + transport.ToString();

                lock(m_pLock){
                    SIP_Flow flow = null;
                    if(m_pFlows.TryGetValue(flowID,out flow)){
                        return flow;
                    }
                    else{                    
                        flow =  new SIP_Flow(m_pOwner.Stack,isServer,localEP,remoteEP,transport);
                        m_pFlows.Add(flow.ID,flow);
                        flow.IsDisposing += new EventHandler(delegate(object s,EventArgs e){
                            lock(m_pLock){
                                m_pFlows.Remove(flowID);
                            }
                        });
                        flow.Start();
                    
                        return flow;
                    }
                }
            }

            #endregion

            #region method GetFlow

            /// <summary>
            /// Returns specified flow or null if no such flow.
            /// </summary>
            /// <param name="flowID">Data flow ID.</param>
            /// <returns>Returns specified flow or null if no such flow.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>flowID</b> is null reference.</exception>
            public SIP_Flow GetFlow(string flowID)
            {
                if(flowID == null){
                    throw new ArgumentNullException("flowID");
                }

                lock(m_pFlows){
                    SIP_Flow retVal = null;
                    m_pFlows.TryGetValue(flowID,out retVal);

                    return retVal;
                }
            }

            #endregion

            #region method CreateFromSession

            /// <summary>
            /// Creates new flow from TCP server session.
            /// </summary>
            /// <param name="session">TCP server session.</param>
            /// <returns>Returns created flow.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null reference.</exception>
            internal SIP_Flow CreateFromSession(TCP_ServerSession session)
            {
                if(session == null){
                    throw new ArgumentNullException("session");
                }

                string flowID = session.LocalEndPoint.ToString() + "-" + session.RemoteEndPoint.ToString() + "-" + (session.IsSecureConnection ? SIP_Transport.TLS : SIP_Transport.TCP);

                lock(m_pLock){
                    SIP_Flow flow = new SIP_Flow(m_pOwner.Stack,session);
                    m_pFlows.Add(flowID,flow);
                    flow.IsDisposing += new EventHandler(delegate(object s,EventArgs e){
                        lock(m_pLock){
                            m_pFlows.Remove(flowID);
                        }
                    });
                    flow.Start();
    
                    return flow;
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
            /// Gets number of flows in the collection.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public int Count
            {
                get{                
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pFlows.Count; 
                }
            }

            /// <summary>
            /// Gets a flow with the specified flow ID.
            /// </summary>
            /// <param name="flowID">SIP flow ID.</param>
            /// <returns>Returns flow with the specified flow ID or null if not found.</returns>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            /// <exception cref="ArgumentNullException">Is raised when <b>flowID</b> is null reference value.</exception>
            public SIP_Flow this[string flowID]
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(flowID == null){
                        throw new ArgumentNullException("flowID");
                    }

                    if(m_pFlows.ContainsKey(flowID)){
                        return m_pFlows[flowID];
                    }
                    else{
                        return null; 
                    }
                }
            }

            /// <summary>
            /// Gets active flows.
            /// </summary>
            public SIP_Flow[] Flows
            {
                get{
                    lock(m_pLock){
                        SIP_Flow[] retVal = new SIP_Flow[m_pFlows.Count];
                        m_pFlows.Values.CopyTo(retVal,0);

                        return retVal;
                    }
                }
            }

            /// <summary>
            /// Gets owner transpoprt layer.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            internal SIP_TransportLayer TransportLayer
            {
                get{ 
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pOwner; 
                }
            }

            #endregion

        }

        #endregion

        private bool                          m_IsDisposed   = false;
        private bool                          m_IsRunning    = false;
        private SIP_Stack                     m_pStack       = null;
        private IPBindInfo[]                  m_pBinds       = null;
        private UDP_Server                    m_pUdpServer   = null;
        private TCP_Server<TCP_ServerSession> m_pTcpServer   = null;
        private SIP_FlowManager               m_pFlowManager = null;
        private string                        m_StunServer   = null;
        private CircleCollection<IPAddress>   m_pLocalIPv4   = null;
        private CircleCollection<IPAddress>   m_pLocalIPv6   = null;
        private Random                        m_pRandom      = null;
                
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Owner SIP stack.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stack</b> is null reference.</exception>
        internal SIP_TransportLayer(SIP_Stack stack)
        {
            if(stack == null){
                throw new ArgumentNullException("stack");
            }

            m_pStack = stack;
          
            m_pUdpServer = new UDP_Server();
            m_pUdpServer.PacketReceived += new EventHandler<UDP_e_PacketReceived>(m_pUdpServer_PacketReceived);
            m_pUdpServer.Error += new ErrorEventHandler(m_pUdpServer_Error);

            m_pTcpServer = new TCP_Server<TCP_ServerSession>();
            m_pTcpServer.SessionCreated += new EventHandler<TCP_ServerSessionEventArgs<TCP_ServerSession>>(m_pTcpServer_SessionCreated);

            m_pFlowManager = new SIP_FlowManager(this);
                        
            m_pBinds = new IPBindInfo[]{};

            m_pRandom = new Random();

            m_pLocalIPv4 = new CircleCollection<IPAddress>();
            m_pLocalIPv6 = new CircleCollection<IPAddress>();
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

            Stop();

            m_IsRunning  = false;
            m_pBinds    = null;
            m_pRandom   = null;

            m_pTcpServer.Dispose();
            m_pTcpServer = null;

            m_pUdpServer.Dispose();
            m_pUdpServer = null;
        }

        #endregion

                
        #region Events handling

        #region method m_pUdpServer_PacketReceived

        /// <summary>
        /// This method is called when new SIP UDP packet has received.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pUdpServer_PacketReceived(object sender,UDP_e_PacketReceived e)
        {
            try{
                SIP_Flow flow = m_pFlowManager.GetOrCreateFlow(true,(IPEndPoint)e.Socket.LocalEndPoint,e.RemoteEP,SIP_Transport.UDP);
                flow.OnUdpPacketReceived(e);
            }
            catch(Exception x){
                m_pStack.OnError(x);
            }
        }

        #endregion

        #region method m_pUdpServer_Error

        /// <summary>
        /// This method is called when UDP server unknown error.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pUdpServer_Error(object sender,Error_EventArgs e)
        {
            m_pStack.OnError(e.Exception);
        }

        #endregion

        #region method m_pTcpServer_SessionCreated

        /// <summary>
        /// This method is called when SIP stack has got new incoming connection.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTcpServer_SessionCreated(object sender,TCP_ServerSessionEventArgs<TCP_ServerSession> e)
        {
            m_pFlowManager.CreateFromSession(e.Session);
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts listening incoming requests and responses.
        /// </summary>
        internal void Start()
        {
            if(m_IsRunning){
                return;
            }
            // Set this flag before running thread, otherwise thead may exist before you set this flag.
            m_IsRunning = true;
                        
            m_pUdpServer.Start();
            m_pTcpServer.Start();            
        }
                                
        #endregion

        #region method Stop

        /// <summary>
        /// Stops listening incoming requests and responses.
        /// </summary>
        internal void Stop()
        {
            if(!m_IsRunning){
                return;
            }
            m_IsRunning = false;

            m_pUdpServer.Stop();
            m_pTcpServer.Stop();        
        }

        #endregion


        #region method OnMessageReceived

        /// <summary>
        /// Is called when specified SIP flow has got new SIP message.
        /// </summary>
        /// <param name="flow">SIP flow.</param>
        /// <param name="message">Received message.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>message</b> is null reference.</exception>
        internal void OnMessageReceived(SIP_Flow flow,byte[] message)
        {
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
            if(message == null){
                throw new ArgumentNullException("message");
            }

            // TODO: Log
                        
            try{

                #region Ping / pong

                // We have "ping"(CRLFCRLF) request, response with "pong".
                if(message.Length == 4){
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddRead("",null,2,"Flow [id='" + flow.ID + "'] received \"ping\"",flow.LocalEP,flow.RemoteEP);
                    }

                    // Send "pong".
                    flow.SendInternal(new byte[]{(byte)'\r',(byte)'\n'});

                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddWrite("",null,2,"Flow [id='" + flow.ID + "'] sent \"pong\"",flow.LocalEP,flow.RemoteEP);
                    }

                    return;
                }
                // We have pong(CRLF), do nothing.
                else if(message.Length == 2){
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddRead("",null,2,"Flow [id='" + flow.ID + "'] received \"pong\"",flow.LocalEP,flow.RemoteEP);
                    }

                    return;
                }

                #endregion

                #region Response

                if(Encoding.UTF8.GetString(message,0,3).ToUpper().StartsWith("SIP")){

                    #region Parse and validate response

                    SIP_Response response = null;
                    try{
                        response = SIP_Response.Parse(message);
                    }
                    catch(Exception x){
                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddText("Skipping message, parse error: " + x.ToString());
                        }

                        return;
                    }
                                
                    try{
                        response.Validate();
                    }
                    catch(Exception x){
                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddText("Response validation failed: " + x.ToString());
                        }

                        return;
                    }

                    #endregion

                    /* RFC 3261 18.1.2 Receiving Responses.
                        When a response is received, the client transport examines the top
                        Via header field value.  If the value of the "sent-by" parameter in
                        that header field value does not correspond to a value that the
                        client transport is configured to insert into requests, the response
                        MUST be silently discarded.

                        If there are any client transactions in existence, the client
                        transport uses the matching procedures of Section 17.1.3 to attempt
                        to match the response to an existing transaction.  If there is a
                        match, the response MUST be passed to that transaction.  Otherwise,
                        the response MUST be passed to the core (whether it be stateless
                        proxy, stateful proxy, or UA) for further processing.  Handling of
                        these "stray" responses is dependent on the core (a proxy will
                        forward them, while a UA will discard, for example).
                    */
                                        
                    SIP_ClientTransaction transaction =  m_pStack.TransactionLayer.MatchClientTransaction(response);
                    // Allow client transaction to process response.
                    if(transaction != null){
                        transaction.ProcessResponse(flow,response);                        
                    }
                    else{
                        // Pass response to dialog.
                        SIP_Dialog dialog = m_pStack.TransactionLayer.MatchDialog(response);
                        if(dialog != null){
                            dialog.ProcessResponse(response);
                        }
                        // Pass response to core.
                        else{                    
                            m_pStack.OnResponseReceived(new SIP_ResponseReceivedEventArgs(m_pStack,null,response));
                        }
                    }
                }

                #endregion

                #region Request

                // SIP request.
                else{

                    #region Parse and validate request

                    SIP_Request request = null;
                    try{
                        request = SIP_Request.Parse(message);
                    }
                    catch(Exception x){
                        // Log
                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddText("Skipping message, parse error: " + x.Message);
                        }

                        return;
                    }

                    try{
                        request.Validate();
                    }
                    catch(Exception x){
                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddText("Request validation failed: " + x.ToString());
                        }

                        // Bad request, send error to request maker.
                        SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x400_Bad_Request + ". " + x.Message,request));

                        return;
                    }

                    #endregion

                    // TODO: Is that needed, core can reject message as it would like.
                    SIP_ValidateRequestEventArgs eArgs = m_pStack.OnValidateRequest(request,flow.RemoteEP);
                    // Request rejected, return response.
                    if(eArgs.ResponseCode != null){
                        SendResponse(m_pStack.CreateResponse(eArgs.ResponseCode,request));

                        return;
                    }

                    request.Flow = flow;
                    request.LocalEndPoint = flow.LocalEP;
                    request.RemoteEndPoint = flow.RemoteEP;

                    /* RFC 3261 18.2.1.
                        When the server transport receives a request over any transport, it
                        MUST examine the value of the "sent-by" parameter in the top Via
                        header field value.  If the host portion of the "sent-by" parameter
                        contains a domain name, or if it contains an IP address that differs
                        from the packet source address, the server MUST add a "received"
                        parameter to that Via header field value.  This parameter MUST
                        contain the source address from which the packet was received.  This
                        is to assist the server transport layer in sending the response,
                        since it must be sent to the source IP address from which the request
                        came.

                        Next, the server transport attempts to match the request to a server
                        transaction.  It does so using the matching rules described in
                        Section 17.2.3.  If a matching server transaction is found, the
                        request is passed to that transaction for processing.  If no match is
                        found, the request is passed to the core, which may decide to
                        construct a new server transaction for that request.  Note that when
                        a UAS core sends a 2xx response to INVITE, the server transaction is
                        destroyed.  This means that when the ACK arrives, there will be no
                        matching server transaction, and based on this rule, the ACK is
                        passed to the UAS core, where it is processed.
                    */

                    /* RFC 3581 4. 
                        When a server compliant to this specification (which can be a proxy
                        or UAS) receives a request, it examines the topmost Via header field
                        value.  If this Via header field value contains an "rport" parameter
                        with no value, it MUST set the value of the parameter to the source
                        port of the request.  This is analogous to the way in which a server
                        will insert the "received" parameter into the topmost Via header
                        field value.  In fact, the server MUST insert a "received" parameter
                        containing the source IP address that the request came from, even if
                        it is identical to the value of the "sent-by" component.  Note that
                        this processing takes place independent of the transport protocol.
                    */

                    SIP_t_ViaParm via = request.Via.GetTopMostValue();
                    via.Received = flow.RemoteEP.Address;
                    if(via.RPort == 0){
                        via.RPort = flow.RemoteEP.Port;
                    }

                    bool processed = false;
                    SIP_ServerTransaction transaction = m_pStack.TransactionLayer.MatchServerTransaction(request);
                    // Pass request to matched server transaction.
                    if(transaction != null){
                        transaction.ProcessRequest(flow,request);

                        processed = true;
                    }
                    else{
                        SIP_Dialog dialog = m_pStack.TransactionLayer.MatchDialog(request);
                        // Pass request to dialog.
                        if(dialog != null){
                            processed = dialog.ProcessRequest(new SIP_RequestReceivedEventArgs(m_pStack,flow,request));
                        }
                    }

                    // Request not proecced by dialog or transaction, pass request to TU.
                    if(!processed){
                        // Log
                        if(m_pStack.Logger != null){
                            byte[] requestData = request.ToByteData();

                            m_pStack.Logger.AddRead(
                                Guid.NewGuid().ToString(),
                                null,
                                0,
                                "Request [method='" + request.RequestLine.Method + "'; cseq='" + request.CSeq.SequenceNumber + "'; " + 
                                    "transport='" + flow.Transport + "'; size='" + requestData.Length + "'; " + 
                                    "received '" + flow.RemoteEP + "' -> '" + flow.LocalEP + "'.",
                                flow.LocalEP,
                                flow.RemoteEP,
                                requestData
                            );
                        }

                        m_pStack.OnRequestReceived(new SIP_RequestReceivedEventArgs(m_pStack,flow,request));
                    }
                }

                #endregion

            }
            catch(SocketException s){
                // Skip all socket errors here
                string dummy = s.Message;
            }
            catch(Exception x){
                m_pStack.OnError(x);
            }
        }

        #endregion


        #region method GetOrCreateFlow

        /// <summary>
        /// Gets existing flow or if flow doesn't exist, new one is created and returned.
        /// </summary>
        /// <param name="transport">SIP transport.</param>
        /// <param name="localEP">Local end point. Value null means system will allocate it.</param>
        /// <param name="remoteEP">Remote end point.</param>
        /// <returns>Returns data flow.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b>.</exception>
        public SIP_Flow GetOrCreateFlow(string transport,IPEndPoint localEP,IPEndPoint remoteEP)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(remoteEP == null){
                throw new ArgumentNullException("remoteEP");
            }

            if(localEP == null){
                if(string.Equals(transport,SIP_Transport.UDP,StringComparison.InvariantCultureIgnoreCase)){
                    // Get load-balanched local endpoint.
                    localEP = m_pUdpServer.GetLocalEndPoint(remoteEP);
                }
                else if(string.Equals(transport,SIP_Transport.TCP,StringComparison.InvariantCultureIgnoreCase)){
                    // Get load-balanched local IP for TCP and create random port.
                    if(remoteEP.AddressFamily == AddressFamily.InterNetwork){
                        localEP = new IPEndPoint(m_pLocalIPv4.Next(),m_pRandom.Next(10000,65000));
                    }
                    else{
                        localEP = new IPEndPoint(m_pLocalIPv4.Next(),m_pRandom.Next(10000,65000));
                    }
                }
                else if(string.Equals(transport,SIP_Transport.TLS,StringComparison.InvariantCultureIgnoreCase)){
                    // Get load-balanched local IP for TLS and create random port.
                    if(remoteEP.AddressFamily == AddressFamily.InterNetwork){
                        localEP = new IPEndPoint(m_pLocalIPv4.Next(),m_pRandom.Next(10000,65000));
                    }
                    else{
                        localEP = new IPEndPoint(m_pLocalIPv4.Next(),m_pRandom.Next(10000,65000));
                    }
                }
                else{
                    throw new ArgumentException("Not supported transoprt '" + transport + "'.");
                }
            }

            return m_pFlowManager.GetOrCreateFlow(false,localEP,remoteEP,transport);
        }

        #endregion

        #region method GetFlow

        /// <summary>
        /// Returns specified flow or null if no such flow.
        /// </summary>
        /// <param name="flowID">Data flow ID.</param>
        /// <returns>Returns specified flow or null if no such flow.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>flowID</b> is null reference.</exception>
        public SIP_Flow GetFlow(string flowID)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(flowID == null){
                throw new ArgumentNullException("flowID");
            }

            return m_pFlowManager.GetFlow(flowID);
        }

        #endregion


        #region method SendRequest

        /// <summary>
        /// Sends request using methods as described in RFC 3261 [4](RFC 3263).
        /// </summary>
        /// <param name="request">SIP request to send.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>request</b> is null.</exception>
        /// <exception cref="SIP_TransportException">Is raised when transport error happens.</exception>
        public void SendRequest(SIP_Request request)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }

            SIP_Hop[] hops = m_pStack.GetHops((SIP_Uri)request.RequestLine.Uri,request.ToByteData().Length,false);
            if(hops.Length == 0){
                throw new SIP_TransportException("No target hops for URI '" + request.RequestLine.Uri.ToString() + "'.");
            }

            SIP_TransportException lastException = null;
            foreach(SIP_Hop hop in hops){
                try{
                    SendRequest(request,null,hop);

                    return;
                }
                catch(SIP_TransportException x){
                    lastException = x;
                }
            }

            // If we reach so far, send failed, return last error.
            throw lastException;
        }

        /// <summary>
        /// Sends request to the specified hop.
        /// </summary>
        /// <param name="request">SIP request.</param>
        /// <param name="localEP">Local end point. Value null means system will allocate it.</param>
        /// <param name="hop">Target hop.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>request</b> or <b>hop</b> is null reference.</exception>
        public void SendRequest(SIP_Request request,IPEndPoint localEP,SIP_Hop hop)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(hop == null){
                throw new ArgumentNullException("hop");
            }

            SendRequest(GetOrCreateFlow(hop.Transport,localEP,hop.EndPoint),request);
        }

        /// <summary>
        /// Sends request to the specified flow.
        /// </summary>
        /// <param name="flow">Data flow.</param>
        /// <param name="request">SIP request.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments contains invalid value.</exception>
        public void SendRequest(SIP_Flow flow,SIP_Request request)
        {
            SendRequest(flow,request,null);
        }

        /// <summary>
        /// Sends request to the specified flow.
        /// </summary>
        /// <param name="flow">Data flow.</param>
        /// <param name="request">SIP request.</param>
        /// <param name="transaction">Owner client transaction or null if stateless sending.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments contains invalid value.</exception>
        internal void SendRequest(SIP_Flow flow,SIP_Request request,SIP_ClientTransaction transaction)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(request.Via.GetTopMostValue() == null){
                throw new ArgumentException("Argument 'request' doesn't contain required Via: header field.");
            }

            // Set sent-by
            SIP_t_ViaParm via = request.Via.GetTopMostValue();
            via.ProtocolTransport = flow.Transport;
            // Via sent-by is used only to send responses when request maker data flow is not active.
            // Normally this never used, so just report first local listening point as sent-by.
            HostEndPoint sentBy = null;
            foreach(IPBindInfo bind in this.BindInfo){
                if(flow.Transport == SIP_Transport.UDP && bind.Protocol == BindInfoProtocol.UDP){
                    if(!string.IsNullOrEmpty(bind.HostName)){
                        sentBy = new HostEndPoint(bind.HostName,bind.Port);
                    }
                    else{
                        sentBy = new HostEndPoint(flow.LocalEP.Address.ToString(),bind.Port);
                    }
                    break;
                }
                else if(flow.Transport == SIP_Transport.TLS && bind.Protocol == BindInfoProtocol.TCP && bind.SslMode == SslMode.SSL){
                    if(!string.IsNullOrEmpty(bind.HostName)){
                        sentBy = new HostEndPoint(bind.HostName,bind.Port);
                    }
                    else{
                        sentBy = new HostEndPoint(flow.LocalEP.Address.ToString(),bind.Port);
                    }
                    break;
                }
                else if(flow.Transport == SIP_Transport.TCP && bind.Protocol == BindInfoProtocol.TCP){
                    if(!string.IsNullOrEmpty(bind.HostName)){
                        sentBy = new HostEndPoint(bind.HostName,bind.Port);
                    }
                    else{
                        sentBy = new HostEndPoint(flow.LocalEP.Address.ToString(),bind.Port);
                    }
                    break;
                }
            }
            // No local end point for sent-by, just use flow local end point for it.
            if(sentBy == null){
                via.SentBy = new HostEndPoint(flow.LocalEP);
            }
            else{
                via.SentBy = sentBy;
            }

            // Send request.
            flow.Send(request);

            // Log.
            if(m_pStack.Logger != null){
                byte[] requestData = request.ToByteData();

                m_pStack.Logger.AddWrite(
                    Guid.NewGuid().ToString(),
                    null,
                    0,
                    "Request [" + (transaction == null ? "" : "transactionID='" + transaction.ID + "';") + "method='" + request.RequestLine.Method + "'; cseq='" + request.CSeq.SequenceNumber + "'; " + 
                    "transport='" + flow.Transport + "'; size='" + requestData.Length + "'; sent '" + flow.LocalEP + "' -> '" + flow.RemoteEP + "'.",
                    flow.LocalEP,
                    flow.RemoteEP,
                    requestData
                );
            }
        }

        #endregion
                
        #region method SendResponse

        /// <summary>
        /// Sends specified response back to request maker using RFC 3261 18. rules.
        /// </summary>
        /// <param name="response">SIP response.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when stack ahs not been started and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SIP_TransportException">Is raised when <b>response</b> sending has failed.</exception>
        /// <remarks>Use this method to send SIP responses from stateless SIP elements, like stateless proxy. 
        /// Otherwise SIP_ServerTransaction.SendResponse method should be used.</remarks>
        public void SendResponse(SIP_Response response)
        {
            SendResponse(response,null);
        }

        /// <summary>
        /// Sends specified response back to request maker using RFC 3261 18. rules.
        /// </summary>
        /// <param name="response">SIP response.</param>
        /// <param name="localEP">Local IP end point to use for sending resposne. Value null means system will allocate it.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when stack ahs not been started and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SIP_TransportException">Is raised when <b>response</b> sending has failed.</exception>
        /// <remarks>Use this method to send SIP responses from stateless SIP elements, like stateless proxy. 
        /// Otherwise SIP_ServerTransaction.SendResponse method should be used.</remarks>
        public void SendResponse(SIP_Response response,IPEndPoint localEP)
        {
            // NOTE: all  paramter / state validations are done in SendResponseInternal.

            SendResponseInternal(null,response,localEP);
        }

        #endregion
    

        #region method SendResponse

        /// <summary>
        /// Sends specified response back to request maker using RFC 3261 18. rules.
        /// </summary>
        /// <param name="transaction">SIP server transaction which response to send.</param>
        /// <param name="response">SIP response.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when stack ahs not been started and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>transaction</b> or <b>response</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SIP_TransportException">Is raised when <b>response</b> sending has failed.</exception>
        internal void SendResponse(SIP_ServerTransaction transaction,SIP_Response response)
        {
            if(transaction == null){
                throw new ArgumentNullException("transaction");
            }
            // NOTE: all other paramter / state validations are done in SendResponseInternal.

            SendResponseInternal(transaction,response,null);
        }

        #endregion


        #region method SendResponseInternal

        /// <summary>
        /// Sends response to request maker using RFC 3261 18. rules.
        /// </summary>
        /// <param name="transaction">Owner server transaction. Can be null if stateless response sending.</param>
        /// <param name="response">SIP response to send.</param>
        /// <param name="localEP">Local IP end point to use for sending resposne. Value null means system will allocate it.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when stack ahs not been started and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SIP_TransportException">Is raised when <b>response</b> sending has failed.</exception>
        private void SendResponseInternal(SIP_ServerTransaction transaction,SIP_Response response,IPEndPoint localEP)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!m_IsRunning){
                throw new InvalidOperationException("Stack has not been started.");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }
                        
            /* RFC 3261 18.2.2.
                The server transport uses the value of the top Via header field in
                order to determine where to send a response.  It MUST follow the
                following process:

                    o  If the "sent-protocol" is a reliable transport protocol such as
                       TCP or SCTP, or TLS over those, the response MUST be sent using
                       the existing connection to the source of the original request
                       that created the transaction, if that connection is still open.
                       This requires the server transport to maintain an association
                       between server transactions and transport connections.  If that
                       connection is no longer open, the server SHOULD open a
                       connection to the IP address in the "received" parameter, if
                       present, using the port in the "sent-by" value, or the default
                       port for that transport, if no port is specified.  If that
                       connection attempt fails, the server SHOULD use the procedures
                       in [4] for servers in order to determine the IP address and
                       port to open the connection and send the response to.

                    o  Otherwise, if the Via header field value contains a "maddr"
                       parameter, the response MUST be forwarded to the address listed
                       there, using the port indicated in "sent-by", or port 5060 if
                       none is present.  If the address is a multicast address, the
                       response SHOULD be sent using the TTL indicated in the "ttl"
                       parameter, or with a TTL of 1 if that parameter is not present.

                    o  Otherwise (for unreliable unicast transports), if the top Via
                       has a "received" parameter, the response MUST be sent to the
                       address in the "received" parameter, using the port indicated
                       in the "sent-by" value, or using port 5060 if none is specified
                       explicitly.  If this fails, for example, elicits an ICMP "port
                       unreachable" response, the procedures of Section 5 of [4]
                       SHOULD be used to determine where to send the response.
                  
                    o  Otherwise, if it is not receiver-tagged, the response MUST be
                       sent to the address indicated by the "sent-by" value, using the
                       procedures in Section 5 of [4].
            */

            /* RFC 3581 4. (Adds new processing between RFC 3261 18.2.2. bullet 2 and 3)
                When a server attempts to send a response, it examines the topmost
                Via header field value of that response.  If the "sent-protocol"
                component indicates an unreliable unicast transport protocol, such as
                UDP, and there is no "maddr" parameter, but there is both a
                "received" parameter and an "rport" parameter, the response MUST be
                sent to the IP address listed in the "received" parameter, and the
                port in the "rport" parameter.  The response MUST be sent from the
                same address and port that the corresponding request was received on.
                This effectively adds a new processing step between bullets two and
                three in Section 18.2.2 of SIP [1].

                The response must be sent from the same address and port that the
                request was received on in order to traverse symmetric NATs.  When a
                server is listening for requests on multiple ports or interfaces, it
                will need to remember the one on which the request was received.  For
                a stateful proxy, storing this information for the duration of the
                transaction is not an issue.  However, a stateless proxy does not
                store state between a request and its response, and therefore cannot
                remember the address and port on which a request was received.  To
                properly implement this specification, a stateless proxy can encode
                the destination address and port of a request into the Via header
                field value that it inserts.  When the response arrives, it can
                extract this information and use it to forward the response.
            */

            SIP_t_ViaParm via = response.Via.GetTopMostValue();
            if(via == null){
                throw new ArgumentException("Argument 'response' does not contain required Via: header field.");
            }

            // TODO: If transport is not supported.            
            //throw new SIP_TransportException("Not supported transport '" + via.ProtocolTransport + "'.");

            string logID         = Guid.NewGuid().ToString();
            string transactionID = transaction == null ? "" : transaction.ID;
            
            // Try to get local IP end point which we should use to send response back.            
            if(transaction != null && transaction.Request.LocalEndPoint != null){
                localEP = transaction.Request.LocalEndPoint;
            }

            // TODO: no "localEP" at moment

            // TODO: Stateless should use flowID instead.

            // Our stateless proxy add 'localEP' parameter to Via: if so normally we can get it from there.
            else if(via.Parameters["localEP"] != null){
                localEP = Net_Utils.ParseIPEndPoint(via.Parameters["localEP"].Value);
            }
            
            byte[] responseData = response.ToByteData();

            #region Try existing flow first

            /* First try active flow to send response, thats not 100% as RFC says, but works better in any case.
               RFC says that for TCP and TLS only, we do it for any transport.
            */

            if(transaction != null){
                try{
                    SIP_Flow flow = transaction.Flow;
                    flow.Send(response);

                    if(m_pStack.Logger != null){
                            m_pStack.Logger.AddWrite(
                                logID,
                                null,
                                0,
                                "Response [flowReuse=true; transactionID='" + transactionID + "'; method='" + response.CSeq.RequestMethod + "'; cseq='" + response.CSeq.SequenceNumber + "'; " + 
                                    "transport='" + flow.Transport + "'; size='" + responseData.Length + "'; statusCode='" + response.StatusCode + "'; " + 
                                    "reason='" + response.ReasonPhrase + "'; sent '" + flow.LocalEP + "' -> '" + flow.RemoteEP + "'.",
                                localEP,
                                flow.RemoteEP,
                                responseData
                            );
                        }

                    return;
                }
                catch{
                    // Do nothing, processing will continue.
                }
            }

            #endregion


            #region Reliable TCP,TLS, ...

            if(SIP_Utils.IsReliableTransport(via.ProtocolTransport)){
                // Get original request remote end point.
                IPEndPoint remoteEP = null;
                if(transaction != null && transaction.Request.RemoteEndPoint != null){
                    remoteEP = transaction.Request.RemoteEndPoint;
                }
                else if(via.Received != null){
                    remoteEP = new IPEndPoint(via.Received,via.SentBy.Port == -1 ? 5060 : via.SentBy.Port);
                }
                    
                #region If original request connection alive, use it

                try{
                    SIP_Flow flow = null;

                    // Statefull
                    if(transaction != null){
                        if(transaction.Request.Flow != null && !transaction.Request.Flow.IsDisposed){
                            flow = transaction.Request.Flow;
                        }
                    }
                    // Stateless
                    else{
                        string flowID = via.Parameters["connectionID"].Value;
                        if(flowID != null){
                            flow = m_pFlowManager[flowID];
                        }
                    }

                    if(flow != null){
                        flow.Send(response);

                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddWrite(
                                logID,
                                null,
                                0,
                                "Response [flowReuse=true; transactionID='" + transactionID + "'; method='" + response.CSeq.RequestMethod + "'; cseq='" + response.CSeq.SequenceNumber + "'; " + 
                                    "transport='" + flow.Transport + "'; size='" + responseData.Length + "'; statusCode='" + response.StatusCode + "'; " + 
                                    "reason='" + response.ReasonPhrase + "'; sent '" + flow.RemoteEP + "' -> '" + flow.LocalEP + "'.",
                                localEP,
                                remoteEP,
                                responseData
                            );
                        }

                        return;
                    }
                }
                catch{
                    // Do nothing, processing will continue.
                    // Override RFC, if there is any existing connection and it gives error, try always RFC 3261 18.2.2(recieved) and 3265 5.
                }

                #endregion

                #region Send RFC 3261 18.2.2(recieved)

                if(remoteEP != null){
                    try{
                        SendResponseToHost(logID,transactionID,null,remoteEP.Address.ToString(),remoteEP.Port,via.ProtocolTransport,response);
                    }
                    catch{
                        // Do nothing, processing will continue -> "RFC 3265 5.".
                    }
                }

                #endregion

                #region Send RFC 3265 5.

                SendResponse_RFC_3263_5(logID,transactionID,localEP,response);

                #endregion

            }

            #endregion

            #region UDP Via: maddr parameter

            else if(via.Maddr != null){
                throw new SIP_TransportException("Sending responses to multicast address(Via: 'maddr') is not supported.");
            }

            #endregion

            #region RFC 3581 4. UDP Via: received and rport parameters

            else if(via.Maddr == null && via.Received != null && via.RPort > 0){
                SendResponseToHost(logID,transactionID,localEP,via.Received.ToString(),via.RPort,via.ProtocolTransport,response);
            }

            #endregion

            #region UDP Via: received parameter

            else if(via.Received != null){
                SendResponseToHost(logID,transactionID,localEP,via.Received.ToString(),via.SentByPortWithDefault,via.ProtocolTransport,response);
            }

            #endregion

            #region UDP

            else{
                SendResponse_RFC_3263_5(logID,transactionID,localEP,response);
            }

            #endregion

        }

        #endregion

        #region method SendResponse_RFC_3263_5

        /// <summary>
        /// Sends specified response back to request maker using RFC 3263 5. rules.
        /// </summary>
        /// <param name="logID">Log ID.</param>
        /// <param name="transactionID">Transaction ID. If null, then stateless response sending.</param>
        /// <param name="localEP">UDP local end point to use for sending. If null, system will use default.</param>
        /// <param name="response">SIP response.</param>
        /// <exception cref="SIP_TransportException">Is raised when <b>response</b> sending has failed.</exception>
        private void SendResponse_RFC_3263_5(string logID,string transactionID,IPEndPoint localEP,SIP_Response response)
        {
            /* RFC 3263 5.
                    RFC 3261 [1] defines procedures for sending responses from a server
                    back to the client.  Typically, for unicast UDP requests, the
                    response is sent back to the source IP address where the request came
                    from, using the port contained in the Via header.  For reliable
                    transport protocols, the response is sent over the connection the
                    request arrived on.  However, it is important to provide failover
                    support when the client element fails between sending the request and
                    receiving the response.

                    A server, according to RFC 3261 [1], will send a response on the
                    connection it arrived on (in the case of reliable transport
                    protocols), and for unreliable transport protocols, to the source
                    address of the request, and the port in the Via header field.  The
                    procedures here are invoked when a server attempts to send to that
                    location and that response fails (the specific conditions are
                    detailed in RFC 3261). "Fails" is defined as any closure of the
                    transport connection the request came in on before the response can
                    be sent, or communication of a fatal error from the transport layer.

                    In these cases, the server examines the value of the sent-by
                    construction in the topmost Via header.  If it contains a numeric IP
                    address, the server attempts to send the response to that address,
                    using the transport protocol from the Via header, and the port from
                    sent-by, if present, else the default for that transport protocol.
                    The transport protocol in the Via header can indicate "TLS", which
                    refers to TLS over TCP.  When this value is present, the server MUST
                    use TLS over TCP to send the response.
                 
                    If, however, the sent-by field contained a domain name and a port
                    number, the server queries for A or AAAA records with that name.  It
                    tries to send the response to each element on the resulting list of
                    IP addresses, using the port from the Via, and the transport protocol
                    from the Via (again, a value of TLS refers to TLS over TCP).  As in
                    the client processing, the next entry in the list is tried if the one
                    before it results in a failure.

                    If, however, the sent-by field contained a domain name and no port,
                    the server queries for SRV records at that domain name using the
                    service identifier "_sips" if the Via transport is "TLS", "_sip"
                    otherwise, and the transport from the topmost Via header ("TLS"
                    implies that the transport protocol in the SRV query is TCP).  The
                    resulting list is sorted as described in [2], and the response is
                    sent to the topmost element on the new list described there.  If that
                    results in a failure, the next entry on the list is tried.
                */

            SIP_t_ViaParm via = response.Via.GetTopMostValue();

            #region Sent-By is IP address
            
            if(via.SentBy.IsIPAddress){
                SendResponseToHost(logID,transactionID,localEP,via.SentBy.Host,via.SentByPortWithDefault,via.ProtocolTransport,response);
            }

            #endregion

            #region Sent-By is host name with port number

            else if(via.SentBy.Port != -1){
                SendResponseToHost(logID,transactionID,localEP,via.SentBy.Host,via.SentByPortWithDefault,via.ProtocolTransport,response);
            }

            #endregion

            #region Sent-By is just host name

            else{
                try{
                    // Query SRV records.
                    string srvQuery = "";
                    if(via.ProtocolTransport == SIP_Transport.UDP){
                        srvQuery = "_sip._udp." + via.SentBy.Host;
                    }
                    else if(via.ProtocolTransport == SIP_Transport.TCP){
                        srvQuery = "_sip._tcp." + via.SentBy.Host;
                    }
                    else if(via.ProtocolTransport == SIP_Transport.UDP){
                        srvQuery = "_sips._tcp." + via.SentBy.Host;
                    }
                    DnsServerResponse dnsResponse = m_pStack.Dns.Query(srvQuery,DNS_QType.SRV);
                    if(dnsResponse.ResponseCode != DNS_RCode.NO_ERROR){
                        throw new SIP_TransportException("Dns error: " + dnsResponse.ResponseCode.ToString());
                    }
                    DNS_rr_SRV[] srvRecords = dnsResponse.GetSRVRecords();

                    // Use SRV records.
                    if(srvRecords.Length > 0){
                        for(int i=0;i<srvRecords.Length;i++){
                            DNS_rr_SRV srv = srvRecords[i];
                            try{
                                if(m_pStack.Logger != null){
                                    m_pStack.Logger.AddText(logID,"Starts sending response to DNS SRV record '" + srv.Target + "'.");
                                }

                                SendResponseToHost(logID,transactionID,localEP,srv.Target,srv.Port,via.ProtocolTransport,response);
                            }
                            catch{
                                // Generate error, all SRV records has failed.
                                if(i == (srvRecords.Length - 1)){
                                    if(m_pStack.Logger != null){
                                        m_pStack.Logger.AddText(logID,"Failed to send response to DNS SRV record '" + srv.Target + "'.");
                                    }

                                    throw new SIP_TransportException("Host '" + via.SentBy.Host + "' is not accessible.");
                                }
                                // For loop will try next SRV record.
                                else{
                                    if(m_pStack.Logger != null){
                                        m_pStack.Logger.AddText(logID,"Failed to send response to DNS SRV record '" + srv.Target + "', will try next.");
                                    }
                                }
                            }
                        }
                    }
                    // If no SRV, use A and AAAA records. (Thats not in 3263 5., but we need to todo it so.)
                    else{
                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddText(logID,"No DNS SRV records found, starts sending to Via: sent-by host '" + via.SentBy.Host + "'.");
                        }

                        SendResponseToHost(logID,transactionID,localEP,via.SentBy.Host,via.SentByPortWithDefault,via.ProtocolTransport,response);
                    }
                }
                catch(DNS_ClientException dnsX){
                    throw new SIP_TransportException("Dns error: " + dnsX.ErrorCode.ToString());
                }
            }

            #endregion
        }

        #endregion

        #region method SendResponseToHost

        /// <summary>
        /// Sends response to the specified host.
        /// </summary>
        /// <param name="logID">Log ID.</param>
        /// <param name="transactionID">Transaction ID. If null, then stateless response sending.</param>
        /// <param name="localEP">UDP local end point to use for sending. If null, system will use default.</param>
        /// <param name="host">Host name or IP address where to send response.</param>
        /// <param name="port">Target host port.</param>
        /// <param name="transport">SIP transport to use.</param>
        /// <param name="response">SIP response to send.</param>
        private void SendResponseToHost(string logID,string transactionID,IPEndPoint localEP,string host,int port,string transport,SIP_Response response)
        {
            try{
                IPAddress[] targets = null;
                if(Net_Utils.IsIPAddress(host)){
                    targets = new IPAddress[]{IPAddress.Parse(host)};
                }
                else{
                    targets = m_pStack.Dns.GetHostAddresses(host);
                    if(targets.Length == 0){
                        throw new SIP_TransportException("Invalid Via: Sent-By host name '" + host + "' could not be resolved.");
                    }
                }

                byte[] responseData = response.ToByteData();

                for(int i=0;i<targets.Length;i++){
                    IPEndPoint remoteEP = new IPEndPoint(targets[i],port);
                    try{                         
                        SIP_Flow flow = GetOrCreateFlow(transport,localEP,remoteEP);
                        flow.Send(response);
                        // localEP = flow.LocalEP;
                       
                        if(m_pStack.Logger != null){
                            m_pStack.Logger.AddWrite(
                                logID,
                                null,
                                0,
                                "Response [transactionID='" + transactionID + "'; method='" + response.CSeq.RequestMethod + "'; cseq='" + response.CSeq.SequenceNumber + "'; " + 
                                    "transport='" + transport + "'; size='" + responseData.Length + "'; statusCode='" + response.StatusCode + "'; " + 
                                    "reason='" + response.ReasonPhrase + "'; sent '" + localEP + "' -> '" + remoteEP + "'.",
                                localEP,
                                remoteEP,
                                responseData
                            );
                        }

                        // If we reach so far, send succeeded.
                        return;
                    }
                    catch{
                        // Generate error, all IP addresses has failed.
                        if(i == (targets.Length - 1)){
                            if(m_pStack.Logger != null){
                                m_pStack.Logger.AddText(logID,"Failed to send response to host '" + host + "' IP end point '" + remoteEP + "'.");
                            }

                            throw new SIP_TransportException("Host '" + host + ":" + port + "' is not accessible.");
                        }
                        // For loop will try next IP address.
                        else{
                            if(m_pStack.Logger != null){
                                m_pStack.Logger.AddText(logID,"Failed to send response to host '" + host + "' IP end point '" + remoteEP + "', will try next A record.");
                            }
                        }
                    }
                }
            }
            catch(DNS_ClientException dnsX){
                throw new SIP_TransportException("Dns error: " + dnsX.ErrorCode.ToString());
            }
        }

        #endregion

// REMOVE ME:
        #region method Resolve
        /*
        /// <summary>
        /// Resolves data flow local NATed IP end point to public IP end point.
        /// </summary>
        /// <param name="flow">Data flow.</param>
        /// <returns>Returns public IP end point of local NATed IP end point.</returns>
        /// <exception cref="ArgumentNullException">Is raised <b>flow</b> is null reference.</exception>
        internal IPEndPoint Resolve(SIP_Flow flow)
        {
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
        
            IPEndPoint resolvedEP = null;
            AutoResetEvent completionWaiter = new AutoResetEvent(false);
            // Create OPTIONS request
            SIP_Request optionsRequest = m_pStack.CreateRequest(SIP_Methods.OPTIONS,new SIP_t_NameAddress("sip:ping@publicIP.com"),new SIP_t_NameAddress("sip:ping@publicIP.com"));
            optionsRequest.MaxForwards = 0;
            SIP_ClientTransaction optionsTransaction = m_pStack.TransactionLayer.CreateClientTransaction(flow,optionsRequest,true);
            optionsTransaction.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(delegate(object s,SIP_ResponseReceivedEventArgs e){
                SIP_t_ViaParm via = e.Response.Via.GetTopMostValue();       
                
                resolvedEP = new IPEndPoint(via.Received == null ? flow.LocalEP.Address : via.Received,via.RPort > 0 ? via.RPort : flow.LocalEP.Port);

                completionWaiter.Set();
            });
            optionsTransaction.StateChanged += new EventHandler(delegate(object s,EventArgs e){
                if(optionsTransaction.State == SIP_TransactionState.Terminated){                 
                    completionWaiter.Set();
                }
            });
            optionsTransaction.Start();
                 
            // Wait OPTIONS request to complete.
            completionWaiter.WaitOne();
            completionWaiter.Close();

            if(resolvedEP != null){
                return resolvedEP;
            }
            else{
                return flow.LocalEP;
            }
        }
*/
        #endregion

        #region method GetContactHost

        /// <summary>
        /// Gets contact URI <b>host</b> parameter suitable to the specified flow.
        /// </summary>
        /// <param name="flow">Data flow.</param>
        /// <returns>Returns contact URI <b>host</b> parameter suitable to the specified flow.</returns>
        internal HostEndPoint GetContactHost(SIP_Flow flow)
        {
            if(flow == null){
                throw new ArgumentNullException("flow");
            }

            HostEndPoint retVal = null;

            // Find suitable listening point for flow.
            foreach(IPBindInfo bind in this.BindInfo){
                if(bind.Protocol == BindInfoProtocol.UDP && flow.Transport == SIP_Transport.UDP){
                    // For UDP flow localEP is also listeining EP, so use it.
                    if(bind.IP.AddressFamily == flow.LocalEP.AddressFamily && bind.Port == flow.LocalEP.Port){
                        retVal = new HostEndPoint((string.IsNullOrEmpty(bind.HostName) ? flow.LocalEP.Address.ToString() : bind.HostName),bind.Port);
                        break;
                    }
                }
                else if(bind.Protocol == BindInfoProtocol.TCP && bind.SslMode == SslMode.SSL && flow.Transport == SIP_Transport.TLS){
                    // Just use first matching listening point.
                    //   TODO: Probably we should imporve it with load-balanched local end point.
                    if(bind.IP.AddressFamily == flow.LocalEP.AddressFamily){
                        if(bind.IP == IPAddress.Any || bind.IP == IPAddress.IPv6Any){
                            retVal = new HostEndPoint((string.IsNullOrEmpty(bind.HostName) ? flow.LocalEP.Address.ToString() : bind.HostName),bind.Port);
                        }
                        else{
                            retVal = new HostEndPoint((string.IsNullOrEmpty(bind.HostName) ? bind.IP.ToString() : bind.HostName),bind.Port);
                        }
                        break;
                    }
                }
                else if(bind.Protocol == BindInfoProtocol.TCP && flow.Transport == SIP_Transport.TCP){
                    // Just use first matching listening point.
                    //   TODO: Probably we should imporve it with load-balanched local end point.
                    if(bind.IP.AddressFamily == flow.LocalEP.AddressFamily){
                        if(bind.IP.Equals(IPAddress.Any) || bind.IP.Equals(IPAddress.IPv6Any)){
                            retVal = new HostEndPoint((string.IsNullOrEmpty(bind.HostName) ? flow.LocalEP.Address.ToString() : bind.HostName),bind.Port);
                        }
                        else{
                            retVal = new HostEndPoint((string.IsNullOrEmpty(bind.HostName) ? bind.IP.ToString() : bind.HostName),bind.Port);
                        }
                        break;
                    }
                }
            }

            // We don't have suitable listening point for active flow.
            // RFC 3261 forces to have, but for TCP based protocls + NAT, server can't connect to use anyway, 
            // so just ignore it and report flow local EP.
            if(retVal == null){
                retVal = new HostEndPoint(flow.LocalEP);
            }

            // If flow remoteEP is public IP and our localEP is private IP, resolve localEP to public.
            if(retVal.IsIPAddress && Net_Utils.IsPrivateIP(IPAddress.Parse(retVal.Host)) && !Net_Utils.IsPrivateIP(flow.RemoteEP.Address)){
                retVal = new HostEndPoint(flow.LocalPublicEP);
            }

            return retVal;
        }

        #endregion

        #region method GetRecordRoute

        /// <summary>
        /// Gets Record-Route for the specified transport.
        /// </summary>
        /// <param name="transport">SIP transport.</param>
        /// <returns>Returns Record-Route ro or null if no record route possible.</returns>
        internal string GetRecordRoute(string transport)
        {
            foreach(IPBindInfo bind in m_pBinds){
                if(!string.IsNullOrEmpty(bind.HostName)){
                    if(bind.Protocol == BindInfoProtocol.TCP && bind.SslMode != SslMode.None && transport == SIP_Transport.TLS){
                        return "<sips:" + bind.HostName + ":" + bind.Port + ";lr>";
                    }
                    else if(bind.Protocol == BindInfoProtocol.TCP && transport == SIP_Transport.TCP){
                        return "<sip:" + bind.HostName + ":" + bind.Port + ";lr>";
                    }
                    else if(bind.Protocol == BindInfoProtocol.UDP && transport == SIP_Transport.UDP){
                        return "<sip:" + bind.HostName + ":" + bind.Port + ";lr>";
                    }
                }
            }

            return null;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if transport layer is running.
        /// </summary>
        public bool IsRunning
        {
            get{ return m_IsRunning; }
        }

        /// <summary>
        /// Gets owner SIP stack.
        /// </summary>
        public SIP_Stack Stack
        {
            get{ return m_pStack; }
        }

        /// <summary>
        /// Gets or sets socket bind info. Use this property to specify on which protocol,IP,port server 
        /// listnes and also if connections is SSL.
        /// </summary>
        public IPBindInfo[] BindInfo
        {
            
            get{ return m_pBinds; }
            
            set{
                if(value == null){
                    throw new ArgumentNullException("BindInfo");
                }

                //--- See binds has changed --------------
                bool changed = false;
                if(m_pBinds.Length != value.Length){
                    changed = true;
                }
                else{
                    for(int i=0;i<m_pBinds.Length;i++){
                        if(!m_pBinds[i].Equals(value[i])){
                            changed = true;
                            break;
                        }
                    }
                }

                if(changed){
                    m_pBinds = value; 
                    
                    // Create listening points.
                    List<IPEndPoint> udpListeningPoints = new List<IPEndPoint>();
                    List<IPBindInfo> tcpListeningPoints = new List<IPBindInfo>();
                    foreach(IPBindInfo bindInfo in m_pBinds){
                        if(bindInfo.Protocol == BindInfoProtocol.UDP){
                            udpListeningPoints.Add(new IPEndPoint(bindInfo.IP,bindInfo.Port));
                        }
                        else{
                            tcpListeningPoints.Add(bindInfo);
                        }
                    }
                    m_pUdpServer.Bindings = udpListeningPoints.ToArray();
                    m_pTcpServer.Bindings = tcpListeningPoints.ToArray();

                    // Build possible local TCP/TLS IP addresses.
                    foreach(IPEndPoint ep in m_pTcpServer.LocalEndPoints){
                        if(ep.AddressFamily == AddressFamily.InterNetwork){
                            m_pLocalIPv4.Add(ep.Address);
                        }
                        else if(ep.AddressFamily == AddressFamily.InterNetwork){
                            m_pLocalIPv6.Add(ep.Address);
                        }                        
                    }
                }
            }            
        }

        /// <summary>
        /// Gets currently active flows.
        /// </summary>
        public SIP_Flow[] Flows
        {
            get{ return m_pFlowManager.Flows; }
        }


        /// <summary>
        /// Gets UDP server.
        /// </summary>
        internal UDP_Server UdpServer
        {
            get{ return m_pUdpServer; }
        }

        /// <summary>
        /// Gets or sets STUN server name or IP address. This value must be filled if SIP stack is running behind a NAT.
        /// </summary>
        internal string StunServer
        {
            get{ return m_StunServer; }

            set{
                m_StunServer = value;
            }
        }

        #endregion

    }
}
