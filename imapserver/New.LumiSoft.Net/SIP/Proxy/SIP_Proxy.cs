using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Timers;

using LumiSoft.Net.AUTH;
using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    #region Delegates

    /// <summary>
    /// Represents the method that will handle the SIP_ProxyCore.IsLocalUri event.
    /// </summary>
    /// <param name="uri">Request URI.</param>
    /// <returns>Returns true if server local URI, otherwise false.</returns>
    public delegate bool SIP_IsLocalUriEventHandler(string uri);

    /// <summary>
    /// Represents the method that will handle the SIP_ProxyCore.Authenticate event.
    /// </summary>
    public delegate void SIP_AuthenticateEventHandler(SIP_AuthenticateEventArgs e);

    /// <summary>
    /// Represents the method that will handle the SIP_ProxyCore.AddressExists event.
    /// </summary>
    /// <param name="address">SIP address to check.</param>
    /// <returns>Returns true if specified address exists, otherwise false.</returns>
    public delegate bool SIP_AddressExistsEventHandler(string address);

    #endregion

    /// <summary>
    /// Implements SIP registrar,statefull and stateless proxy.
    /// </summary>
    public class SIP_Proxy : IDisposable
    {
        private  bool                   m_IsDisposed     = false;
        private  SIP_Stack              m_pStack         = null;
        private  SIP_ProxyMode          m_ProxyMode      = SIP_ProxyMode.Registrar | SIP_ProxyMode.Statefull;
        private  SIP_ForkingMode        m_ForkingMode    = SIP_ForkingMode.Parallel;
        private  SIP_Registrar          m_pRegistrar     = null;
        private  SIP_B2BUA              m_pB2BUA         = null;
        private  string                 m_Opaque         = "";
        internal List<SIP_ProxyContext> m_pProxyContexts = null;
        private  List<SIP_ProxyHandler> m_pHandlers      = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Reference to SIP stack.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sipStack</b> is null reference.</exception>
        public SIP_Proxy(SIP_Stack stack)
        {
            if(stack == null){
                throw new ArgumentNullException("stack");
            }

            m_pStack = stack;
            m_pStack.RequestReceived += new EventHandler<SIP_RequestReceivedEventArgs>(m_pStack_RequestReceived);
            m_pStack.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(m_pStack_ResponseReceived);

            m_pRegistrar     = new SIP_Registrar(this);
            m_pB2BUA         = new SIP_B2BUA(this);
            m_Opaque         = Auth_HttpDigest.CreateOpaque();
            m_pProxyContexts = new List<SIP_ProxyContext>();
            m_pHandlers      = new List<SIP_ProxyHandler>();
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

            if(m_pStack != null){
                m_pStack.Dispose();
                m_pStack = null;
            }
            m_pRegistrar = null;
            m_pB2BUA = null;
            m_pProxyContexts = null;
        }

        #endregion


        #region Events Handling

        #region method m_pStack_RequestReceived

        /// <summary>
        /// This method is called when SIP stack receives new request.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pStack_RequestReceived(object sender,SIP_RequestReceivedEventArgs e)
        {
            OnRequestReceived(e);
        }

        #endregion

        #region method m_pStack_ResponseReceived

        /// <summary>
        /// This method is called when SIP stack receives new response.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pStack_ResponseReceived(object sender,SIP_ResponseReceivedEventArgs e)
        {
            OnResponseReceived(e);            
        }

        #endregion

        #endregion


        #region method OnRequestReceived

        /// <summary>
        /// This method is called when new request is received.
        /// </summary>
        /// <param name="e">Request event arguments.</param>
        private void OnRequestReceived(SIP_RequestReceivedEventArgs e)
        {            
            SIP_Request request = e.Request;
            try{  

                #region Statefull

                // Statefull
                if((m_ProxyMode & SIP_ProxyMode.Statefull) != 0){
                    // Statefull proxy is transaction statefull proxy only, 
                    // what don't create dialogs and keep dialog state.

                    /* RFC 3261 16.10.
                        StateFull proxy:
	                        If a matching response context is found, the element MUST
	                        immediately return a 200 (OK) response to the CANCEL request. 

	                        If a response context is not found, the element does not have any
	                        knowledge of the request to apply the CANCEL to.  It MUST statelessly
	                        forward the CANCEL request (it may have statelessly forwarded the
	                        associated request previously).
                    */
                    if(e.Request.RequestLine.Method == SIP_Methods.CANCEL){
                        // Don't do server transaction before we get CANCEL matching transaction.
                        SIP_ServerTransaction trToCancel = m_pStack.TransactionLayer.MatchCancelToTransaction(e.Request);
                        if(trToCancel != null){
                            trToCancel.Cancel();
                            e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x200_Ok,request));
                        }
                        else{
                            ForwardRequest(false,e,true);
                        }
                    }
                    // ACK never creates transaction, it's always passed directly to transport layer.
                    else if(e.Request.RequestLine.Method == SIP_Methods.ACK){
                        ForwardRequest(false,e,true);
                    }
                    else{
                        ForwardRequest(true,e,true);
                    }
                }

                #endregion
                
                #region B2BUA
                
                // B2BUA
                else if((m_ProxyMode & SIP_ProxyMode.B2BUA) != 0){
                    m_pB2BUA.OnRequestReceived(e);
                }

                #endregion

                #region Stateless

                // Stateless
                else if((m_ProxyMode & SIP_ProxyMode.Stateless) != 0){
                    // Stateless proxy don't do transaction, just forwards all.
                    ForwardRequest(false,e,true);
                }

                #endregion

                #region Proxy won't accept command
                
                else{
                    e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x501_Not_Implemented,request));
                }

                #endregion

            }
            catch(Exception x){
                try{
                    m_pStack.TransportLayer.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x500_Server_Internal_Error + ": " + x.Message,e.Request));
                }
                catch{
                    // Skip transport layer exception if send fails.
                }

                // Don't raise OnError for transport errors.
                if(!(x is SIP_TransportException)){
                    m_pStack.OnError(x);
                }
            }            
        }

        #endregion

        #region method OnResponseReceived

        /// <summary>
        /// This method is called when new response is received.
        /// </summary>
        /// <param name="e">Response event arguments.</param>
        private void OnResponseReceived(SIP_ResponseReceivedEventArgs e)
        {
            if((m_ProxyMode & SIP_ProxyMode.B2BUA) != 0){
                m_pB2BUA.OnResponseReceived(e);
            }
            else if((m_ProxyMode & SIP_ProxyMode.Statefull) != 0){
                /* RFC 6026 7.3. Proxy Considerations.
                    This document changes the behavior of transaction-stateful proxies to
                    not forward stray INVITE responses.  When receiving any SIP response,
                    a transaction-stateful proxy MUST compare the transaction identifier
                    in that response against its existing transaction state machines.
                    The proxy MUST NOT forward the response if there is no matching
                    transaction state machine.
                */
            }
            else{
                /* This method is called when stateless proxy gets response.
                */
                               
                /* RFC 3261 16.11.
                    When a response arrives at a stateless proxy, the proxy MUST inspect the sent-by 
                    value in the first (topmost) Via header field value. If that address matches the proxy,
                    (it equals a value this proxy has inserted into previous requests) the proxy MUST 
                    remove that header field value from the response and forward the result to the 
                    location indicated in the next Via header field value.
                */
                // Just remove topmost Via:, sent-by check is done in transport layer.
                e.Response.Via.RemoveTopMostValue();

                if((m_ProxyMode & SIP_ProxyMode.Statefull) != 0){
                    // We should not reach here. This happens when no matching client transaction found.
                    // RFC 3161 18.1.2 orders to forward them statelessly.
                    m_pStack.TransportLayer.SendResponse(e.Response);
                }
                else if((m_ProxyMode & SIP_ProxyMode.Stateless) != 0){
                    m_pStack.TransportLayer.SendResponse(e.Response);
                }
            }
        }

        #endregion


        #region method ForwardRequest

        /// <summary>
        /// Forwards specified request to target recipient.
        /// </summary>
        /// <param name="statefull">Specifies if request is sent statefully or statelessly.</param>
        /// <param name="e">Request event arguments.</param>
        /// <param name="addRecordRoute">If true Record-Route header field is added.</param>
        internal void ForwardRequest(bool statefull,SIP_RequestReceivedEventArgs e,bool addRecordRoute)
        {
            SIP_RequestContext requestContext = new SIP_RequestContext(this,e.Request,e.Flow);

            SIP_Request request = e.Request;
            SIP_Uri     route   = null;
                        
            /* RFC 3261 16.
                1. Validate the request (Section 16.3)
                    1. Reasonable Syntax
                    2. URI scheme (NOTE: We do it later)
                    3. Max-Forwards
                    4. (Optional) Loop Detection
                    5. Proxy-Require
                    6. Proxy-Authorization
                2. Preprocess routing information (Section 16.4)
                3. Determine target(s) for the request (Section 16.5)
                x. Custom handling (non-RFC)
                    1. Process custom request handlers.
                    2. URI scheme
                4. Forward the request (Section 16.6)
            */

            #region 1. Validate the request (Section 16.3)

            // 1.1 Reasonable Syntax.
            //      SIP_Message parsing have done it.

            // 1.2 URI scheme check.
            //      We do it later.

            // 1.3 Max-Forwards.
            if(request.MaxForwards <= 0){
                e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x483_Too_Many_Hops,request));
                return;
            }

            // 1.4 (Optional) Loop Detection.
            //      Skip.

            // 1.5 Proxy-Require.
            //      TODO:

            // 1.6 Proxy-Authorization.
            /*  If an element requires credentials before forwarding a request,
                the request MUST be inspected as described in Section 22.3.  That
                section also defines what the element must do if the inspection
                fails.
            */
                        
            // We need to auth all foreign calls.            
            if(!SIP_Utils.IsSipOrSipsUri(request.RequestLine.Uri.ToString()) || !this.OnIsLocalUri(((SIP_Uri)request.RequestLine.Uri).Host)){
                // If To: field is registrar AOR and request-URI is local registration contact, skip authentication.
                bool skipAuth = false;
                if(request.To.Address.IsSipOrSipsUri){
                    SIP_Registration registration = m_pRegistrar.GetRegistration(((SIP_Uri)request.To.Address.Uri).Address);
                    if(registration != null){
                        if(registration.GetBinding(request.RequestLine.Uri) != null){
                            skipAuth = true;
                        }
                    }
                }

                if(!skipAuth){
                    string userName = null;

                    // We need to pass-through ACK.
                    if(request.RequestLine.Method == SIP_Methods.ACK){
                    }
                    else if(!AuthenticateRequest(e,out userName)){
                        return;
                    }

                    requestContext.SetUser(userName);
                }
            }

            #endregion

            #region 2. Preprocess routing information (Section 16.4).

            /*
                The proxy MUST inspect the Request-URI of the request.  If the
                Request-URI of the request contains a value this proxy previously
                placed into a Record-Route header field (see Section 16.6 item 4),
                the proxy MUST replace the Request-URI in the request with the last
                value from the Route header field, and remove that value from the
                Route header field.  The proxy MUST then proceed as if it received
                this modified request.

                    This will only happen when the element sending the request to the
                    proxy (which may have been an endpoint) is a strict router.  This
                    rewrite on receive is necessary to enable backwards compatibility
                    with those elements.  It also allows elements following this
                    specification to preserve the Request-URI through strict-routing
                    proxies (see Section 12.2.1.1).

                    This requirement does not obligate a proxy to keep state in order
                    to detect URIs it previously placed in Record-Route header fields.
                    Instead, a proxy need only place enough information in those URIs
                    to recognize them as values it provided when they later appear.

                If the Request-URI contains a maddr parameter, the proxy MUST check
                to see if its value is in the set of addresses or domains the proxy
                is configured to be responsible for.  If the Request-URI has a maddr
                parameter with a value the proxy is responsible for, and the request
                was received using the port and transport indicated (explicitly or by
                default) in the Request-URI, the proxy MUST strip the maddr and any
                non-default port or transport parameter and continue processing as if
                those values had not been present in the request.

                    A request may arrive with a maddr matching the proxy, but on a
                    port or transport different from that indicated in the URI.  Such
                    a request needs to be forwarded to the proxy using the indicated
                    port and transport.

                If the first value in the Route header field indicates this proxy,
                the proxy MUST remove that value from the request.
            */
            
            // Strict route - handle it.
            if((request.RequestLine.Uri is SIP_Uri) && IsRecordRoute(((SIP_Uri)request.RequestLine.Uri)) && request.Route.GetAllValues().Length > 0){
                request.RequestLine.Uri = request.Route.GetAllValues()[request.Route.GetAllValues().Length - 1].Address.Uri;
                SIP_t_AddressParam[] routes = request.Route.GetAllValues();
                route = (SIP_Uri)routes[routes.Length - 1].Address.Uri;
                request.Route.RemoveLastValue();
            }

            // Check if Route header field indicates this proxy.
            if(request.Route.GetAllValues().Length > 0){
                route = (SIP_Uri)request.Route.GetTopMostValue().Address.Uri;

                // We consider loose-route always ours, because otherwise this message never reach here.
                if(route.Param_Lr){
                    request.Route.RemoveTopMostValue();
                }
                // It's our route, remove it.
                else if(IsLocalRoute(route)){
                    request.Route.RemoveTopMostValue();
                }
            }

            #endregion

            #region REGISTER request processing
                       
            if(e.Request.RequestLine.Method == SIP_Methods.REGISTER){
                SIP_Uri requestUri = (SIP_Uri)e.Request.RequestLine.Uri;

                // REGISTER is meant for us.
                if(this.OnIsLocalUri(requestUri.Host)){
                    if((m_ProxyMode & SIP_ProxyMode.Registrar) != 0){
                        m_pRegistrar.Register(e);

                        return;
                    }
                    else{
                        e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x405_Method_Not_Allowed,e.Request));

                        return;
                    }
                }
                // Forward REGISTER.
                // else{
            }

            #endregion

            #region 3. Determine target(s) for the request (Section 16.5)

            /* 3. Determine target(s) for the request (Section 16.5)
                    Next, the proxy calculates the target(s) of the request.  The set of
                    targets will either be predetermined by the contents of the request
                    or will be obtained from an abstract location service.  Each target
                    in the set is represented as a URI.
             
                    If the domain of the Request-URI indicates a domain this element is
                    not responsible for, the Request-URI MUST be placed into the target
                    set as the only target, and the element MUST proceed to the task of
                    Request Forwarding (Section 16.6).
              
                    If the target set for the request has not been predetermined as
                    described above, this implies that the element is responsible for the
                    domain in the Request-URI, and the element MAY use whatever mechanism
                    it desires to determine where to send the request.  Any of these
                    mechanisms can be modeled as accessing an abstract Location Service.
                    This may consist of obtaining information from a location service
                    created by a SIP Registrar, reading a database, consulting a presence
                    server, utilizing other protocols, or simply performing an
                    algorithmic substitution on the Request-URI.  When accessing the
                    location service constructed by a registrar, the Request-URI MUST
                    first be canonicalized as described in Section 10.3 before being used
                    as an index.  The output of these mechanisms is used to construct the
                    target set.
            */
            
            // Non-SIP
            // Foreign SIP
            // Local SIP

            if(e.Request.RequestLine.Uri is SIP_Uri){                        
                SIP_Uri requestUri = (SIP_Uri)e.Request.RequestLine.Uri;

                // Proxy is not responsible for the domain in the Request-URI.
                if(!this.OnIsLocalUri(requestUri.Host)){
                    /* NAT traversal.
                        When we do record routing, store request sender flow info and request target flow info.
                        Now the tricky part, how proxy later which flow is target (because both sides can send requests).
                          Sender-flow will store from-tag to flow and target-flow will store flowID only (Because we don't know to-tag).
                          Later if request to-tag matches(incoming request), use that flow, otherwise(outgoing request) other flow.
                 
                          flowInfo: sender-flow "/" target-flow
                              sender-flow = from-tag ":" flowID
                              target-flow = flowID                        
                    */

                    SIP_Flow targetFlow = null;
                    string flowInfo = (route != null && route.Parameters["flowInfo"] != null) ? route.Parameters["flowInfo"].Value : null;
                    if(flowInfo != null && request.To.Tag != null){
                        string flow1Tag = flowInfo.Substring(0,flowInfo.IndexOf(':'));
                        string flow1ID  = flowInfo.Substring(flowInfo.IndexOf(':') + 1,flowInfo.IndexOf('/') - flowInfo.IndexOf(':') - 1);
                        string flow2ID  = flowInfo.Substring(flowInfo.IndexOf('/') + 1);
                                             
                        if(flow1Tag == request.To.Tag){
                            targetFlow = m_pStack.TransportLayer.GetFlow(flow1ID);
                        }
                        else{;
                            targetFlow = m_pStack.TransportLayer.GetFlow(flow2ID);
                        }
                    }

                    requestContext.Targets.Add(new SIP_ProxyTarget(requestUri,targetFlow));
                }
                // Proxy is responsible for the domain in the Request-URI.
                else{
                    // Try to get AOR from registrar.
                    SIP_Registration registration = m_pRegistrar.GetRegistration(requestUri.Address);

                    // We have AOR specified in request-URI in registrar server.
                    if(registration != null){
                        // Add all AOR SIP contacts to target set.
                        foreach(SIP_RegistrationBinding binding in registration.Bindings){
                            if(binding.ContactURI is SIP_Uri && binding.TTL > 0){
                                requestContext.Targets.Add(new SIP_ProxyTarget((SIP_Uri)binding.ContactURI,binding.Flow));
                            }
                        }
                    }
                    // We don't have AOR specified in request-URI in registrar server.
                    else{
                        // If the Request-URI indicates a resource at this proxy that does not
                        // exist, the proxy MUST return a 404 (Not Found) response.                    
                        if(!this.OnAddressExists(requestUri.Address)){                        
                            e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x404_Not_Found,e.Request));
                            return;
                        }
                    }

                    // If the target set remains empty after applying all of the above, the proxy MUST return an error response, 
                    // which SHOULD be the 480 (Temporarily Unavailable) response.
                    if(requestContext.Targets.Count == 0){
                        e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x480_Temporarily_Unavailable,e.Request));
                        return;
                    }
                }               
            }

            #endregion

            #region x. Custom handling

            // x.1 Process custom request handlers, if any.
            foreach(SIP_ProxyHandler handler in this.Handlers){
                try{
                    SIP_ProxyHandler h = handler;

                    // Reusing existing handler not allowed, create new instance of handler.
                    if(!handler.IsReusable){
                        h = (SIP_ProxyHandler)System.Activator.CreateInstance(handler.GetType());
                    }

                    if(h.ProcessRequest(requestContext)){
                        // Handler processed request, we are done.
                        return;
                    }
                }
                catch(Exception x){
                    m_pStack.OnError(x);
                }
            }

            // x.2 URI scheme.
            //  If no targets and request-URI non-SIP, reject request because custom handlers should have been handled it.
            if(requestContext.Targets.Count == 0 && !SIP_Utils.IsSipOrSipsUri(request.RequestLine.Uri.ToString())){
                e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x416_Unsupported_URI_Scheme,e.Request));
                return;
            }

            #endregion

            #region 4. Forward the request (Section 16.6)

            #region Statefull

            if (statefull){
                SIP_ProxyContext proxyContext = this.CreateProxyContext(requestContext,e.ServerTransaction,request,addRecordRoute);
                proxyContext.Start();
            }

            #endregion

            #region Stateless

            else{
                /* RFC 3261 16.6 Request Forwarding.
                For each target, the proxy forwards the request following these steps:
                    1.  Make a copy of the received request
                    2.  Update the Request-URI
                    3.  Update the Max-Forwards header field
                    4.  Optionally add a Record-route header field value
                    5.  Optionally add additional header fields
                    6.  Postprocess routing information
                    7.  Determine the next-hop address, port, and transport
                    8.  Add a Via header field value
                    9.  Add a Content-Length header field if necessary
                    10. Forward the new request
                */

                /* RFC 3261 16.11 Stateless Proxy.                 
                    o  A stateless proxy MUST choose one and only one target from the target set. This choice 
                       MUST only rely on fields in the message and time-invariant properties of the server. In
                       particular, a retransmitted request MUST be forwarded to the same destination each time 
                       it is processed. Furthermore, CANCEL and non-Routed ACK requests MUST generate the same
                       choice as their associated INVITE.
                 
                    However, a stateless proxy cannot simply use a random number generator to compute
                    the first component of the branch ID, as described in Section 16.6 bullet 8.
                    This is because retransmissions of a request need to have the same value, and 
                    a stateless proxy cannot tell a retransmission from the original request.
                
                    We just use: "z9hG4bK-" + md5(topmost branch)                
                */

                bool      isStrictRoute = false;
                SIP_Hop[] hops          = null;
                                
                #region 1.  Make a copy of the received request

                SIP_Request forwardRequest = request.Copy();

                #endregion

                #region 2.  Update the Request-URI

                forwardRequest.RequestLine.Uri = requestContext.Targets[0].TargetUri;

                #endregion

                #region 3.  Update the Max-Forwards header field

                forwardRequest.MaxForwards--;

                #endregion

                #region 4.  Optionally add a Record-route header field value

                #endregion

                #region 5.  Optionally add additional header fields

                #endregion

                #region 6.  Postprocess routing information

                /* 6. Postprocess routing information.
             
                    If the copy contains a Route header field, the proxy MUST inspect the URI in its first value.  
                    If that URI does not contain an lr parameter, the proxy MUST modify the copy as follows:             
                        - The proxy MUST place the Request-URI into the Route header
                          field as the last value.
             
                        - The proxy MUST then place the first Route header field value
                          into the Request-URI and remove that value from the Route header field.
                */
                if(forwardRequest.Route.GetAllValues().Length > 0 && !forwardRequest.Route.GetTopMostValue().Parameters.Contains("lr")){
                    forwardRequest.Route.Add(forwardRequest.RequestLine.Uri.ToString());

                    forwardRequest.RequestLine.Uri = SIP_Utils.UriToRequestUri(forwardRequest.Route.GetTopMostValue().Address.Uri);
                    forwardRequest.Route.RemoveTopMostValue();

                    isStrictRoute = true;
                }

                #endregion

                #region 7.  Determine the next-hop address, port, and transport

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
                    uri = (SIP_Uri)forwardRequest.RequestLine.Uri;
                }
                else if(forwardRequest.Route.GetTopMostValue() != null){
                    uri = (SIP_Uri)forwardRequest.Route.GetTopMostValue().Address.Uri;
                }
                else{
                    uri = (SIP_Uri)forwardRequest.RequestLine.Uri;
                }
                
                hops = m_pStack.GetHops(uri,forwardRequest.ToByteData().Length,((SIP_Uri)forwardRequest.RequestLine.Uri).IsSecure);

                if(hops.Length == 0){
                    if(forwardRequest.RequestLine.Method != SIP_Methods.ACK){
                        e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x503_Service_Unavailable + ": No hop(s) for target.",forwardRequest));
                    }

                    return;
                }
                
                #endregion

                #region 8.  Add a Via header field value
                                
                forwardRequest.Via.AddToTop("SIP/2.0/transport-tl-addign sentBy-tl-assign-it;branch=z9hG4bK-" + Net_Utils.ComputeMd5(request.Via.GetTopMostValue().Branch,true));
                
                // Add 'flowID' what received request, you should use the same flow to send response back.
                // For more info see RFC 3261 18.2.2.
                forwardRequest.Via.GetTopMostValue().Parameters.Add("flowID",request.Flow.ID);

                #endregion

                #region 9.  Add a Content-Length header field if necessary

                // Skip, our SIP_Message class is smart and do it when ever it's needed.

                #endregion

                #region 10. Forward the new request

                try{
                    try{
                        if(requestContext.Targets[0].Flow != null){
                            m_pStack.TransportLayer.SendRequest(requestContext.Targets[0].Flow,request);
                            
                            return;
                        }
                    }
                    catch{
                        m_pStack.TransportLayer.SendRequest(request,null,hops[0]);
                    }
                }
                catch(SIP_TransportException x){
                    string dummy = x.Message;

                    if(forwardRequest.RequestLine.Method != SIP_Methods.ACK){
                        /* RFC 3261 16.9 Handling Transport Errors
                            If the transport layer notifies a proxy of an error when it tries to
                            forward a request (see Section 18.4), the proxy MUST behave as if the
                            forwarded request received a 503 (Service Unavailable) response.
                        */
                        e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x503_Service_Unavailable + ": Transport error.",forwardRequest));
                    }
                }

                #endregion

            }

            #endregion

            #endregion
        }

        #endregion

        #region method AuthenticateRequest

        /// <summary>
        /// Authenticates SIP request. This method also sends all needed replys to request sender.
        /// </summary>
        /// <param name="e">Request event arguments.</param>
        /// <returns>Returns true if request was authenticated.</returns>
        internal bool AuthenticateRequest(SIP_RequestReceivedEventArgs e)
        {
            string userName = null;
            return AuthenticateRequest(e,out userName);
        }

        /// <summary>
        /// Authenticates SIP request. This method also sends all needed replys to request sender.
        /// </summary>
        /// <param name="e">Request event arguments.</param>
        /// <param name="userName">If authentication sucessful, then authenticated user name is stored to this variable.</param>
        /// <returns>Returns true if request was authenticated.</returns>
        internal bool AuthenticateRequest(SIP_RequestReceivedEventArgs e,out string userName)
        {            
            userName = null;
         
            SIP_t_Credentials credentials = SIP_Utils.GetCredentials(e.Request,m_pStack.Realm);
            // No credentials for our realm.
            if(credentials == null){
                SIP_Response notAuthenticatedResponse = m_pStack.CreateResponse(SIP_ResponseCodes.x407_Proxy_Authentication_Required,e.Request);
                notAuthenticatedResponse.ProxyAuthenticate.Add(new Auth_HttpDigest(m_pStack.Realm,m_pStack.DigestNonceManager.CreateNonce(),m_Opaque).ToChallange());
                    
                e.ServerTransaction.SendResponse(notAuthenticatedResponse);
                return false;
            }
                                       
            Auth_HttpDigest auth = new Auth_HttpDigest(credentials.AuthData,e.Request.RequestLine.Method);
            // Check opaque validity.
            if(auth.Opaque != m_Opaque){
                SIP_Response notAuthenticatedResponse = m_pStack.CreateResponse(SIP_ResponseCodes.x407_Proxy_Authentication_Required + ": Opaque value won't match !",e.Request);
                notAuthenticatedResponse.ProxyAuthenticate.Add(new Auth_HttpDigest(m_pStack.Realm,m_pStack.DigestNonceManager.CreateNonce(),m_Opaque).ToChallange());

                // Send response
                e.ServerTransaction.SendResponse(notAuthenticatedResponse);
                return false;
            }
            // Check nonce validity.
            if(!m_pStack.DigestNonceManager.NonceExists(auth.Nonce)){
                SIP_Response notAuthenticatedResponse = m_pStack.CreateResponse(SIP_ResponseCodes.x407_Proxy_Authentication_Required + ": Invalid nonce value !",e.Request);
                notAuthenticatedResponse.ProxyAuthenticate.Add(new Auth_HttpDigest(m_pStack.Realm,m_pStack.DigestNonceManager.CreateNonce(),m_Opaque).ToChallange());

                // Send response
                e.ServerTransaction.SendResponse(notAuthenticatedResponse);
                return false;
            }
            // Valid nonce, consume it so that nonce can't be used any more. 
            else{
                m_pStack.DigestNonceManager.RemoveNonce(auth.Nonce);
            }

            SIP_AuthenticateEventArgs eArgs = this.OnAuthenticate(auth);
            // Authenticate failed.
            if(!eArgs.Authenticated){
                SIP_Response notAuthenticatedResponse = m_pStack.CreateResponse(SIP_ResponseCodes.x407_Proxy_Authentication_Required + ": Authentication failed.",e.Request);
                notAuthenticatedResponse.ProxyAuthenticate.Add(new Auth_HttpDigest(m_pStack.Realm,m_pStack.DigestNonceManager.CreateNonce(),m_Opaque).ToChallange());
                    
                // Send response
                e.ServerTransaction.SendResponse(notAuthenticatedResponse);
                return false;
            }

            userName = auth.UserName;

            return true;
        }

        #endregion

        #region method IsLocalRoute

        /// <summary>
        /// Gets if this proxy server is responsible for specified route.
        /// </summary>
        /// <param name="uri">Route value to check.</param>
        /// <returns>Returns trues if server route, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        internal bool IsLocalRoute(SIP_Uri uri)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            // Not a route.
            if(uri.User != null){
                return false;
            }

            foreach(IPBindInfo bind in m_pStack.BindInfo){
                if(uri.Host.ToLower() == bind.HostName.ToLower()){
                    return true;
                }
            }
            
            return false;
        }

        #endregion

        #region method IsRecordRoute

        /// <summary>
        /// Checks if the specified route is Record-Route what we add.
        /// </summary>
        /// <param name="route">Record route.</param>
        /// <returns>Returns true if the specified route is local Record-Route value, otherwise false.</returns>
        private bool IsRecordRoute(SIP_Uri route)
        {
            if(route == null){
                throw new ArgumentNullException("route");
            }

            foreach(IPBindInfo bind in m_pStack.BindInfo){
                if(route.Host.ToLower() == bind.HostName.ToLower()){
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region method CreateProxyContext

        internal SIP_ProxyContext CreateProxyContext(SIP_RequestContext requestContext,SIP_ServerTransaction transaction,SIP_Request request,bool addRecordRoute)
        {            
            // Create proxy context that will be responsible for forwarding request.
            SIP_ProxyContext proxyContext = new SIP_ProxyContext(
                this,
                transaction,
                request,
                addRecordRoute,
                m_ForkingMode,
                (this.ProxyMode & SIP_ProxyMode.B2BUA) != 0,
                false,
                false,
                requestContext.Targets.ToArray()
            );
            m_pProxyContexts.Add(proxyContext);

            return proxyContext;
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
        /// Gets owner SIP stack.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Stack Stack
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pStack; 
            }
        }

        /// <summary>
        /// Gets or sets proxy mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid combination modes passed.</exception>
        public SIP_ProxyMode ProxyMode
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_ProxyMode; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                                
                // Check for invalid mode ()
                if((value & SIP_ProxyMode.Statefull) != 0 && (value & SIP_ProxyMode.Stateless) != 0){
                    throw new ArgumentException("Proxy can't be at Statefull and Stateless at same time !");
                }

                m_ProxyMode = value;
            }
        }

        /// <summary>
        /// Gets or sets how proxy handle forking. This property applies for statefull proxy only.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_ForkingMode ForkingMode
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_ForkingMode; 
            }

            set{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_ForkingMode = value; 
            }
        }

        /// <summary>
        /// Gets SIP registrar server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Registrar Registrar
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRegistrar; 
            }
        }

        /// <summary>
        /// Gets SIP B2BUA server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_B2BUA B2BUA
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pB2BUA; 
            }
        }

        /// <summary>
        /// Gets SIP proxy request handlers collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        /// <remarks>
        /// NOTE: Handlers with lower index number are processed first.
        /// </remarks>
        public List<SIP_ProxyHandler> Handlers
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pHandlers; 
            }
        }
                
        #endregion

        #region Events Implementation
                                
        /// <summary>
        /// This event is raised when SIP proxy needs to know if specified request URI is local URI or remote URI.
        /// </summary>
        public event SIP_IsLocalUriEventHandler IsLocalUri = null;

        #region mehtod OnIsLocalUri

        /// <summary>
        /// Raises <b>IsLocalUri</b> event.
        /// </summary>
        /// <param name="uri">Request URI.</param>
        /// <returns>Returns true if server local URI, otherwise false.</returns>
        internal bool OnIsLocalUri(string uri)
        {
            if(this.IsLocalUri != null){
                return this.IsLocalUri(uri);
            }

            return true;
        }

        #endregion
        
        /// <summary>
        /// This event is raised when SIP proxy or registrar server needs to authenticate user.
        /// </summary>
        public event SIP_AuthenticateEventHandler Authenticate = null;
                
        #region method OnAuthenticate

        /// <summary>
        /// Is called by SIP proxy or registrar server when it needs to authenticate user.
        /// </summary>
        /// <param name="auth">Authentication context.</param>
        /// <returns></returns>
        internal SIP_AuthenticateEventArgs OnAuthenticate(Auth_HttpDigest auth)
        {
            SIP_AuthenticateEventArgs eArgs = new SIP_AuthenticateEventArgs(auth);
            if(this.Authenticate != null){
                this.Authenticate(eArgs);
            }

            return eArgs;
        }

        #endregion
               
        /// <summary>
        /// This event is raised when SIP proxy needs to know if specified local server address exists.
        /// </summary>
        public event SIP_AddressExistsEventHandler AddressExists = null;
                
        #region method OnAddressExists

        /// <summary>
        /// Is called by SIP proxy if it needs to check if specified address exists.
        /// </summary>
        /// <param name="address">SIP address to check.</param>
        /// <returns>Returns true if specified address exists, otherwise false.</returns>
        internal bool OnAddressExists(string address)
        {            
            if(this.AddressExists != null){
                return this.AddressExists(address);
            }
                        
            return false;
        }

        #endregion

        #endregion

    }
}
