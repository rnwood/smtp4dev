using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.SIP.Message;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Implements SIP server transaction. Defined in rfc 3261 17.2.
    /// </summary>
    public class SIP_ServerTransaction : SIP_Transaction
    {
        private TimerEx m_pTimer100 = null;
        private TimerEx m_pTimerG   = null;
        private TimerEx m_pTimerH   = null;
        private TimerEx m_pTimerI   = null;
        private TimerEx m_pTimerJ   = null;
        private TimerEx m_pTimerL   = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Owner SIP stack.</param>
        /// <param name="flow">SIP data flow which received request.</param>
        /// <param name="request">SIP request that transaction will handle.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stack</b>,<b>flow</b> or <b>request</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SIP_ServerTransaction(SIP_Stack stack,SIP_Flow flow,SIP_Request request) : base(stack,flow,request)
        {
            // Log
            if(this.Stack.Logger != null){
                this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] created.");
            }

            Start();            
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public override void Dispose()
        {
            lock(this.SyncRoot){
                if(m_pTimer100 != null){
                    m_pTimer100.Dispose();
                    m_pTimer100 = null;
                }
                if(m_pTimerG != null){
                    m_pTimerG.Dispose();
                    m_pTimerG = null;
                }
                if(m_pTimerH != null){
                    m_pTimerH.Dispose();
                    m_pTimerH = null;
                }
                if(m_pTimerI != null){
                    m_pTimerI.Dispose();
                    m_pTimerI = null;
                }
                if(m_pTimerJ != null){
                    m_pTimerJ.Dispose();
                    m_pTimerJ = null;
                }                
                if(m_pTimerL != null){
                    m_pTimerL.Dispose();
                    m_pTimerL = null;
                }
            }
        }

        #endregion


        #region Events handling

        #region method m_pTimer100_Elapsed

        /// <summary>
        /// Is raised when INVITE 100 (Trying) response must be sent if no response sent by transaction user.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimer100_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            lock(this.SyncRoot){
                // RFC 3261 17.2.1. TU didn't generate response in 200 ms, send '100 Trying' to stop request retransmission.
                if(this.State == SIP_TransactionState.Proceeding && this.Responses.Length == 0){
                    /* RFC 3261 17.2.1.
                        The 100 (Trying) response is constructed according to the procedures in Section 8.2.6, except that the
                        insertion of tags in the To header field of the response (when none was present in the request) 
                        is downgraded from MAY to SHOULD NOT.
                     * 
                       RFC 3261 8.2.6.
                        When a 100 (Trying) response is generated, any Timestamp header field present in the request MUST 
                        be copied into this 100 (Trying) response. If there is a delay in generating the response, the UAS
                        SHOULD add a delay value into the Timestamp value in the response. This value MUST contain the difference 
                        between the time of sending of the response and receipt of the request, measured in seconds.
                    */

                    SIP_Response tryingResponse = this.Stack.CreateResponse(SIP_ResponseCodes.x100_Trying,this.Request);
                    if(this.Request.Timestamp != null){
                        tryingResponse.Timestamp = new SIP_t_Timestamp(this.Request.Timestamp.Time,(DateTime.Now - this.CreateTime).Seconds);
                    }

                    try{
                        this.Stack.TransportLayer.SendResponse(this,tryingResponse);
                    }
                    catch(Exception x){
                        OnTransportError(x);
                        SetState(SIP_TransactionState.Terminated);
                        return;
                    }
                }

                if(m_pTimer100 != null){
                    m_pTimer100.Dispose();
                    m_pTimer100 = null;
                }
            }
        }

        #endregion

        #region method m_pTimerG_Elapsed

        /// <summary>
        /// Is raised when INVITE timer G triggered.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerG_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            /* RFC 3261 17.2.1.
                If timer G fires, the response is passed to the transport layer once more for retransmission, and
                timer G is set to fire in MIN(2*T1, T2) seconds.  From then on, when timer G fires, the response is 
                passed to the transport again for transmission, and timer G is reset with a value that doubles, unless
                that value exceeds T2, in which case it is reset with the value of T2.
            */

            lock(this.SyncRoot){
                if(this.State == SIP_TransactionState.Completed){
                    // Log
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer G(INVITE response(3xx - 6xx) retransmission) triggered.");
                    }

                    try{
                        this.Stack.TransportLayer.SendResponse(this,this.FinalResponse);

                        // Update(double current) next transmit time.
                        m_pTimerG.Interval *= Math.Min(m_pTimerG.Interval * 2,SIP_TimerConstants.T2);
                        m_pTimerG.Enabled = true;

                        // Log
                        if(this.Stack.Logger != null){
                            this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=false] timer G(INVITE response(3xx - 6xx) retransmission) updated, will trigger after " + m_pTimerG.Interval + ".");
                        }
                    }
                    catch(Exception x){
                        OnTransportError(x);
                        SetState(SIP_TransactionState.Terminated);
                    }                    
                }
            }
        }

        #endregion

        #region method m_pTimerH_Elapsed

        /// <summary>
        /// Is raised when INVITE timer H triggered.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerH_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            /* RFC 3261 17.2.1.
                If timer H fires while in the "Completed" state, it implies that the
                ACK was never received.  In this case, the server transaction MUST
                transition to the "Terminated" state, and MUST indicate to the TU
                that a transaction failure has occurred.
            */

            lock(this.SyncRoot){
                if(this.State == SIP_TransactionState.Completed){
                    // Log
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer H(INVITE ACK wait) triggered.");
                    }

                    OnTransactionError("ACK was never received.");
                    SetState(SIP_TransactionState.Terminated);
                }
            }
        }

        #endregion

        #region method m_pTimerI_Elapsed

        /// <summary>
        /// Is raised when INVITE timer I triggered.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerI_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            /* RFC 3261 17.2.1.
                Once timer I fires, the server MUST transition to the "Terminated" state.
            */

            lock(this.SyncRoot){
                // Log
                if(this.Stack.Logger != null){
                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer I(INVITE ACK retransmission wait) triggered.");
                }

                SetState(SIP_TransactionState.Terminated);
            }
        }

        #endregion

        #region mehtod m_pTimerJ_Elapsed
        
        /// <summary>
        /// Is raised when INVITE timer J triggered.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerJ_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            /* RFC 3261 172.2.2.
                Timer J fires, at which point it MUST transition to the "Terminated" state.
            */

            lock(this.SyncRoot){
                // Log
                if(this.Stack.Logger != null){
                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer I(Non-INVITE request retransmission wait) triggered.");
                }

                SetState(SIP_TransactionState.Terminated);
            }
        }

        #endregion

        #region method m_pTimerL_Elapsed

        /// <summary>
        /// Is called when INVITE time L triggered.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerL_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            /* RFC 6026 7.1.
                When Timer L fires and the state machine is in the "Accepted" state,
                the machine MUST transition to the "Terminated" state.
            */

            lock(this.SyncRoot){
                // Log
                if(this.Stack.Logger != null){
                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer L(ACK wait) triggered.");
                }

                SetState(SIP_TransactionState.Terminated);
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts transaction processing.
        /// </summary>
        private void Start()
        {
            #region INVITE

            if(this.Method == SIP_Methods.INVITE){
                /* RFC 3261 17.2.1.
                    When a server transaction is constructed for a request, it enters the "Proceeding" state. The server 
                    transaction MUST generate a 100 (Trying) response unless it knows that the TU will generate a provisional 
                    or final response within 200 ms, in which case it MAY generate a 100 (Trying) response.
                */

                SetState(SIP_TransactionState.Proceeding);

                m_pTimer100 = new TimerEx(200,false);
                m_pTimer100.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimer100_Elapsed);
                m_pTimer100.Enabled = true;
            }

            #endregion

            #region Non-INVITE

            else{
                // RFC 3261 17.2.2. The state machine is initialized in the "Trying" state.
                SetState(SIP_TransactionState.Trying);
            }

            #endregion
        }
                
        #endregion


        #region method SendResponse

        /// <summary>
        /// Sends specified response to remote party.
        /// </summary>
        /// <param name="response">SIP response to send.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public void SendResponse(SIP_Response response)
        {
            lock(this.SyncRoot){
                if(this.State == SIP_TransactionState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(response == null){
                    throw new ArgumentNullException("response");
                }

                try{
                    #region INVITE

                    /* RFC 6026 7.1. INVITE server transaction. (Udpates RFC 3261)   
                     
                                                             |INVITE
                                                             |pass INV to TU
                                          INVITE             V send 100 if TU won't in 200 ms
                                          send response+------------+
                                              +--------|            |--------+ 101-199 from TU
                                              |        |            |        | send response
                                              +------->|            |<-------+
                                                       | Proceeding |
                                                       |            |--------+ Transport Err.
                                                       |            |        | Inform TU
                                                       |            |<-------+
                                                       +------------+
                                          300-699 from TU |    |2xx from TU
                                          send response   |    |send response
                                           +--------------+    +------------+
                                           |                                |
                          INVITE           V          Timer G fires         |
                          send response +-----------+ send response         |
                               +--------|           |--------+              |
                               |        |           |        |              |
                               +------->| Completed |<-------+      INVITE  |  Transport Err.
                                        |           |               -       |  Inform TU
                               +--------|           |----+          +-----+ |  +---+
                               |        +-----------+    | ACK      |     | v  |   v
                               |          ^   |          | -        |  +------------+
                               |          |   |          |          |  |            |---+ ACK
                               +----------+   |          |          +->|  Accepted  |   | to TU
                               Transport Err. |          |             |            |<--+
                               Inform TU      |          V             +------------+
                                              |      +-----------+        |  ^     |
                                              |      |           |        |  |     |
                                              |      | Confirmed |        |  +-----+
                                              |      |           |        |  2xx from TU
                                Timer H fires |      +-----------+        |  send response
                                -             |          |                |
                                              |          | Timer I fires  |
                                              |          | -              | Timer L fires
                                              |          V                | -
                                              |        +------------+     |
                                              |        |            |<----+
                                              +------->| Terminated |
                                                       |            |
                                                       +------------+

                    */

                    if(this.Method == SIP_Methods.INVITE){
                        #region Proceeding

                        if(this.State == SIP_TransactionState.Proceeding){
                            AddResponse(response);

                            // 1xx
                            if(response.StatusCodeType == SIP_StatusCodeType.Provisional){
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                            }
                            // 2xx
                            else if(response.StatusCodeType == SIP_StatusCodeType.Success){
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                                SetState(SIP_TransactionState.Accpeted);

                                /* RFC 6025 7.1.
                                    When the "Accepted" state is entered, timer L MUST be set to fire in 64*T1.
                                */
                                m_pTimerL = new TimerEx(64 * SIP_TimerConstants.T1);
                                m_pTimerL.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerL_Elapsed);
                                m_pTimerL.Enabled = true;

                                // Log
                                if(this.Stack.Logger != null){
                                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer L(ACK wait) started, will trigger after " + m_pTimerL.Interval + ".");
                                }
                            }
                            // 3xx - 6xx
                            else{
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                                SetState(SIP_TransactionState.Completed);

                                /* RFC 3261 17.2.1.
                                    For unreliable transports, timer G is set to fire in T1 seconds, and is not set to fire for reliable transports.
                                */
                                if(!this.Flow.IsReliable){
                                    m_pTimerG = new TimerEx(SIP_TimerConstants.T1,false);
                                    m_pTimerG.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerG_Elapsed);
                                    m_pTimerG.Enabled = true;

                                    // Log
                                    if(this.Stack.Logger != null){
                                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer G(INVITE response(3xx - 6xx) retransmission) started, will trigger after " + m_pTimerG.Interval + ".");
                                    }
                                }

                                /* RFC 3261 17.2.1.
                                    When the "Completed" state is entered, timer H MUST be set to fire in 64*T1 seconds for all transports.
                                */
                                m_pTimerH = new TimerEx(64 * SIP_TimerConstants.T1);
                                m_pTimerH.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerH_Elapsed);
                                m_pTimerH.Enabled = true;

                                // Log
                                if(this.Stack.Logger != null){
                                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer H(INVITE ACK wait) started, will trigger after " + m_pTimerH.Interval + ".");
                                }
                            }
                        }

                        #endregion

                        #region Accepted

                        else if(this.State == SIP_TransactionState.Accpeted){
                            this.Stack.TransportLayer.SendResponse(this,response);
                            OnResponseSent(response);
                        }

                        #endregion

                        #region Completed

                        else if(this.State == SIP_TransactionState.Completed){
                            // We do nothing here, we just wait ACK to arrive.
                        }

                        #endregion

                        #region Confirmed

                        else if(this.State == SIP_TransactionState.Confirmed){
                            // We do nothing, just wait ACK retransmissions.
                        }

                        #endregion

                        #region Terminated

                        else if(this.State == SIP_TransactionState.Terminated){
                            // We should never rreach here, but if so, skip it.
                        }

                        #endregion
                    }

                    #endregion

                    #region Non-INVITE

                    /* RFC 3261 17.2.2.
                                              |Request received
                                              |pass to TU
                                              V
                                        +-----------+
                                        |           |
                                        | Trying    |-------------+
                                        |           |             |
                                        +-----------+             |200-699 from TU
                                              |                   |send response
                                              |1xx from TU        |
                                              |send response      |
                                              |                   |
                           Request            V      1xx from TU  |
                           send response+-----------+send response|
                               +--------|           |--------+    |
                               |        | Proceeding|        |    |
                               +------->|           |<-------+    |
                        +<--------------|           |             |
                        |Trnsprt Err    +-----------+             |
                        |Inform TU            |                   |
                        |                     |                   |
                        |                     |200-699 from TU    |
                        |                     |send response      |
                        |  Request            V                   |
                        |  send response+-----------+             |
                        |      +--------|           |             |
                        |      |        | Completed |<------------+
                        |      +------->|           |
                        +<--------------|           |
                        |Trnsprt Err    +-----------+
                        |Inform TU            |
                        |                     |Timer J fires
                        |                     |-
                        |                     |
                        |                     V
                        |               +-----------+
                        |               |           |
                        +-------------->| Terminated|
                                        |           |
                                        +-----------+
                    */

                    else{
                        #region Trying

                        if(this.State == SIP_TransactionState.Trying){
                            AddResponse(response);

                            // 1xx
                            if(response.StatusCodeType == SIP_StatusCodeType.Provisional){
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                                SetState(SIP_TransactionState.Proceeding);
                            }
                            // 2xx - 6xx
                            else{
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                                SetState(SIP_TransactionState.Completed);

                                /* RFC 3261 17.2.2.
                                    When the server transaction enters the "Completed" state, it MUST set
                                    Timer J to fire in 64*T1 seconds for unreliable transports, and zero
                                    seconds for reliable transports.
                                */
                                m_pTimerJ = new TimerEx(64 * SIP_TimerConstants.T1,false);
                                m_pTimerJ.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerJ_Elapsed);
                                m_pTimerJ.Enabled = true;

                                // Log
                                if(this.Stack.Logger != null){
                                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer J(Non-INVITE request retransmission wait) started, will trigger after " + m_pTimerJ.Interval + ".");
                                }
                            }
                        }

                        #endregion

                        #region Proceeding

                        else if(this.State == SIP_TransactionState.Proceeding){
                            AddResponse(response);

                            // 1xx
                            if(response.StatusCodeType == SIP_StatusCodeType.Provisional){
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                            }
                            // 2xx - 6xx
                            else{
                                this.Stack.TransportLayer.SendResponse(this,response);
                                OnResponseSent(response);
                                SetState(SIP_TransactionState.Completed);

                                /* RFC 3261 17.2.2.
                                    When the server transaction enters the "Completed" state, it MUST set
                                    Timer J to fire in 64*T1 seconds for unreliable transports, and zero
                                    seconds for reliable transports.
                                */
                                m_pTimerJ = new TimerEx(64 * SIP_TimerConstants.T1,false);
                                m_pTimerJ.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerJ_Elapsed);
                                m_pTimerJ.Enabled = true;

                                // Log
                                if(this.Stack.Logger != null){
                                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer J(Non-INVITE request retransmission wait) started, will trigger after " + m_pTimerJ.Interval + ".");
                                }
                            }
                        }

                        #endregion

                        #region Completed

                        else if(this.State == SIP_TransactionState.Completed){
                            // Do nothing.
                        }

                        #endregion

                        #region Terminated

                        else if(this.State == SIP_TransactionState.Terminated){
                            // Do nothing.
                        }

                        #endregion
                    }

                    #endregion
                }
                catch(SIP_TransportException x){
                    // Log
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] transport exception: " + x.Message);
                    }

                    OnTransportError(x);
                }
            }
        }
                                                                
        #endregion

        #region method Cancel

        /// <summary>
        /// Cancels current transaction processing and sends '487 Request Terminated'.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when final response is sent and Cancel method is called after it.</exception>
        public override void Cancel()
        {
            lock(this.SyncRoot){
                if(this.State == SIP_TransactionState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(this.FinalResponse != null){
                    throw new InvalidOperationException("Final response is already sent, CANCEL not allowed.");
                }

                try{
                    SIP_Response response = this.Stack.CreateResponse(SIP_ResponseCodes.x487_Request_Terminated,this.Request);
                    this.Stack.TransportLayer.SendResponse(this,response);

                    OnCanceled();
                }
                catch(SIP_TransportException x){
                    // Log
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] transport exception: " + x.Message);
                    }

                    OnTransportError(x);
                    SetState(SIP_TransactionState.Terminated);
                }
            }
        }

        #endregion


        #region method ProcessRequest

        /// <summary>
        /// Processes specified request through this transaction.
        /// </summary>
        /// <param name="flow">SIP data flow.</param>
        /// <param name="request">SIP request.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null reference.</exception>
        internal void ProcessRequest(SIP_Flow flow,SIP_Request request)
        {
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }

            lock(this.SyncRoot){
                if(this.State == SIP_TransactionState.Disposed){
                    return;
                }

                try{
                    // Log
                    if(this.Stack.Logger != null){
                        byte[] requestData = request.ToByteData();

                        this.Stack.Logger.AddRead(
                            Guid.NewGuid().ToString(),
                            null,
                            0,
                            "Request [transactionID='" +  this.ID + "'; method='" + request.RequestLine.Method + "'; cseq='" + request.CSeq.SequenceNumber + "'; " + 
                            "transport='" + flow.Transport + "'; size='" + requestData.Length + "'; received '" + flow.LocalEP + "' <- '" + flow.RemoteEP + "'.",
                            flow.LocalEP,
                            flow.RemoteEP,
                            requestData
                        );
                    }

                    #region INVITE

                    if(this.Method == SIP_Methods.INVITE){
                        #region INVITE

                        if(request.RequestLine.Method == SIP_Methods.INVITE){
                            if(this.State == SIP_TransactionState.Proceeding){
                                /* RFC 3261 17.2.1.
                                    If a request retransmission is received while in the "Proceeding" state, the most recent provisional 
                                    response that was received from the TU MUST be passed to the transport layer for retransmission.
                                */
                                SIP_Response response = this.LastProvisionalResponse;
                                if(response != null){
                                    this.Stack.TransportLayer.SendResponse(this,response);
                                }
                            }
                            else if(this.State == SIP_TransactionState.Completed){
                                /* RFC 3261 17.2.1.
                                    While in the "Completed" state, if a request retransmission is received, the server SHOULD 
                                    pass the response to the transport for retransmission.
                                */                                
                                this.Stack.TransportLayer.SendResponse(this,this.FinalResponse);
                            }
                        }

                        #endregion

                        #region ACK

                        else if(request.RequestLine.Method == SIP_Methods.ACK){
                            #region Accepeted

                            if(this.State == SIP_TransactionState.Accpeted){
                                
                            }

                            #endregion

                            #region Completed

                            else if(this.State == SIP_TransactionState.Completed){
                                /* RFC 3261 17.2.1
                                    If an ACK is received while the server transaction is in the "Completed" state, the server transaction 
                                    MUST transition to the "Confirmed" state.  As Timer G is ignored in this state, any retransmissions of the 
                                    response will cease.
                         
                                    When this state is entered, timer I is set to fire in T4 seconds for unreliable transports, 
                                    and zero seconds for reliable transports.
                                */

                                SetState(SIP_TransactionState.Confirmed);

                                // Stop timers G,H
                                if(m_pTimerG != null){
                                    m_pTimerG.Dispose();
                                    m_pTimerG = null;

                                    // Log
                                    if(this.Stack.Logger != null){
                                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer G(INVITE response(3xx - 6xx) retransmission) stopped.");
                                    }
                                }
                                if(m_pTimerH != null){
                                    m_pTimerH.Dispose();
                                    m_pTimerH = null;

                                    // Log
                                    if(this.Stack.Logger != null){
                                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer H(INVITE ACK wait) stopped.");
                                    }
                                }

                                // Start timer I.
                                m_pTimerI = new TimerEx((flow.IsReliable ? 0 : SIP_TimerConstants.T4),false);
                                m_pTimerI.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerI_Elapsed);
                                // Log
                                if(this.Stack.Logger != null){
                                    this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] timer I(INVITE ACK retransission wait) started, will trigger after " + m_pTimerI.Interval + ".");
                                }
                                m_pTimerI.Enabled = true;
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #endregion

                    #region Non-INVITE

                    else{
                        // Non-INVITE transaction may have only request retransmission requests.
                        if(this.Method == request.RequestLine.Method){
                            if(this.State == SIP_TransactionState.Proceeding){
                                /* RFC 3261 17.2.2.
                                    If a retransmission of the request is received while in the "Proceeding" state, the most
                                    recently sent provisional response MUST be passed to the transport layer for retransmission.
                                */
                                this.Stack.TransportLayer.SendResponse(this,this.LastProvisionalResponse);
                            }
                            else if(this.State == SIP_TransactionState.Completed){
                                /* RFC 3261 17.2.2.
                                    While in the "Completed" state, the server transaction MUST pass the final response to the transport
                                    layer for retransmission whenever a retransmission of the request is received.
                                */
                                this.Stack.TransportLayer.SendResponse(this,this.FinalResponse);
                            }
                        }
                    }

                    #endregion
                }
                catch(SIP_TransportException x){
                    // Log
                    if(this.Stack.Logger != null){
                        this.Stack.Logger.AddText(this.ID,"Transaction [branch='" + this.ID + "';method='" + this.Method + "';IsServer=true] transport exception: " + x.Message);
                    }

                    OnTransportError(x);
                }
            }
        }
                
        #endregion


        #region Properties implementation

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when transaction has sent response to remote party.
        /// </summary>
        public event EventHandler<SIP_ResponseSentEventArgs> ResponseSent = null;

        #region method OnResponseSent

        /// <summary>
        /// Raises <b>ResponseSent</b> event.
        /// </summary>
        /// <param name="response">SIP response.</param>
        private void OnResponseSent(SIP_Response response)
        {
            if(this.ResponseSent != null){
                this.ResponseSent(this,new SIP_ResponseSentEventArgs(this,response));
            }
        }

        #endregion

        /// <summary>
        /// Is raised when transaction has canceled.
        /// </summary>
        public event EventHandler Canceled = null;

        #region method OnCanceled

        /// <summary>
        /// Raises <b>Canceled</b> event.
        /// </summary>
        private void OnCanceled()
        {
            if(this.Canceled != null){
                this.Canceled(this,new EventArgs());
            }
        }

        #endregion

        #endregion

    }
}
