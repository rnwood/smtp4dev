using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Net;
using System.Threading;

using LumiSoft.Net.AUTH;
using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// Implements SIP 'proxy context'. Defined in RFC 3261.
    /// </summary>
    /// <remarks>Proxy context is bridge between caller and calee. 
    /// Proxy context job is to forward request to contact(s) and send received responses back to caller.</remarks>
    public class SIP_ProxyContext : IDisposable
    {
        #region class TargetHandler

        /// <summary>
        /// This class is responsible for sending <b>request</b> to target(HOPs) and processing responses.
        /// </summary>
        private class TargetHandler : IDisposable
        {
            private object                m_pLock               = new object();
            private bool                  m_IsDisposed          = false;
            private bool                  m_IsStarted           = false;
            private SIP_ProxyContext      m_pOwner              = null;
            private SIP_Request           m_pRequest            = null;
            private SIP_Flow              m_pFlow               = null;
            private SIP_Uri               m_pTargetUri          = null;
            private bool                  m_AddRecordRoute      = true;
            private bool                  m_IsRecursed          = false;
            private Queue<SIP_Hop>        m_pHops               = null;
            private SIP_ClientTransaction m_pTransaction        = null;
            private TimerEx               m_pTimerC             = null;
            private bool                  m_HasReceivedResponse = false;
            private bool                  m_IsCompleted         = false;
                                    
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner proxy context.</param>
            /// <param name="flow">Data flow to use for sending. Value null means system will choose it.</param>
            /// <param name="targetUri">Target URI where to send request.</param>
            /// <param name="addRecordRoute">If true, handler will add Record-Route header to forwarded message.</param>
            /// <param name="isRecursed">If true then this target is redirected by proxy context.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> or <b>targetURI</b> is null reference.</exception>
            public TargetHandler(SIP_ProxyContext owner,SIP_Flow flow,SIP_Uri targetUri,bool addRecordRoute,bool isRecursed)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                if(targetUri == null){
                    throw new ArgumentNullException("targetUri");
                }

                m_pOwner         = owner; 
                m_pFlow          = flow;
                m_pTargetUri     = targetUri;
                m_AddRecordRoute = addRecordRoute;
                m_IsRecursed     = isRecursed;
                                
                m_pHops = new Queue<SIP_Hop>();  
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

                    m_pOwner.TargetHandler_Disposed(this);

                    m_pOwner = null;
                    m_pRequest = null;
                    m_pTargetUri = null;
                    m_pHops = null;
                    if(m_pTransaction != null){
                        m_pTransaction.Dispose();
                        m_pTransaction = null;
                    }
                    if(m_pTimerC != null){
                        m_pTimerC.Dispose();
                        m_pTimerC = null;
                    }
                }
            }

            #endregion

            #region method Init

            /// <summary>
            /// Initializes target.
            /// </summary>
            private void Init()
            {
                /* RFC 3261 16.6 Request Forwarding.
                For each target, the proxy forwards the request following these steps:
                    1.  Make a copy of the received request
                    2.  Update the Request-URI
                    3.  Update the Max-Forwards header field
                    4.  Optionally add a Record-route header field value
                    5.  Optionally add additional header fields
                    6.  Postprocess routing information
                    7.  Determine the next-hop address, port, and transport
                */

                bool isStrictRoute = false;

                #region 1. Make a copy of the received request.

                // 1. Make a copy of the received request.
                m_pRequest = m_pOwner.Request.Copy();

                #endregion

                #region 2. Update the Request-URI.

                // 2. Update the Request-URI.
                m_pRequest.RequestLine.Uri = m_pTargetUri;

                #endregion

                #region 3. Update the Max-Forwards header field.

                // 3. Update the Max-Forwards header field.
                m_pRequest.MaxForwards--;

                #endregion
                                
                #region 5. Optionally add additional header fields.

                // 5. Optionally add additional header fields.
                //    Skip.

                #endregion

                #region 6. Postprocess routing information.

                /* 6. Postprocess routing information.
             
                    If the copy contains a Route header field, the proxy MUST inspect the URI in its first value.  
                    If that URI does not contain an lr parameter, the proxy MUST modify the copy as follows:             
                        - The proxy MUST place the Request-URI into the Route header
                          field as the last value.
             
                        - The proxy MUST then place the first Route header field value
                          into the Request-URI and remove that value from the Route header field.
                */
                if(m_pRequest.Route.GetAllValues().Length > 0 && !m_pRequest.Route.GetTopMostValue().Parameters.Contains("lr")){
                    m_pRequest.Route.Add(m_pRequest.RequestLine.Uri.ToString());

                    m_pRequest.RequestLine.Uri = SIP_Utils.UriToRequestUri(m_pRequest.Route.GetTopMostValue().Address.Uri);
                    m_pRequest.Route.RemoveTopMostValue();

                    isStrictRoute = true;
                }

                #endregion

                #region 7. Determine the next-hop address, port, and transport.

                /* 7. Determine the next-hop address, port, and transport.
                      The proxy MAY have a local policy to send the request to a
                      specific IP address, port, and transport, independent of the
                      values of the Route and Request-URI.  Such a policy MUST NOT be
                      used if the proxy is not certain that the IP address, port, and
                      transport correspond to a server that is a loose router.
                      However, this mechanism for sending the request through a
                      specific next hop is NOT RECOMMENDED; instead a Route header
                      field should be used for that purpose as described above.
             
                      In the absence of such an overriding mechanism, the proxy
                      applies the procedures listed in [4] as follows to determine
                      where to send the request.  If the proxy has reformatted the
                      request to send to a strict-routing element as described in
                      step 6 above, the proxy MUST apply those procedures to the
                      Request-URI of the request.  Otherwise, the proxy MUST apply
                      the procedures to the first value in the Route header field, if
                      present, else the Request-URI.  The procedures will produce an
                      ordered set of (address, port, transport) tuples.
                      Independently of which URI is being used as input to the
                      procedures of [4], if the Request-URI specifies a SIPS
                      resource, the proxy MUST follow the procedures of [4] as if the
                      input URI were a SIPS URI.

                      As described in [4], the proxy MUST attempt to deliver the
                      message to the first tuple in that set, and proceed through the
                      set in order until the delivery attempt succeeds.

                      For each tuple attempted, the proxy MUST format the message as
                      appropriate for the tuple and send the request using a new
                      client transaction as detailed in steps 8 through 10.
             
                      Since each attempt uses a new client transaction, it represents
                      a new branch.  Thus, the branch parameter provided with the Via
                      header field inserted in step 8 MUST be different for each
                      attempt.

                      If the client transaction reports failure to send the request
                      or a timeout from its state machine, the proxy continues to the
                      next address in that ordered set.  If the ordered set is
                      exhausted, the request cannot be forwarded to this element in
                      the target set.  The proxy does not need to place anything in
                      the response context, but otherwise acts as if this element of
                      the target set returned a 408 (Request Timeout) final response.
                */            
                SIP_Uri uri = null;
                if(isStrictRoute){
                    uri = (SIP_Uri)m_pRequest.RequestLine.Uri;
                }
                else if(m_pRequest.Route.GetTopMostValue() != null){
                    uri = (SIP_Uri)m_pRequest.Route.GetTopMostValue().Address.Uri;
                }
                else{
                    uri = (SIP_Uri)m_pRequest.RequestLine.Uri;
                }
                
                // Queue hops.
                foreach(SIP_Hop hop in m_pOwner.Proxy.Stack.GetHops(uri,m_pRequest.ToByteData().Length,((SIP_Uri)m_pRequest.RequestLine.Uri).IsSecure)){
                    m_pHops.Enqueue(hop);
                }
                
                #endregion

                #region 4. Optionally add a Record-route header field value.

                // We need to do step 4 after step 7, because then transport in known.
                // Each transport can have own host-name, otherwise we don't know what to put into Record-Route.

                /*
                    If the Request-URI contains a SIPS URI, or the topmost Route header
                    field value (after the post processing of bullet 6) contains a SIPS URI,                 
                    the URI placed into the Record-Route header field MUST be a SIPS URI.
                    Furthermore, if the request was not received over TLS, the proxy MUST 
                    insert a Record-Route header field. In a similar fashion, a proxy that 
                    receives a request over TLS, but generates a request without a SIPS URI 
                    in the Request-URI or topmost Route header field value (after the post
                    processing of bullet 6), MUST insert a Record-Route header field that 
                    is not a SIPS URI.
                */
                
                // NOTE: ACK don't add Record-route header.
                if(m_pHops.Count > 0 && m_AddRecordRoute && m_pRequest.RequestLine.Method != SIP_Methods.ACK){
                    string recordRoute = m_pOwner.Proxy.Stack.TransportLayer.GetRecordRoute(m_pHops.Peek().Transport);
                    if(recordRoute != null){
                        m_pRequest.RecordRoute.Add(recordRoute);
                    }
                }

                #endregion

            }

            #endregion


            #region Events handling
                        
            #region method ClientTransaction_ResponseReceived

            /// <summary>
            /// Is called when client transactions receives response.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void ClientTransaction_ResponseReceived(object sender,SIP_ResponseReceivedEventArgs e)
            {
                lock(m_pLock){
                    m_HasReceivedResponse = true;

                    /* RFC 3261 16.7 Response Processing.
                        1.  Find the appropriate response context
                        2.  Update timer C for provisional responses
                 
                        Steps 3 - 10 done in ProxyContext.ProcessResponse method.
                    */

                    #region 1. Find Context

                    // Done, m_pOwner is it.

                    #endregion

                    #region 2. Update timer C for provisional responses

                    /* For an INVITE transaction, if the response is a provisional
                       response with status codes 101 to 199 inclusive (i.e., anything
                       but 100), the proxy MUST reset timer C for that client
                       transaction.  The timer MAY be reset to a different value, but
                       this value MUST be greater than 3 minutes.
                    */
                    if(m_pTimerC != null && e.Response.StatusCode >= 101 && e.Response.StatusCode <= 199){
                        m_pTimerC.Interval = 3 * 60 * 1000;
                    }

                    #endregion

                    /*
                    // If 401 or 407 (Authentication required), see i we have specified realm(s) credentials, 
                    // if so try to authenticate.
                    if(e.Response.StatusCode == 401 || e.Response.StatusCode == 407){
                        SIP_t_Challenge[] challanges = null;
                        if(e.Response.StatusCode == 401){
                            challanges = e.Response.WWWAuthenticate.GetAllValues();
                        }
                        else{
                            challanges = e.Response.ProxyAuthenticate.GetAllValues();
                        }

                        // TODO: Porbably we need to auth only if we can provide authentication data to all realms ?

                        SIP_Request request = m_pServerTransaction.Request.Copy();
                        request.CSeq.SequenceNumber++;
                        bool hasAny = false;
                        foreach(SIP_t_Challenge challange in challanges){
                            Auth_HttpDigest authDigest = new Auth_HttpDigest(challange.AuthData,m_pServerTransaction.Request.Method);
                            NetworkCredential credential = GetCredential(authDigest.Realm);
                            if(credential != null){
                                // Don't authenticate again, if we tried already once and failed.
                                // FIX ME: if user passed authorization, then works wrong.
                                if(e.ClientTransaction.Request.Authorization.Count == 0 && e.ClientTransaction.Request.ProxyAuthorization.Count == 0){
                                    authDigest.RequestMethod = m_pServerTransaction.Request.Method;
                                    authDigest.Uri           = e.ClientTransaction.Request.Uri;
                                    authDigest.Realm         = credential.Domain;
                                    authDigest.UserName      = credential.UserName;
                                    authDigest.Password      = credential.Password;
                                    authDigest.CNonce        = Auth_HttpDigest.CreateNonce();
                                    authDigest.Qop           = authDigest.Qop;
                                    authDigest.Opaque        = authDigest.Opaque;
                                    authDigest.Algorithm     = authDigest.Algorithm;
                                    if(e.Response.StatusCode == 401){
                                        request.Authorization.Add(authDigest.ToAuthorization());
                                    }
                                    else{
                                        request.ProxyAuthorization.Add(authDigest.ToAuthorization());
                                    }
                                    hasAny = true;
                                }
                            }
                        }
                        if(hasAny){
                            // CreateClientTransaction((SIP_Target)e.ClientTransaction.Tag,request);
                            return;
                        }
                    }*/

                    if(e.Response.StatusCodeType != SIP_StatusCodeType.Provisional){
                        m_IsCompleted = true;
                    }
                                
                    m_pOwner.ProcessResponse(this,m_pTransaction,e.Response);                
                }
            }

            #endregion

            #region method ClientTransaction_TimedOut

            /// <summary>
            /// Is called when client transaction has timed out.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void ClientTransaction_TimedOut(object sender,EventArgs e)
            {
                lock(m_pLock){
                    /* RFC 3263 4.3.
                        For SIP requests, failure occurs if the transaction layer reports a
                        503 error response or a transport failure of some sort (generally,
                        due to fatal ICMP errors in UDP or connection failures in TCP).
                        Failure also occurs if the transaction layer times out without ever
                        having received any response, provisional or final (i.e., timer B or
                        timer F in RFC 3261 [1] fires). If a failure occurs, the client
                        SHOULD create a new request, which is identical to the previous, but
                        has a different value of the Via branch ID than the previous (and
                        therefore constitutes a new SIP transaction).  That request is sent
                        to the next element in the list as specified by RFC 2782.
                    */
                    if(m_pHops.Count > 0){
                        CleanUpActiveHop();
                        SendToNextHop();
                    }
                    /* RFC 3161 16.6.7.
                        If the ordered set is exhausted, the request cannot be forwarded to this element in
                        the target set. The proxy does not need to place anything in the response context, 
                        but otherwise acts as if this element of the target set returned a 
                        408 (Request Timeout) final response.
                    */
                    else{
                        m_IsCompleted = true;
                        m_pOwner.ProcessResponse(this,m_pTransaction,m_pOwner.Proxy.Stack.CreateResponse(SIP_ResponseCodes.x408_Request_Timeout,m_pTransaction.Request));
                        Dispose();
                    }
                }
            }

            #endregion

            #region method ClientTransaction_TransportError

            /// <summary>
            /// Is called when client transaction encountered transport error.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void ClientTransaction_TransportError(object sender,ExceptionEventArgs e)
            {
                lock(m_pLock){
                    /* RFC 3263 4.3.
                        For SIP requests, failure occurs if the transaction layer reports a
                        503 error response or a transport failure of some sort (generally,
                        due to fatal ICMP errors in UDP or connection failures in TCP).
                        Failure also occurs if the transaction layer times out without ever
                        having received any response, provisional or final (i.e., timer B or
                        timer F in RFC 3261 [1] fires). If a failure occurs, the client
                        SHOULD create a new request, which is identical to the previous, but
                        has a different value of the Via branch ID than the previous (and
                        therefore constitutes a new SIP transaction).  That request is sent
                        to the next element in the list as specified by RFC 2782.
                    */
                    if(m_pHops.Count > 0){
                        CleanUpActiveHop();
                        SendToNextHop();
                    }
                    /* RFC 3161 16.6.7.
                        If the ordered set is exhausted, the request cannot be forwarded to this element in
                        the target set.  The proxy does not need to place anything in the response context, 
                        but otherwise acts as if this element of the target set returned a 
                        408 (Request Timeout) final response.
                    */
                    else{
                        m_IsCompleted = true;
                        m_pOwner.ProcessResponse(this,m_pTransaction,m_pOwner.Proxy.Stack.CreateResponse(SIP_ResponseCodes.x408_Request_Timeout,m_pTransaction.Request));
                        Dispose();
                    }
                }
            }

            #endregion

            #region method m_pTransaction_Disposed

            /// <summary>
            /// This method is called when client transaction has disposed.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void m_pTransaction_Disposed(object sender,EventArgs e)
            {
                lock(m_pLock){
                    if(m_IsDisposed){
                        return;
                    }

                    // If we have got any response from any client transaction, then all done.
                    if(this.HasReceivedResponse){
                        Dispose();
                    }
                }
            }

            #endregion

            #region method m_pTimerC_Elapsed

            private void m_pTimerC_Elapsed(object sender,ElapsedEventArgs e)
            {
                lock(m_pLock){
                    /* RFC 3261 16.8 Processing Timer C.
                        If the client transaction has received a provisional response, the proxy
                        MUST generate a CANCEL request matching that transaction.  If the client 
                        transaction has not received a provisional response, the proxy MUST behave 
                        as if the transaction received a 408 (Request Timeout) response.
                    */
                                
                    if(m_pTransaction.HasProvisionalResponse){
                        m_pTransaction.Cancel();
                    }
                    else{
                        m_pOwner.ProcessResponse(this,m_pTransaction,m_pOwner.Proxy.Stack.CreateResponse(SIP_ResponseCodes.x408_Request_Timeout,m_pTransaction.Request));
                        Dispose();
                    }
                }
            }

            #endregion

            #endregion


            #region method Start

            /// <summary>
            /// Starts target processing.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when <b>Start</b> method is already called and this method is called.</exception>
            public void Start()
            {
                lock(m_pLock){
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_IsStarted){
                        throw new InvalidOperationException("Start has already called.");
                    }
                    m_IsStarted = true;

                    Init();

                    // No hops for target. Invalid target domain name,dns server down,target won't support needed target.
                    if(m_pHops.Count == 0){
                        m_pOwner.ProcessResponse(this,m_pTransaction,m_pOwner.Proxy.Stack.CreateResponse(SIP_ResponseCodes.x503_Service_Unavailable + ": No hop(s) for target.",m_pTransaction.Request));
                        Dispose();
                        return;
                    }
                    else{
                        if(m_pFlow != null){
                            SendToFlow(m_pFlow,m_pRequest.Copy());
                        }
                        else{
                            SendToNextHop();
                        }
                    }
                }
            }

            #endregion

            #region method Cancel

            /// <summary>
            /// Cancels target processing.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
            public void Cancel()
            {
                lock(m_pLock){
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    if(m_IsStarted){
                        m_pTransaction.Cancel();
                    }
                    else{
                        Dispose();
                    }
                }
            }

            #endregion


            #region method SendToNextHop

            /// <summary>
            /// Starts sending request to next hop in queue.
            /// </summary>
            /// <exception cref="InvalidOperationException">Is raised when no next hop available(m_pHops.Count == 0) and this method is accessed.</exception>
            private void SendToNextHop()
            {
                if(m_pHops.Count == 0){
                    throw new InvalidOperationException("No more hop(s).");
                }

                SIP_Hop hop = m_pHops.Dequeue();
                
                SendToFlow(m_pOwner.Proxy.Stack.TransportLayer.GetOrCreateFlow(hop.Transport,null,hop.EndPoint),m_pRequest.Copy());
            }
                                                            
            #endregion

            #region method SendToFlow

            /// <summary>
            /// Sends specified request to the specified data flow.
            /// </summary>
            /// <param name="flow">SIP data flow.</param>
            /// <param name="request">SIP request to send.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null reference.</exception>
            private void SendToFlow(SIP_Flow flow,SIP_Request request)
            {
                if(flow == null){
                    throw new ArgumentNullException("flow");
                }
                if(request == null){
                    throw new ArgumentNullException("request");
                }

                /* NAT traversal.
                    When we do record routing, store request sender flow info and request target flow info.
                    Now the tricky part, how proxy later which flow is target (because both sides can send requests).
                      Sender-flow will store from-tag to flow and target-flow will store flowID only (Because we don't know to-tag).
                      Later if request to-tag matches(incoming request), use that flow, otherwise(outgoing request) other flow.
                 
                    flowInfo: sender-flow "/" target-flow
                              sender-flow = from-tag ":" flowID
                              target-flow = flowID                        
                */
                if(m_AddRecordRoute && request.From.Tag != null && request.RecordRoute.GetAllValues().Length > 0){
                    string flowInfo = request.From.Tag + ":" + m_pOwner.ServerTransaction.Flow.ID + "/" + flow.ID;
                    ((SIP_Uri)request.RecordRoute.GetTopMostValue().Address.Uri).Parameters.Add("flowInfo",flowInfo);
                }

                /* RFC 3261 16.6 Request Forwarding.
                        Common Steps 1 - 7 are done in target Init().
                                                  
                        8.  Add a Via header field value
                        9.  Add a Content-Length header field if necessary
                        10. Forward the new request
                        11. Set timer C
                */
                                
                #region 8.  Add a Via header field value

                // Skip, Client transaction will add it.

                #endregion

                #region 9.  Add a Content-Length header field if necessary

                // Skip, our SIP_Message class is smart and do it when ever it's needed.

                #endregion

                #region 10. Forward the new request
                                
                m_pTransaction = m_pOwner.Proxy.Stack.TransactionLayer.CreateClientTransaction(flow,request,true);             
                m_pTransaction.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(ClientTransaction_ResponseReceived);
                m_pTransaction.TimedOut += new EventHandler(ClientTransaction_TimedOut);
                m_pTransaction.TransportError += new EventHandler<ExceptionEventArgs>(ClientTransaction_TransportError);
                m_pTransaction.Disposed += new EventHandler(m_pTransaction_Disposed);

                // Start transaction processing.
                m_pTransaction.Start();

                #endregion

                #region 11. Set timer C

                /* 11. Set timer C
                    In order to handle the case where an INVITE request never
                    generates a final response, the TU uses a timer which is called
                    timer C.  Timer C MUST be set for each client transaction when
                    an INVITE request is proxied.  The timer MUST be larger than 3
                    minutes.  Section 16.7 bullet 2 discusses how this timer is
                    updated with provisional responses, and Section 16.8 discusses
                    processing when it fires.
                */                
                if(request.RequestLine.Method == SIP_Methods.INVITE){
                    m_pTimerC = new TimerEx();
                    m_pTimerC.AutoReset = false;
                    m_pTimerC.Interval = 3 * 60 * 1000;
                    m_pTimerC.Elapsed += new ElapsedEventHandler(m_pTimerC_Elapsed);
                }

                #endregion
            }

            #endregion

            #region method CleanUpActiveHop

            /// <summary>
            /// Cleans up acitve hop resources.
            /// </summary>
            private void CleanUpActiveHop()
            {
                if(m_pTimerC != null){
                    m_pTimerC.Dispose();
                    m_pTimerC = null;
                }
                if(m_pTransaction != null){
                    m_pTransaction.Dispose();
                    m_pTransaction = null;
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
            /// Gets if this target processing has been started.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public bool IsStarted
            {
                get{ 
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsStarted; 
                }
            }

            /// <summary>
            /// Gets if request sender has completed.
            /// </summary>
            public bool IsCompleted
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompleted; 
                }
            }

            /// <summary>
            /// Gets SIP request what this <b>Target</b> is sending.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public SIP_Request Request
            {
                get{  
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pRequest; 
                }
            }

            /// <summary>
            /// Gets target URI where request is being sent.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public SIP_Uri TargetUri
            {
                get{  
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pTargetUri; 
                }
            }
            
            /// <summary>
            /// Gets if this target is recording routing(By adding Record-Route header field to forwarded requests).
            /// </summary>
            public bool IsRecordingRoute
            {
                get{ return m_AddRecordRoute; }
            }

            /// <summary>
            /// Gets if this target is redirected by proxy context.
            /// </summary>
            public bool IsRecursed
            {
                get{ return m_IsRecursed; }
            }

            /// <summary>
            /// Gets if this handler has received any response from target.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public bool HasReceivedResponse
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_HasReceivedResponse; 
                }
            }

            #endregion

        }

        #endregion
        
        private bool                        m_IsDisposed          = false;
        private bool                        m_IsStarted           = false;
        private SIP_Proxy                   m_pProxy              = null;
        private SIP_ServerTransaction       m_pServerTransaction  = null;
        private SIP_Request                 m_pRequest            = null;
        private bool                        m_AddRecordRoute      = false;
        private SIP_ForkingMode             m_ForkingMode         = SIP_ForkingMode.Parallel;
        private bool                        m_IsB2BUA             = true;
        private bool                        m_NoCancel            = false;
        private bool                        m_NoRecurse           = true;
        private string                      m_ID                  = "";
        private DateTime                    m_CreateTime;
        private List<TargetHandler>         m_pTargetsHandlers    = null;
        private List<SIP_Response>          m_pResponses          = null;
        private Queue<TargetHandler>        m_pTargets            = null;
        private List<NetworkCredential>     m_pCredentials        = null;        
        private bool                        m_IsFinalResponseSent = false;
        private object                      m_pLock               = new object();

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="proxy">Owner proxy.</param>
        /// <param name="transaction">Server transaction what is used to send SIP responses back to caller.</param>
        /// <param name="request">Request to forward.</param>
        /// <param name="addRecordRoute">If true, Record-Route header field will be added.</param>
        /// <param name="forkingMode">Specifies how proxy context must handle forking.</param>
        /// <param name="isB2BUA">Specifies if proxy context is in B2BUA or just transaction satefull mode.</param>
        /// <param name="noCancel">Specifies if proxy should not send Cancel to forked requests.</param>
        /// <param name="noRecurse">Specifies what proxy server does when it gets 3xx response. If true proxy will forward
        /// request to new specified address if false, proxy will return 3xx response to caller.</param>
        /// <param name="targets">Possible remote targets. NOTE: These values must be in priority order !</param>
        /// <exception cref="ArgumentNullException">Is raised when any of the reference type prameters is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal SIP_ProxyContext(SIP_Proxy proxy,SIP_ServerTransaction transaction,SIP_Request request,bool addRecordRoute,SIP_ForkingMode forkingMode,bool isB2BUA,bool noCancel,bool noRecurse,SIP_ProxyTarget[] targets)
        {
            if(proxy == null){
                throw new ArgumentNullException("proxy");
            }
            if(transaction == null){
                throw new ArgumentNullException("transaction");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(targets == null){
                throw new ArgumentNullException("targets");
            }
            if(targets.Length == 0){
                throw new ArgumentException("Argumnet 'targets' must contain at least 1 value.");
            }

            m_pProxy = proxy;

            m_pServerTransaction = transaction;
            m_pServerTransaction.Canceled += new EventHandler(m_pServerTransaction_Canceled);
            m_pServerTransaction.Disposed += new EventHandler(m_pServerTransaction_Disposed);

            m_pRequest       = request;
            m_AddRecordRoute = addRecordRoute;
            m_ForkingMode    = forkingMode;
            m_IsB2BUA        = isB2BUA;
            m_NoCancel       = noCancel;
            m_NoRecurse      = noRecurse;

            m_pTargetsHandlers = new List<TargetHandler>();
            m_pResponses       = new List<SIP_Response>();
            m_ID               = Guid.NewGuid().ToString();
            m_CreateTime       = DateTime.Now;

            // Queue targets up, higest to lowest.
            m_pTargets = new Queue<TargetHandler>();
            foreach(SIP_ProxyTarget target in targets){
                m_pTargets.Enqueue(new TargetHandler(this,target.Flow,target.TargetUri,m_AddRecordRoute,false));
            }

            m_pCredentials = new List<NetworkCredential>();

            /*  RFC 3841 9.1.
                The Request-Disposition header field specifies caller preferences for
                how a server should process a request.
              
                Override SIP proxy default value.
            */
            foreach(SIP_t_Directive directive in request.RequestDisposition.GetAllValues()){
                if(directive.Directive == SIP_t_Directive.DirectiveType.NoFork){
                    m_ForkingMode = SIP_ForkingMode.None;
                }
                else if(directive.Directive == SIP_t_Directive.DirectiveType.Parallel){
                    m_ForkingMode = SIP_ForkingMode.Parallel;
                }
                else if(directive.Directive == SIP_t_Directive.DirectiveType.Sequential){
                    m_ForkingMode = SIP_ForkingMode.Sequential;
                }                    
                else if(directive.Directive == SIP_t_Directive.DirectiveType.NoCancel){
                    m_NoCancel = true;
                }                    
                else if(directive.Directive == SIP_t_Directive.DirectiveType.NoRecurse){
                    m_NoRecurse = true;
                }
            }

            m_pProxy.Stack.Logger.AddText("ProxyContext(id='" + m_ID + "') created.");
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

                m_pProxy.Stack.Logger.AddText("ProxyContext(id='" + m_ID + "') disposed.");

                m_pProxy.m_pProxyContexts.Remove(this);

                m_pProxy             = null;
                m_pServerTransaction = null;
                m_pTargetsHandlers   = null;
                m_pResponses         = null;
                m_pTargets           = null;
            }
        }

        #endregion


        #region Events Handling

        #region method m_pServerTransaction_Canceled

        /// <summary>
        /// Is called when server transaction has canceled.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pServerTransaction_Canceled(object sender,EventArgs e)
        {
            lock(m_pLock){
                CancelAllTargets();

                // We dont need to Dispose proxy context, server transaction will call Terminated event
                // after cancel, there we dispose it.
            }
        }

        #endregion

        #region method m_pServerTransaction_Disposed

        /// <summary>
        /// This method is called when server transaction has disposed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pServerTransaction_Disposed(object sender,EventArgs e)
        {
            // All done, just dispose proxy context.
            Dispose();
        }

        #endregion

        #region method TargetHandler_Disposed

        /// <summary>
        /// This method is called when specified target handler has disposed.
        /// </summary>
        /// <param name="handler">TargetHandler what disposed.</param>
        private void TargetHandler_Disposed(TargetHandler handler)
        {
            lock(m_pLock){
                m_pTargetsHandlers.Remove(handler);

                // All targets are processed.
                if(m_pTargets.Count == 0 && m_pTargetsHandlers.Count == 0){
                    Dispose();
                }
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts processing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is called more than once.</exception>
        public void Start()
        {
            lock(m_pLock){
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(m_IsStarted){
                    throw new InvalidOperationException("Start has already called.");
                }
                m_IsStarted = true;

                // Only use destination with the highest q value.
                // We already have ordered highest to lowest, so just get first destination.
                if(m_ForkingMode == SIP_ForkingMode.None){
                    TargetHandler handler = m_pTargets.Dequeue();
                    m_pTargetsHandlers.Add(handler);
                    handler.Start();
                }
                // Use all destinations at same time.
                else if(m_ForkingMode == SIP_ForkingMode.Parallel){
                    // NOTE: If target count == 1 and transport exception, target may Dispose proxy context., so we need to handle it.
                    while(!m_IsDisposed && m_pTargets.Count > 0){
                        TargetHandler handler = m_pTargets.Dequeue();
                        m_pTargetsHandlers.Add(handler);
                        handler.Start();
                    }
                }
                // Start processing destinations with highest q value to lowest.
                else if(m_ForkingMode == SIP_ForkingMode.Sequential){
                    TargetHandler handler = m_pTargets.Dequeue();
                    m_pTargetsHandlers.Add(handler);
                    handler.Start();
                }
            }
        }

        #endregion

        #region method Cancel

        /// <summary>
        /// Cancels proxy context processing. All client transactions and owner server transaction will be canceled,
        /// proxy context will be disposed. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>Start</b> method is not called and this method is accessed.</exception>
        public void Cancel()
        {
            lock(m_pLock){
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!m_IsStarted){
                    throw new InvalidOperationException("Start method is not called, nothing to cancel.");
                }

                m_pServerTransaction.Cancel();

                // Server transaction raised Cancled event, we dispose active targets there.
            }
        }

        #endregion


        #region method ProcessResponse

        /// <summary>
        /// Processes received response.
        /// </summary>
        /// <param name="handler">Target handler what received response.</param>
        /// <param name="transaction">Client transaction what response it is.</param>
        /// <param name="response">Response received.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>handler</b>,<b>transaction</b> or <b>response</b> is null reference.</exception>
        private void ProcessResponse(TargetHandler handler,SIP_ClientTransaction transaction,SIP_Response response)
        {
            if(handler == null){
                throw new ArgumentNullException("handler");
            }
            if(transaction == null){
                throw new ArgumentNullException("transaction");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3261 16.7 Response Processing.
                Steps 1 - 2 handled in TargetHandler.
             
                3.  Remove the topmost Via
                4.  Add the response to the response context
                5.  Check to see if this response should be forwarded immediately
                6.  When necessary, choose the best final response from the response context.
                    If no final response has been forwarded after every client
                    transaction associated with the response context has been terminated,
                    the proxy must choose and forward the "best" response from those it
                    has seen so far.

                The following processing MUST be performed on each response that is
                forwarded.  It is likely that more than one response to each request
                will be forwarded: at least each provisional and one final response.

                7.  Aggregate authorization header field values if necessary
                8.  Optionally rewrite Record-Route header field values
                9.  Forward the response
                10. Generate any necessary CANCEL requests
            */

            bool forwardResponse = false;

            lock(m_pLock){

                #region 3.  Remove the topmost Via

                /* 
                    The proxy removes the topmost Via header field value from the
                    response.
                 
                    If no Via header field values remain in the response, the
                    response was meant for this element and MUST NOT be forwarded.
                    The remainder of the processing described in this section is
                    not performed on this message, the UAC processing rules
                    described in Section 8.1.3 are followed instead (transport
                    layer processing has already occurred).

                    This will happen, for instance, when the element generates
                    CANCEL requests as described in Section 10.
                 
                    NOTE: We MAY NOT do it for B2BUA, skip it for B2BUA
                */
                if(!m_IsB2BUA){
                    response.Via.RemoveTopMostValue();
                    if(response.Via.GetAllValues().Length == 0){
                        return;
                    }
                }

                #endregion

                #region 4.  Add the response to the response context

                /*
                    Final responses received are stored in the response context
                    until a final response is generated on the server transaction
                    associated with this context.  The response may be a candidate
                    for the best final response to be returned on that server
                    transaction.  Information from this response may be needed in
                    forming the best response, even if this response is not chosen.
 
                    If the proxy chooses to recurse on any contacts in a 3xx
                    response by adding them to the target set, it MUST remove them
                    from the response before adding the response to the response
                    context.  However, a proxy SHOULD NOT recurse to a non-SIPS URI
                    if the Request-URI of the original request was a SIPS URI.  If
                    the proxy recurses on all of the contacts in a 3xx response,
                    the proxy SHOULD NOT add the resulting contactless response to
                    the response context.
                  
                    Removing the contact before adding the response to the response
                    context prevents the next element upstream from retrying a
                    location this proxy has already attempted.

                    3xx responses may contain a mixture of SIP, SIPS, and non-SIP
                    URIs.  A proxy may choose to recurse on the SIP and SIPS URIs
                    and place the remainder into the response context to be
                    returned, potentially in the final response.
                */

                if(response.StatusCodeType == SIP_StatusCodeType.Redirection && !m_NoRecurse && !handler.IsRecursed){
                    // Get SIP contacts and remove them from response.
                    SIP_t_ContactParam[] contacts = response.Contact.GetAllValues();
                    // Remove all contacts from response, we add non-SIP URIs back.
                    response.Contact.RemoveAll();
                    foreach(SIP_t_ContactParam contact in contacts){
                        // SIP URI add it to fork list.
                        if(contact.Address.IsSipOrSipsUri){
                            m_pTargets.Enqueue(new TargetHandler(this,null,(SIP_Uri)contact.Address.Uri,m_AddRecordRoute,true));
                        }
                        // Add specified URI back to response.
                        else{
                            response.Contact.Add(contact.ToStringValue());
                        }
                    }

                    // There are remaining non-SIP contacts, so we need to add the response to reponses collection.
                    if(response.Contact.GetAllValues().Length > 0){
                        m_pResponses.Add(response);
                    }

                    // Handle forking
                    if(m_pTargets.Count > 0){
                        if(m_ForkingMode == SIP_ForkingMode.Parallel){
                            while(m_pTargets.Count > 0){
                                TargetHandler h = m_pTargets.Dequeue();
                                m_pTargetsHandlers.Add(handler);
                                h.Start();
                            }
                        }
                        // Just fork next.
                        else{
                            TargetHandler h = m_pTargets.Dequeue();
                            m_pTargetsHandlers.Add(handler);
                            h.Start();
                        }

                        // Because we forked request to new target(s), we don't need to do steps 5 - 10.
                        return;
                    }
                }
                // Not 3xx response or recursing disabled.
                else{
                    m_pResponses.Add(response);
                }

                #endregion

                #region 5.  Check to see if this response should be forwarded immediately

                /*
                    Until a final response has been sent on the server transaction,
                    the following responses MUST be forwarded immediately:

                    -  Any provisional response other than 100 (Trying)

                    -  Any 2xx response

                    If a 6xx response is received, it is not immediately forwarded,
                    but the stateful proxy SHOULD cancel all client pending
                    transactions as described in Section 10, and it MUST NOT create
                    any new branches in this context.
                 
                    After a final response has been sent on the server transaction,
                    the following responses MUST be forwarded immediately:

                    -  Any 2xx response to an INVITE request
                */

                if(!m_IsFinalResponseSent){
                    if(response.StatusCodeType == SIP_StatusCodeType.Provisional && response.StatusCode != 100){
                        forwardResponse = true;
                    }
                    else if(response.StatusCodeType == SIP_StatusCodeType.Success){
                        forwardResponse = true;
                    }
                    else if(response.StatusCodeType == SIP_StatusCodeType.GlobalFailure){
                        CancelAllTargets();
                    }
                }
                else{
                    if(response.StatusCodeType == SIP_StatusCodeType.Success && m_pServerTransaction.Request.RequestLine.Method == SIP_Methods.INVITE){
                        forwardResponse = true;
                    }
                }

                #endregion

                #region x.  Handle sequential forking

                /*
                    Sequential Search: In a sequential search, a proxy server attempts
                    each contact address in sequence, proceeding to the next one
                    only after the previous has generated a final response.  A 2xx
                    or 6xx class final response always terminates a sequential
                    search.
                */
                if(m_ForkingMode == SIP_ForkingMode.Sequential && response.StatusCodeType != SIP_StatusCodeType.Provisional){
                    if(response.StatusCodeType == SIP_StatusCodeType.Success){
                        // Do nothing, 2xx will be always forwarded and step 10. Cancels all targets.
                    }
                    else if(response.StatusCodeType == SIP_StatusCodeType.GlobalFailure){
                        // Do nothing, 6xx is already handled in setp 5.
                    }
                    else if(m_pTargets.Count > 0){
                        TargetHandler h = m_pTargets.Dequeue();
                        m_pTargetsHandlers.Add(handler);
                        h.Start();

                        // Skip all next steps, we will get new responses from new target.
                        return;
                    }
                }

                #endregion

                #region 6.  When necessary, choose the best final response from the response context

                /* 
                    A stateful proxy MUST send a final response to a response
                    context's server transaction if no final responses have been
                    immediately forwarded by the above rules and all client
                    transactions in this response context have been terminated.

                    The stateful proxy MUST choose the "best" final response among
                    those received and stored in the response context.

                    If there are no final responses in the context, the proxy MUST
                    send a 408 (Request Timeout) response to the server
                    transaction.

                */

                if(!m_IsFinalResponseSent && !forwardResponse && m_pTargets.Count == 0){
                    bool mustChooseBestFinalResponse = true; 
                    // Check if all transactions terminated.
                    foreach(TargetHandler h in m_pTargetsHandlers){
                        if(!h.IsCompleted){
                            mustChooseBestFinalResponse = false;
                            break;
                        }
                    }

                    if(mustChooseBestFinalResponse){
                        response = GetBestFinalResponse();
                        if(response == null){
                            response = this.Proxy.Stack.CreateResponse(SIP_ResponseCodes.x408_Request_Timeout,m_pServerTransaction.Request);
                        }

                        forwardResponse = true;
                    }
                }

                #endregion

                if(forwardResponse){

                    #region 7.  Aggregate authorization header field values if necessary

                    /* 
                        If the selected response is a 401 (Unauthorized) or 407 (Proxy Authentication Required), 
                        the proxy MUST collect any WWW-Authenticate and Proxy-Authenticate header field values 
                        from all other 401 (Unauthorized) and 407 (Proxy Authentication Required) responses 
                        received so far in this response context and add them to this response without 
                        modification before forwarding. The resulting 401 (Unauthorized) or 407 (Proxy
                        Authentication Required) response could have several WWW-Authenticate AND 
                        Proxy-Authenticate header field values.

                        This is necessary because any or all of the destinations the request was forwarded to 
                        may have requested credentials.  The client needs to receive all of those challenges and 
                        supply credentials for each of them when it retries the request.
                    */
                    if(response.StatusCode == 401 || response.StatusCode == 407){
                        foreach(SIP_Response resp in m_pResponses.ToArray()){
                            if(response != resp && (resp.StatusCode == 401 || resp.StatusCode == 407)){
                                // WWW-Authenticate
                                foreach(SIP_HeaderField hf in resp.WWWAuthenticate.HeaderFields){
                                    resp.WWWAuthenticate.Add(hf.Value);
                                }
                                // Proxy-Authenticate
                                foreach(SIP_HeaderField hf in resp.ProxyAuthenticate.HeaderFields){
                                    resp.ProxyAuthenticate.Add(hf.Value);
                                }
                            }
                        }
                    }

                    #endregion

                    #region 8.  Optionally rewrite Record-Route header field values

                    // This is optional so we currently won't do that.

                    #endregion

                    #region 9.  Forward the response

                    SendResponse(transaction,response);
                    if(response.StatusCodeType != SIP_StatusCodeType.Provisional){
                        m_IsFinalResponseSent = true;
                    }

                    #endregion

                    #region 10. Generate any necessary CANCEL requests

                    /* 
                        If the forwarded response was a final response, the proxy MUST
                        generate a CANCEL request for all pending client transactions
                        associated with this response context.
                    */                
                    if(response.StatusCodeType != SIP_StatusCodeType.Provisional){
                        CancelAllTargets();
                    }

                    #endregion
                }
            }
        }

        #endregion

        #region method SendResponse

        /// <summary>
        /// Sends SIP response to caller. If proxy context is in B2BUA mode, new response is generated 
        /// as needed.
        /// </summary>
        /// <param name="transaction">Client transaction what response it is.</param>
        /// <param name="response">Response to send.</param>
        private void SendResponse(SIP_ClientTransaction transaction,SIP_Response response)
        { 
            if(m_IsB2BUA){
                /* draft-marjou-sipping-b2bua-00 4.1.3.
                    When the UAC side of the B2BUA receives the downstream SIP response
                    of a forwarded request, its associated UAS creates an upstream
                    response (except for 100 responses).  The creation of the Via, Max-
                    Forwards, Call-Id, CSeq, Record-Route and Contact header fields
                    follows the rules of [2].  The Record-Route header fields of the
                    downstream response are not copied in the new upstream response, as
                    Record-Route is meaningful for the downstream dialog.  The UAS SHOULD
                    copy other header fields and body from the downstream response into
                    this upstream response before sending it.
                */
                
                SIP_Request originalRequest = m_pServerTransaction.Request;

                // We need to use caller original request to construct response from proxied response.
                SIP_Response b2buaResponse = response.Copy();
                b2buaResponse.Via.RemoveAll();
                b2buaResponse.Via.AddToTop(originalRequest.Via.GetTopMostValue().ToStringValue());
                b2buaResponse.CallID = originalRequest.CallID;
                b2buaResponse.CSeq = originalRequest.CSeq;
                b2buaResponse.Contact.RemoveAll();
                //b2buaResponse.Contact.Add(m_pProxy.CreateContact(originalRequest.From.Address).ToStringValue());
                b2buaResponse.RecordRoute.RemoveAll();
                               
                b2buaResponse.Allow.RemoveAll();
                b2buaResponse.Supported.RemoveAll();
                // Accept to non ACK,BYE request.
                if(originalRequest.RequestLine.Method != SIP_Methods.ACK && originalRequest.RequestLine.Method != SIP_Methods.BYE){
                    b2buaResponse.Allow.Add("INVITE,ACK,OPTIONS,CANCEL,BYE,PRACK");
                }
                // Supported to non ACK request. 
                if(originalRequest.RequestLine.Method != SIP_Methods.ACK){
                    b2buaResponse.Supported.Add("100rel,timer");
                }
                // Remove Require: header.
                b2buaResponse.Require.RemoveAll();

                m_pServerTransaction.SendResponse(b2buaResponse);
                
                // If INVITE 2xx response do call here.
                if(response.CSeq.RequestMethod.ToUpper() == SIP_Methods.INVITE && response.StatusCodeType == SIP_StatusCodeType.Success){
                    m_pProxy.B2BUA.AddCall(m_pServerTransaction.Dialog,transaction.Dialog);
                }
            }
            else{
                m_pServerTransaction.SendResponse(response);
            }
        }

        #endregion

        #region method CancelAllTargets

        /// <summary>
        /// Cancels all targets processing.
        /// </summary>
        private void CancelAllTargets()
        {
            if(!m_NoCancel){
                m_pTargets.Clear();

                foreach(TargetHandler target in m_pTargetsHandlers.ToArray()){
                    target.Cancel();
                }
            }
        }

        #endregion

        #region method GetBestFinalResponse

        /// <summary>
        /// Gets best final response. If no final response in responses collection, null is returned.
        /// </summary>
        /// <returns>Resturns best final response or  null if no final response.</returns>
        private SIP_Response GetBestFinalResponse()
        {
            // 6xx -> 2xx -> 3xx -> 4xx -> 5xx

            // 6xx
            foreach(SIP_Response resp in m_pResponses.ToArray()){
                if(resp.StatusCodeType == SIP_StatusCodeType.GlobalFailure){
                    return resp;
                }
            }
            // 2xx
            foreach(SIP_Response resp in m_pResponses.ToArray()){
                if(resp.StatusCodeType == SIP_StatusCodeType.Success){
                    return resp;
                }
            }
            // 3xx
            foreach(SIP_Response resp in m_pResponses.ToArray()){
                if(resp.StatusCodeType == SIP_StatusCodeType.Redirection){
                    return resp;
                }
            }                
            // 4xx
            foreach(SIP_Response resp in m_pResponses.ToArray()){
                if(resp.StatusCodeType == SIP_StatusCodeType.RequestFailure){
                    return resp;
                }
            }
            // 5xx
            foreach(SIP_Response resp in m_pResponses.ToArray()){
                if(resp.StatusCodeType == SIP_StatusCodeType.ServerFailure){
                    return resp;
                }
            }

            return null;
        }

        #endregion

        #region method GetCredential

        /// <summary>
        /// Gets credentials for specified realm. Returns null if none such credentials.
        /// </summary>
        /// <param name="realm">Realm which credentials to get.</param>
        /// <returns>Returns specified realm credentials or null in none.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>realm</b> is null reference.</exception>
        private NetworkCredential GetCredential(string realm)
        {
            if(realm == null){
                throw new ArgumentNullException("realm");
            }

            foreach(NetworkCredential c in m_pCredentials){
                if(c.Domain.ToLower() == realm.ToLower()){
                    return c;
                }
            }
            return null;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets owner SIP proxy server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Proxy Proxy
        {
            get{  
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pProxy; 
            }
        }

        /// <summary>
        /// Gets proxy context ID.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public string ID
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return m_ID; 
            }
        }

        /// <summary>
        /// Gets time when proxy context was created.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public DateTime CreateTime
        {
            get{  
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return m_CreateTime; 
            }
        }

        /// <summary>
        /// Gets forking mode used by this 'proxy context'.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_ForkingMode ForkingMode
        {
            get{  
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return m_ForkingMode; 
            }
        }

        /// <summary>
        /// Gets if proxy cancels forked requests what are not needed any more. If true, 
        /// requests not canceled, otherwise canceled.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public bool NoCancel
        {
            get{  
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return m_NoCancel; 
            }
        }

        /// <summary>
        /// Gets what proxy server does when it gets 3xx response. If true proxy will forward
        /// request to new specified address if false, proxy will return 3xx response to caller.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public bool Recurse
        {
            get{  
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return !m_NoRecurse; 
            }
        }

        /// <summary>
        /// Gets server transaction what is responsible for sending responses to caller.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public SIP_ServerTransaction ServerTransaction
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return m_pServerTransaction; 
            }
        }

        /// <summary>
        /// Gets request what is forwarded by proxy context.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Request Request
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SIP_ProxyContext");
                }

                return m_pRequest; 
            }
        }
       
        /// <summary>
        /// Gets all responses what proxy context has received.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public SIP_Response[] Responses
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pResponses.ToArray(); 
            }
        }

        /// <summary>
        /// Gets credentials collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public List<NetworkCredential> Credentials
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pCredentials; 
            }
        }

        #endregion

    }
}
