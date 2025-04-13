using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;

using LumiSoft.Net.SIP;
using LumiSoft.Net.SIP.Stack;
using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class implements SIP request sender.
    /// </summary>
    /// <remarks>
    /// Request is sent using following methods:<br/>
    ///     *) If there is active data flow, it is used.
    ///     *) Request is sent as described in RFC 3261 [4](RFC 3263).
    /// </remarks>
    public class SIP_RequestSender : IDisposable
    {
        #region enum SIP_RequestSenderState

        private enum SIP_RequestSenderState
        {
            Initial,
            Starting,
            Started,
            Completed,
            Disposed
        }

        #endregion

        private object                  m_pLock        = new object();
        private SIP_RequestSenderState  m_State        = SIP_RequestSenderState.Initial;
        private bool                    m_IsStarted    = false;
        private SIP_Stack               m_pStack       = null;
        private SIP_Request             m_pRequest     = null;
        private List<NetworkCredential> m_pCredentials = null;
        private Queue<SIP_Hop>          m_pHops        = null;
        private SIP_ClientTransaction   m_pTransaction = null;
        private SIP_Flow                m_pFlow        = null;
        private object                  m_pTag         = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Owner stack.</param>
        /// <param name="request">SIP request.</param>
        /// <param name="flow">Active data flow what to try before RFC 3261 [4](RFC 3263) methods to use to send request.
        /// This value can be null.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stack</b> or <b>request</b> is null.</exception>
        internal SIP_RequestSender(SIP_Stack stack,SIP_Request request,SIP_Flow flow)
        {
            if(stack == null){
                throw new ArgumentNullException("stack");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }

            m_pStack   = stack;
            m_pRequest = request;
            m_pFlow    = flow;

            m_pCredentials = new List<NetworkCredential>();
            m_pHops = new Queue<SIP_Hop>();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            lock(m_pLock){
                if(m_State == SIP_RequestSenderState.Disposed){
                    return;
                }
                m_State = SIP_RequestSenderState.Disposed;

                OnDisposed();

                this.ResponseReceived = null;
                this.Completed = null;
                this.Disposed = null;
                
                m_pStack       = null;
                m_pRequest     = null;
                m_pCredentials = null;
                m_pHops        = null;
                m_pTransaction = null;
                m_pLock        = null;
            }
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
                m_pFlow = e.ClientTransaction.Request.Flow;

                if(e.Response.StatusCode == 401 || e.Response.StatusCode == 407){
                    // Check if authentication failed(We sent authorization data and it's challenged again, 
                    // probably user name or password inccorect)
                    bool hasFailedAuthorization = false;
                    foreach(SIP_t_Challenge challange in e.Response.WWWAuthenticate.GetAllValues()){
                        foreach(SIP_t_Credentials credentials in m_pTransaction.Request.Authorization.GetAllValues()){
                            if(new Auth_HttpDigest(challange.AuthData,"").Realm == new Auth_HttpDigest(credentials.AuthData,"").Realm){
                                hasFailedAuthorization = true;
                                break;
                            }
                        }
                    }
                    foreach(SIP_t_Challenge challange in e.Response.ProxyAuthenticate.GetAllValues()){
                        foreach(SIP_t_Credentials credentials in m_pTransaction.Request.ProxyAuthorization.GetAllValues()){
                            if(new Auth_HttpDigest(challange.AuthData,"").Realm == new Auth_HttpDigest(credentials.AuthData,"").Realm){
                                hasFailedAuthorization = true;
                                break;
                            }
                        }
                    }

                    // Authorization failed, pass response to UA.
                    if(hasFailedAuthorization){
                        OnResponseReceived(e.Response);
                    }
                    // Try to authorize challanges.
                    else{
                        SIP_Request request = m_pRequest.Copy();

                        /* RFC 3261 22.2.
                            When a UAC resubmits a request with its credentials after receiving a
                            401 (Unauthorized) or 407 (Proxy Authentication Required) response,
                            it MUST increment the CSeq header field value as it would normally
                            when sending an updated request.
                        */
                        request.CSeq = new SIP_t_CSeq(m_pStack.ConsumeCSeq(),request.CSeq.RequestMethod);

                        // All challanges authorized, resend request.
                        if(Authorize(request,e.Response,this.Credentials.ToArray())){
                            SIP_Flow flow  = m_pTransaction.Flow;
                            CleanUpActiveTransaction();            
                            SendToFlow(flow,request);
                        }
                        // We don't have credentials for one or more challenges.
                        else{
                            OnResponseReceived(e.Response);
                        }
                    }                   
                }
                else{
                    OnResponseReceived(e.Response);
                    if(e.Response.StatusCodeType != SIP_StatusCodeType.Provisional){
                        OnCompleted();
                    }
                }
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
                /* RFC 3261 8.1.2.
                    The UAC SHOULD follow the procedures defined in [4] for stateful
                    elements, trying each address until a server is contacted.  Each try
                    constitutes a new transaction, and therefore each carries a different
                    topmost Via header field value with a new branch parameter.
                    Furthermore, the transport value in the Via header field is set to
                    whatever transport was determined for the target server.
                */
                if(m_pHops.Count > 0){
                    CleanUpActiveTransaction();
                    SendToNextHop();
                }
                /* 8.1.3.1 Transaction Layer Errors
                    In some cases, the response returned by the transaction layer will
                    not be a SIP message, but rather a transaction layer error.  When a
                    timeout error is received from the transaction layer, it MUST be
                    treated as if a 408 (Request Timeout) status code has been received.
                    If a fatal transport error is reported by the transport layer
                    (generally, due to fatal ICMP errors in UDP or connection failures in
                    TCP), the condition MUST be treated as a 503 (Service Unavailable)
                    status code.                    
                */
                else{
                    OnResponseReceived(m_pStack.CreateResponse(SIP_ResponseCodes.x408_Request_Timeout,m_pRequest));
                    OnCompleted();
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
        private void ClientTransaction_TransportError(object sender,EventArgs e)
        {
            lock(m_pLock){
                /* RFC 3261 8.1.2.
                    The UAC SHOULD follow the procedures defined in [4] for stateful
                    elements, trying each address until a server is contacted.  Each try
                    constitutes a new transaction, and therefore each carries a different
                    topmost Via header field value with a new branch parameter.
                    Furthermore, the transport value in the Via header field is set to
                    whatever transport was determined for the target server.
                */
                if(m_pHops.Count > 0){
                    CleanUpActiveTransaction();
                    SendToNextHop();
                }
                /* RFC 3261 8.1.3.1 Transaction Layer Errors
                    In some cases, the response returned by the transaction layer will
                    not be a SIP message, but rather a transaction layer error.  When a
                    timeout error is received from the transaction layer, it MUST be
                    treated as if a 408 (Request Timeout) status code has been received.
                    If a fatal transport error is reported by the transport layer
                    (generally, due to fatal ICMP errors in UDP or connection failures in
                    TCP), the condition MUST be treated as a 503 (Service Unavailable)
                    status code.                    
                */
                else{
                    OnResponseReceived(m_pStack.CreateResponse(SIP_ResponseCodes.x503_Service_Unavailable + ": Transport error.",m_pRequest));
                    OnCompleted();
                }                
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts sending request.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>Start</b> method has alredy called.</exception>
        /// <exception cref="SIP_TransportException">Is raised when no transport hop(s) for request.</exception>
        public void Start()
        {
            lock(m_pLock){
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(m_IsStarted){
                    throw new InvalidOperationException("Start method has been already called.");
                }
                m_IsStarted = true;
                m_State = SIP_RequestSenderState.Starting;

                // Start may take so, process it on thread pool.
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state){
                    lock(m_pLock){
                        if(m_State == SIP_RequestSenderState.Disposed){
                            return;
                        }
                                                
                        /* RFC 3261 8.1.2 Sending the Request                
                            The destination for the request is then computed.  Unless there is
                            local policy specifying otherwise, the destination MUST be determined
                            by applying the DNS procedures described in [4] as follows.  If the
                            first element in the route set indicated a strict router (resulting
                            in forming the request as described in Section 12.2.1.1), the
                            procedures MUST be applied to the Request-URI of the request.
                            Otherwise, the procedures are applied to the first Route header field
                            value in the request (if one exists), or to the request's Request-URI
                            if there is no Route header field present.  These procedures yield an
                            ordered set of address, port, and transports to attempt.  Independent
                            of which URI is used as input to the procedures of [4], if the
                            Request-URI specifies a SIPS resource, the UAC MUST follow the
                            procedures of [4] as if the input URI were a SIPS URI.
                 
                            The UAC SHOULD follow the procedures defined in [4] for stateful
                            elements, trying each address until a server is contacted.  Each try
                            constitutes a new transaction, and therefore each carries a different
                            topmost Via header field value with a new branch parameter.
                            Furthermore, the transport value in the Via header field is set to
                            whatever transport was determined for the target server.
                        */
                                                                
                        // We never use strict, only loose route.
                        bool isStrictRoute = false;

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

                        try{
                            // Queue hops.
                            foreach(SIP_Hop hop in m_pStack.GetHops(uri,m_pRequest.ToByteData().Length,((SIP_Uri)m_pRequest.RequestLine.Uri).IsSecure)){
                                m_pHops.Enqueue(hop);
                            }
                        }
                        catch(Exception x){
                            OnTransportError(new SIP_TransportException("SIP hops resolving failed '" + x.Message + "'."));
                            OnCompleted();

                            return;
                        }

                        if(m_pHops.Count == 0){
                            OnTransportError(new SIP_TransportException("No target hops resolved for '" + uri + "'."));
                            OnCompleted();
                        }
                        else{
                            m_State = SIP_RequestSenderState.Started;

                            try{
                                if(m_pFlow != null){
                                    SendToFlow(m_pFlow,m_pRequest.Copy());

                                    return;
                                }
                            }
                            catch{
                                // Sending to specified flow failed, probably disposed, just try send to first hop.
                            }

                            SendToNextHop();
                        }
                    }
                }));
            }
        }

        #endregion

        #region method Cancel

        /// <summary>
        /// Cancels current request sending.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when request sending has not been started by <b>Start</b> method.</exception>
        public void Cancel()
        {
            // If sender is in starting state, we must wait that state to complete.
            while(m_State == SIP_RequestSenderState.Starting){
                Thread.Sleep(5);
            }

            lock(m_pLock){
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!m_IsStarted){
                    throw new InvalidOperationException("Request sending has not started, nothing to cancel.");
                }
                if(m_State != SIP_RequestSenderState.Started){
                    return;
                }
                
                m_pHops.Clear();                
            }

            // We may not call m_pTransaction.Cancel() in lock block, because deadlock can happen when transaction get response at same time.
            // Transaction waits lock for us and we wait lock to transaction.
            m_pTransaction.Cancel();
        }

        #endregion


        #region method Authorize

        /// <summary>
        /// Creates authorization for each challange in <b>response</b>.
        /// </summary>
        /// <param name="request">SIP request where to add authorization values.</param>
        /// <param name="response">SIP response which challanges to authorize.</param>
        /// <param name="credentials">Credentials for authorization.</param>
        /// <returns>Returns true if all challanges were authorized. If any of the challanges was not authorized, returns false.</returns>
        private bool Authorize(SIP_Request request,SIP_Response response,NetworkCredential[] credentials)
        {
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }
            if(credentials == null){
                throw new ArgumentNullException("credentials");
            }

            bool allAuthorized = true;

            #region WWWAuthenticate

            foreach(SIP_t_Challenge challange in response.WWWAuthenticate.GetAllValues()){
                Auth_HttpDigest authDigest = new Auth_HttpDigest(challange.AuthData,request.RequestLine.Method);

                // Serach credential for the specified challange.
                NetworkCredential credential = null;
                foreach(NetworkCredential c in credentials){
                    if(c.Domain.ToLower() == authDigest.Realm.ToLower()){
                        credential = c;
                        break;
                    }
                }
                // We don't have credential for this challange.
                if(credential == null){
                    allAuthorized = false;
                }
                // Authorize challange.
                else{
                    authDigest.UserName = credential.UserName;
                    authDigest.Password = credential.Password;
                    authDigest.CNonce   = Auth_HttpDigest.CreateNonce();
                    authDigest.Uri      = request.RequestLine.Uri.ToString();

                    request.Authorization.Add(authDigest.ToAuthorization());
                }
            }

            #endregion

            #region ProxyAuthenticate

            foreach(SIP_t_Challenge challange in response.ProxyAuthenticate.GetAllValues()){
                Auth_HttpDigest authDigest = new Auth_HttpDigest(challange.AuthData,request.RequestLine.Method);

                // Serach credential for the specified challange.
                NetworkCredential credential = null;
                foreach(NetworkCredential c in credentials){
                    if(c.Domain.ToLower() == authDigest.Realm.ToLower()){
                        credential = c;
                        break;
                    }
                }
                // We don't have credential for this challange.
                if(credential == null){
                    allAuthorized = false;
                }
                // Authorize challange.
                else{
                    authDigest.UserName = credential.UserName;
                    authDigest.Password = credential.Password;
                    authDigest.CNonce   = Auth_HttpDigest.CreateNonce();
                    authDigest.Uri      = request.RequestLine.Uri.ToString();

                    request.ProxyAuthorization.Add(authDigest.ToAuthorization());                    
                }
            }

            #endregion

            return allAuthorized;
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

            try{
                SIP_Hop hop = m_pHops.Dequeue();        
                SendToFlow(m_pStack.TransportLayer.GetOrCreateFlow(hop.Transport,null,hop.EndPoint),m_pRequest.Copy());
            }
            catch(ObjectDisposedException x){
                // Skip all exceptions if owner stack is disposed.
                if(m_pStack.State != SIP_StackState.Disposed){
                    throw x;
                }
            }
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

            #region Contact (RFC 3261 8.1.1.8)

            /*
                The Contact header field provides a SIP or SIPS URI that can be used
                to contact that specific instance of the UA for subsequent requests.
                The Contact header field MUST be present and contain exactly one SIP
                or SIPS URI in any request that can result in the establishment of a
                dialog.  For the methods defined in this specification, that includes
                only the INVITE request.  For these requests, the scope of the
                Contact is global.  That is, the Contact header field value contains
                the URI at which the UA would like to receive requests, and this URI
                MUST be valid even if used in subsequent requests outside of any
                dialogs.

                If the Request-URI or top Route header field value contains a SIPS
                URI, the Contact header field MUST contain a SIPS URI as well.
            */

            SIP_t_ContactParam contact = request.Contact.GetTopMostValue();

            // Add contact header If request-Method can establish dialog and contact header not present.            
            if(SIP_Utils.MethodCanEstablishDialog(request.RequestLine.Method) && contact == null){    
                SIP_Uri from = (SIP_Uri)request.From.Address.Uri;

                request.Contact.Add((flow.IsSecure ? "sips:" : "sip:" ) + from.User + "@" + flow.LocalPublicEP.ToString());

                // REMOVE ME: 22.10.2010
                //request.Contact.Add((flow.IsSecure ? "sips:" : "sip:" ) + from.User + "@" + m_pStack.TransportLayer.GetContactHost(flow).ToString());
            }
            // If contact SIP URI and host = auto-allocate, allocate it as needed.
            else if(contact != null && contact.Address.Uri is SIP_Uri && ((SIP_Uri)contact.Address.Uri).Host == "auto-allocate"){
                ((SIP_Uri)contact.Address.Uri).Host =  flow.LocalPublicEP.ToString();

                // REMOVE ME: 22.10.2010
                //((SIP_Uri)contact.Address.Uri).Host =  m_pStack.TransportLayer.GetContactHost(flow).ToString();
            }

            #endregion
         
            m_pTransaction = m_pStack.TransactionLayer.CreateClientTransaction(flow,request,true);  
            m_pTransaction.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(ClientTransaction_ResponseReceived);
            m_pTransaction.TimedOut += new EventHandler(ClientTransaction_TimedOut);
            m_pTransaction.TransportError += new EventHandler<ExceptionEventArgs>(ClientTransaction_TransportError);
 
            // Start transaction processing.
            m_pTransaction.Start();
        }

        #endregion

        #region method CleanUpActiveHop

        /// <summary>
        /// Cleans up active transaction.
        /// </summary>
        private void CleanUpActiveTransaction()
        {
            if(m_pTransaction != null){
                // Don't dispose transaction, transaction will dispose itself when done.
                // Otherwise for example failed INVITE won't linger in "Completed" state as it must be.
                // We just release Events processing, because you don't care about them any more.
                m_pTransaction.ResponseReceived -= new EventHandler<SIP_ResponseReceivedEventArgs>(ClientTransaction_ResponseReceived);
                m_pTransaction.TimedOut -= new EventHandler(ClientTransaction_TimedOut);
                m_pTransaction.TransportError -= new EventHandler<ExceptionEventArgs>(ClientTransaction_TransportError);

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
            get{ return m_State == SIP_RequestSenderState.Disposed; }
        }

        /// <summary>
        /// Gets if request sending has been started.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public bool IsStarted
        {
            get{
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_IsStarted; 
            }
        }

        /// <summary>
        /// Gets if request sender has complted sending request.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public bool IsCompleted
        {
            get{
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_State == SIP_RequestSenderState.Completed; 
            }
        }

        /// <summary>
        /// Gets owner stack.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Stack Stack
        {
            get{ 
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pStack; 
            }
        }

        /// <summary>
        /// Gets SIP request.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Request Request
        {
            get{
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRequest; 
            }
        }

        /// <summary>
        /// Gets SIP flow what was used to send request or null if request is not sent yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Flow Flow
        {
            get{
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pFlow; 
            }
        }

        /// <summary>
        /// Gets credentials collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public List<NetworkCredential> Credentials
        {
            get{ 
                if(m_State == SIP_RequestSenderState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pCredentials; 
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

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when this transaction has got response from target end point.
        /// </summary>
        public event EventHandler<SIP_ResponseReceivedEventArgs> ResponseReceived = null;

        #region method OnResponseReceived

        /// <summary>
        /// Raises ResponseReceived event.
        /// </summary>
        /// <param name="response">SIP response received.</param>
        private void OnResponseReceived(SIP_Response response)
        {
            if(this.ResponseReceived != null){
                this.ResponseReceived(this,new SIP_ResponseReceivedEventArgs(m_pStack,m_pTransaction,response));
            }
        }

        #endregion


        #region method OnTransportError

        /// <summary>
        /// Raises event <b>TransportError</b>.
        /// </summary>
        /// <param name="exception">Excption happened.</param>
        private void OnTransportError(Exception exception)
        {
            // TODO:
        }

        #endregion

        /// <summary>
        /// Is raised when sender has finished processing(got final-response or error).
        /// </summary>
        public event EventHandler Completed = null;

        #region method OnCompleted

        /// <summary>
        /// Raises event <b>Completed</b>.
        /// </summary>
        private void OnCompleted()
        {
            m_State = SIP_RequestSenderState.Completed;

            if(this.Completed != null){
                this.Completed(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// Is raised when this object has disposed.
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

        #endregion
    }
}
