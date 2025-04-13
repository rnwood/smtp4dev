using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using LumiSoft.Net.AUTH;
using LumiSoft.Net.Log;
using LumiSoft.Net.DNS;
using LumiSoft.Net.DNS.Client;
using LumiSoft.Net.SIP.Message;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Implements SIP stack.
    /// </summary>
    public class SIP_Stack
    {    
        private SIP_StackState               m_State              = SIP_StackState.Stopped;
        private SIP_TransportLayer           m_pTransportLayer    = null;
        private SIP_TransactionLayer         m_pTransactionLayer  = null;
        private string                       m_UserAgent          = null;
        private Auth_HttpDigest_NonceManager m_pNonceManager      = null;
        private List<SIP_Uri>                m_pProxyServers      = null;
        private string                       m_Realm              = "";
        private int                          m_CSeq               = 1;
        private int                          m_MaxForwards        = 70;
        private int                          m_MinExpireTime      = 1800;
        private List<string>                 m_pAllow             = null;
        private List<string>                 m_pSupported         = null;
        private int                          m_MaximumConnections = 0;
        private int                          m_MaximumMessageSize = 1000000;
        private int                          m_MinSessionExpires  = 90;
        private int                          m_SessionExpires     = 1800;
        private List<NetworkCredential>      m_pCredentials       = null;        
        private List<SIP_UA_Registration>    m_pRegistrations     = null;
        private SIP_t_CallID                 m_RegisterCallID     = null;
        private Logger                       m_pLogger            = null;
        private Dns_Client                   m_pDnsClient         = null;
        private int                          MTU                  = 1400;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_Stack()
        {            
            m_pTransportLayer = new SIP_TransportLayer(this);
            m_pTransactionLayer = new SIP_TransactionLayer(this);
            m_pNonceManager = new Auth_HttpDigest_NonceManager();
            m_pProxyServers = new List<SIP_Uri>();
            m_pRegistrations = new List<SIP_UA_Registration>();
            m_pCredentials = new List<NetworkCredential>();
            m_RegisterCallID = SIP_t_CallID.CreateCallID();
            
            m_pAllow = new List<string>();
            m_pAllow.AddRange(new string[]{"INVITE","ACK","CANCEL","BYE","MESSAGE"});

            m_pSupported = new List<string>();
                       
            m_pLogger = new Logger();

            m_pDnsClient = new Dns_Client();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            if(m_State == SIP_StackState.Disposed){
                return;
            }
            
            Stop();

            m_State = SIP_StackState.Disposed;

            // TODO: "clean" clean up with disposing state, wait some time transaction/dialogs to die, block new ones.

            // TODO: Currently stack switched Disposed state before all transactions has disposed, so some active 
            // transaction which accesses stack will get disposed exception.
                        
            // Release events.
            this.RequestReceived = null;
            this.ResponseReceived = null;
            this.Error = null;                      
            
            if(m_pTransactionLayer != null){
                m_pTransactionLayer.Dispose();
            }
            if(m_pTransportLayer != null){
                m_pTransportLayer.Dispose();
            }
            if(m_pNonceManager != null){
                m_pNonceManager.Dispose();
            }
            if(m_pLogger != null){
                m_pLogger.Dispose();
            }            
        }

        #endregion


        #region method Start

        /// <summary>
        /// Starts SIP stack.
        /// </summary>
        public void Start()
        {
            if(m_State == SIP_StackState.Started){
                return;
            }
            m_State = SIP_StackState.Started;

            m_pTransportLayer.Start();            
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stops SIP stack.
        /// </summary>
        public void Stop()
        {
            if(m_State != SIP_StackState.Started){
                return;
            }
            m_State = SIP_StackState.Stopping;

            /* Cleanup order:             
                *) Unregister registrations.
                *) Terminate dialogs.
                *) Wait while all active transactions has terminated or timeout reaches.
            */

            // Unregister registrations.
            foreach(SIP_UA_Registration reg in m_pRegistrations.ToArray()){
                reg.BeginUnregister(true);
            }

            // Terminate dialogs.
            foreach(SIP_Dialog dialog in m_pTransactionLayer.Dialogs){
                dialog.Terminate();
            }

            // Wait while all active transactions has completed.
            DateTime start = DateTime.Now;
            while(true){
                bool activeTransactions = false;
                foreach(SIP_Transaction tr in m_pTransactionLayer.Transactions){
                    // We have active transactions.
                    if(tr.State == SIP_TransactionState.WaitingToStart || tr.State == SIP_TransactionState.Calling || tr.State == SIP_TransactionState.Proceeding || tr.State == SIP_TransactionState.Trying){
                        activeTransactions = true;
                
                        break;
                    }
                }

                if(activeTransactions){
                    System.Threading.Thread.Sleep(500);

                    // Timeout.
                    if(((TimeSpan)(DateTime.Now - start)).Seconds > 10){
                        break;
                    }
                }
                else{
                    break;
                }
            }
            
            // REMOVE ME: Dispose Transaction layer instead.
            foreach(SIP_Transaction tr in m_pTransactionLayer.Transactions){
                try{
                    tr.Dispose();
                }
                catch{
                }
            }

            
            m_pTransportLayer.Stop();

            m_State = SIP_StackState.Stopped;            
        }

        #endregion


        #region method CreateRequest
        
        /// <summary>
        /// Creates new out-off dialog SIP request.
        /// </summary>
        /// <param name="method">SIP request-method.</param>
        /// <param name="to">Recipient address. For example: sip:user@domain.com</param>
        /// <param name="from">Senders address. For example: sip:user@domain.com</param>
        /// <returns>Returns created request.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>method</b>,<b>to</b> or <b>from</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SIP_Request CreateRequest(string method,SIP_t_NameAddress to,SIP_t_NameAddress from)
        {
            if(m_State == SIP_StackState.Disposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(method == null){
                throw new ArgumentNullException("method");
            }
            if(method == ""){
                throw new ArgumentException("Argument 'method' value must be specified.");
            }
            if(to == null){
                throw new ArgumentNullException("to");
            }
            if(from == null){
                throw new ArgumentNullException("from");
            }

            method = method.ToUpper();

            /* RFC 3261 8.1.1 Generating the Request

                A valid SIP request formulated by a UAC MUST, at a minimum, contain
                the following header fields: To, From, CSeq, Call-ID, Max-Forwards,
                and Via; all of these header fields are mandatory in all SIP
                requests.  These six header fields are the fundamental building
                blocks of a SIP message, as they jointly provide for most of the
                critical message routing services including the addressing of
                messages, the routing of responses, limiting message propagation,
                ordering of messages, and the unique identification of transactions.
                These header fields are in addition to the mandatory request line,
                which contains the method, Request-URI, and SIP version.
            */

            SIP_Request request = new SIP_Request(method);

            #region Request-URI (section 8.1.1.1)

            /*
                The initial Request-URI of the message SHOULD be set to the value of
                the URI in the To field.  One notable exception is the REGISTER
                method; behavior for setting the Request-URI of REGISTER is given in
                Section 10.
            */
            
            request.RequestLine.Uri = to.Uri;

            #endregion

            #region To (section 8.1.1.2)

            /*
                The To header field first and foremost specifies the desired
                "logical" recipient of the request, or the address-of-record of the
                user or resource that is the target of this request.  This may or may
                not be the ultimate recipient of the request.  The To header field
                MAY contain a SIP or SIPS URI, but it may also make use of other URI
                schemes (the tel URL (RFC 2806 [9]), for example) when appropriate.
            */

            SIP_t_To t = new SIP_t_To(to);
            request.To = t;

            #endregion

            #region From (section 8.1.1.3)

            /*
                The From header field indicates the logical identity of the initiator
                of the request, possibly the user's address-of-record.  Like the To
                header field, it contains a URI and optionally a display name.  It is
                used by SIP elements to determine which processing rules to apply to
                a request (for example, automatic call rejection).  As such, it is
                very important that the From URI not contain IP addresses or the FQDN
                of the host on which the UA is running, since these are not logical
                names.

                The From header field allows for a display name.  A UAC SHOULD use
                the display name "Anonymous", along with a syntactically correct, but
                otherwise meaningless URI (like sip:thisis@anonymous.invalid), if the
                identity of the client is to remain hidden.
            
                The From field MUST contain a new "tag" parameter, chosen by the UAC.
                See Section 19.3 for details on choosing a tag.
            */

            SIP_t_From f = new SIP_t_From(from);
            f.Tag = SIP_Utils.CreateTag();
            request.From = f;

            #endregion
                        
            #region CallID (section 8.1.1.4)

            /*
                The Call-ID header field acts as a unique identifier to group
                together a series of messages.  It MUST be the same for all requests
                and responses sent by either UA in a dialog.  It SHOULD be the same
                in each registration from a UA.
            */

            if(method == SIP_Methods.REGISTER){
                request.CallID = m_RegisterCallID.ToStringValue();
            }
            else{
                request.CallID = SIP_t_CallID.CreateCallID().ToStringValue();
            }

            #endregion

            #region CSeq (section 8.1.1.5)

            /*
                The CSeq header field serves as a way to identify and order
                transactions.  It consists of a sequence number and a method.  The
                method MUST match that of the request.  For non-REGISTER requests
                outside of a dialog, the sequence number value is arbitrary.  The
                sequence number value MUST be expressible as a 32-bit unsigned
                integer and MUST be less than 2**31.  As long as it follows the above
                guidelines, a client may use any mechanism it would like to select
                CSeq header field values.
            */

            request.CSeq = new SIP_t_CSeq(ConsumeCSeq(),method);

            #endregion

            #region Max-Forwards (section 8.1.1.6)

            request.MaxForwards = m_MaxForwards;

            #endregion

            #region Allow,Supported (section 13.2.1)

            // RFC requires these headers for dialog establishing requests only.
            // We just add these to every request - this is won't violate RFC.

            request.Allow.Add(SIP_Utils.ListToString(m_pAllow));

            if(m_pSupported.Count > 0){
                request.Supported.Add(SIP_Utils.ListToString(m_pAllow));
            }

            #endregion


            #region Pre-configured route (proxy server)

            // section 8.1.2 suggests to use pre-configured route for proxy.

            foreach(SIP_Uri proxy in m_pProxyServers){
                request.Route.Add(proxy.ToString());
            }

            #endregion
                                    

            #region User-Agent

            if(!string.IsNullOrEmpty(m_UserAgent)){
                request.UserAgent = m_UserAgent;
            }

            #endregion

            return request;
        }

        #endregion

        #region method CreateRequestSender

        /// <summary>
        /// Creates SIP request sender for the specified request.
        /// </summary>
        /// <param name="request">SIP request.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>request</b> is null reference.</exception>
        public SIP_RequestSender CreateRequestSender(SIP_Request request)
        {
            return CreateRequestSender(request,null);
        }

        /// <summary>
        /// Creates SIP request sender for the specified request.
        /// </summary>
        /// <param name="request">SIP request.</param>
        /// <param name="flow">Data flow.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>request</b> is null reference.</exception>
        internal SIP_RequestSender CreateRequestSender(SIP_Request request,SIP_Flow flow)
        {
            if(m_State == SIP_StackState.Disposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }

            SIP_RequestSender sender = new SIP_RequestSender(this,request,flow);
            sender.Credentials.AddRange(m_pCredentials);

            return sender;
        }

        #endregion

        #region method ConsumeCSeq

        /// <summary>
        /// Consumes current CSeq number and increments it by 1.
        /// </summary>
        /// <returns>Returns CSeq number.</returns>
        public int ConsumeCSeq()
        {
            return m_CSeq++;
        }

        #endregion

        #region method CreateResponse

        /// <summary>
        /// Creates response for the specified request.
        /// </summary>
        /// <param name="statusCode_reasonText">Status-code reasontext.</param>
        /// <param name="request">SIP request.</param>
        /// <returns>Returns created response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>statusCode_reasonText</b> or <b>request</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when request is ACK-request. ACK request is response less.</exception>
        public SIP_Response CreateResponse(string statusCode_reasonText,SIP_Request request)
        {
            return CreateResponse(statusCode_reasonText,request,null);
        }

        /// <summary>
        /// Creates response for the specified request.
        /// </summary>
        /// <param name="statusCode_reasonText">Status-code reasontext.</param>
        /// <param name="request">SIP request.</param>
        /// <param name="flow">Data flow what sends response. This value is used to construct Contact: header value. 
        /// This value can be null, but then adding Contact: header is response sender responsibility.</param>
        /// <returns>Returns created response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>statusCode_reasonText</b> or <b>request</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when request is ACK-request. ACK request is response less.</exception>
        public SIP_Response CreateResponse(string statusCode_reasonText,SIP_Request request,SIP_Flow flow)
        {
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(request.RequestLine.Method == SIP_Methods.ACK){
                throw new InvalidOperationException("ACK is responseless request !");
            }

            /* RFC 3261 8.2.6.1.
                When a 100 (Trying) response is generated, any Timestamp header field
                present in the request MUST be copied into this 100 (Trying)
                response.
              
               RFC 3261 8.2.6.2.
                The From field of the response MUST equal the From header field of
                the request.  The Call-ID header field of the response MUST equal the
                Call-ID header field of the request.  The CSeq header field of the
                response MUST equal the CSeq field of the request.  The Via header
                field values in the response MUST equal the Via header field values
                in the request and MUST maintain the same ordering.

                If a request contained a To tag in the request, the To header field
                in the response MUST equal that of the request.  However, if the To
                header field in the request did not contain a tag, the URI in the To
                header field in the response MUST equal the URI in the To header
                field; additionally, the UAS MUST add a tag to the To header field in
                the response (with the exception of the 100 (Trying) response, in
                which a tag MAY be present).  This serves to identify the UAS that is
                responding, possibly resulting in a component of a dialog ID.  The
                same tag MUST be used for all responses to that request, both final
                and provisional (again excepting the 100 (Trying)).  Procedures for
                the generation of tags are defined in Section 19.3.
            
               RFC 3261 12.1.1.
                When a UAS responds to a request with a response that establishes a
                dialog (such as a 2xx to INVITE), the UAS MUST copy all Record-Route
                header field values from the request into the response (including the
                URIs, URI parameters, and any Record-Route header field parameters,
                whether they are known or unknown to the UAS) and MUST maintain the
                order of those values.            
            */

            SIP_Response response = new SIP_Response(request);
            response.StatusCode_ReasonPhrase = statusCode_reasonText;
            foreach(SIP_t_ViaParm via in request.Via.GetAllValues()){
                response.Via.Add(via.ToStringValue());
            }
            response.From   = request.From;
            response.To     = request.To;
            if(request.To.Tag == null){
                response.To.Tag = SIP_Utils.CreateTag();
            }
            response.CallID = request.CallID;
            response.CSeq   = request.CSeq;

            #region Allow,Supported (section 13.2.1)

            // RFC requires these headers for dialog establishing requests only.
            // We just add these to every request - this is won't violate RFC.

            response.Allow.Add(SIP_Utils.ListToString(m_pAllow));

            if(m_pSupported.Count > 0){
                response.Supported.Add(SIP_Utils.ListToString(m_pAllow));
            }

            #endregion

            #region User-Agent

            if(!string.IsNullOrEmpty(m_UserAgent)){
                request.UserAgent = m_UserAgent;
            }

            #endregion

            if(SIP_Utils.MethodCanEstablishDialog(request.RequestLine.Method)){
                foreach(SIP_t_AddressParam route in request.RecordRoute.GetAllValues()){
                    response.RecordRoute.Add(route.ToStringValue());
                }
                
                if(response.Contact.GetTopMostValue() ==  null && flow != null){
                    string user = ((SIP_Uri)response.To.Address.Uri).User;
                    response.Contact.Add((flow.IsSecure ? "sips:" : "sip:") + user + "@" + flow.LocalPublicEP.ToString());
                }
            }
                        
            return response;
        }

        #endregion

        #region method GetHops

        /// <summary>
        /// Gets target hops(address,port,transport) of the specified URI.
        /// </summary>
        /// <param name="uri">Target URI.</param>
        /// <param name="messageSize">SIP message size.</param>
        /// <param name="forceTLS">If true only TLS hops are returned.</param>
        /// <returns>Returns target hops(address,port,transport) of the specified URI.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        public SIP_Hop[] GetHops(SIP_Uri uri,int messageSize,bool forceTLS)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            List<SIP_Hop>    retVal                 = new List<SIP_Hop>();
            string           transport              = "";
            bool             transportSetExplicitly = false;
            List<DNS_rr_SRV> targetSRV              = new List<DNS_rr_SRV>();

            #region RFC 3263 4.1 Selecting a Transport Protocol

            /* 4.1 Selecting a Transport Protocol

                If the URI specifies a transport protocol in the transport parameter,
                that transport protocol SHOULD be used.

                Otherwise, if no transport protocol is specified, but the TARGET is a
                numeric IP address, the client SHOULD use UDP for a SIP URI, and TCP
                for a SIPS URI.  Similarly, if no transport protocol is specified,
                and the TARGET is not numeric, but an explicit port is provided, the
                client SHOULD use UDP for a SIP URI, and TCP for a SIPS URI.  This is
                because UDP is the only mandatory transport in RFC 2543 [6], and thus
                the only one guaranteed to be interoperable for a SIP URI.  It was
                also specified as the default transport in RFC 2543 when no transport
                was present in the SIP URI.  However, another transport, such as TCP,
                MAY be used if the guidelines of SIP mandate it for this particular
                request.  That is the case, for example, for requests that exceed the
                path MTU.

                Otherwise, if no transport protocol or port is specified, and the
                target is not a numeric IP address, the client SHOULD perform a NAPTR
                query for the domain in the URI.  The services relevant for the task
                of transport protocol selection are those with NAPTR service fields
                with values "SIP+D2X" and "SIPS+D2X", where X is a letter that
                corresponds to a transport protocol supported by the domain.  This
                specification defines D2U for UDP, D2T for TCP, and D2S for SCTP.  We
                also establish an IANA registry for NAPTR service name to transport
                protocol mappings.

                These NAPTR records provide a mapping from a domain to the SRV record
                for contacting a server with the specific transport protocol in the
                NAPTR services field.  The resource record will contain an empty
                regular expression and a replacement value, which is the SRV record
                for that particular transport protocol.  If the server supports
                multiple transport protocols, there will be multiple NAPTR records,
                each with a different service value.  As per RFC 2915 [3], the client
                discards any records whose services fields are not applicable.  For
                the purposes of this specification, several rules are defined.

                First, a client resolving a SIPS URI MUST discard any services that
                do not contain "SIPS" as the protocol in the service field.  The
                converse is not true, however.  A client resolving a SIP URI SHOULD
                retain records with "SIPS" as the protocol, if the client supports
                TLS.  Second, a client MUST discard any service fields that identify
                a resolution service whose value is not "D2X", for values of X that
                indicate transport protocols supported by the client.  The NAPTR
                processing as described in RFC 2915 will result in the discovery of
                the most preferred transport protocol of the server that is supported
                by the client, as well as an SRV record for the server.  It will also
                allow the client to discover if TLS is available and its preference
                for its usage.

                As an example, consider a client that wishes to resolve
                sip:user@example.com.  The client performs a NAPTR query for that
                domain, and the following NAPTR records are returned:

                ;          order pref flags service      regexp  replacement
                    IN NAPTR 50   50  "s"  "SIPS+D2T"     ""  _sips._tcp.example.com.
                    IN NAPTR 90   50  "s"  "SIP+D2T"      ""  _sip._tcp.example.com
                    IN NAPTR 100  50  "s"  "SIP+D2U"      ""  _sip._udp.example.com.

                This indicates that the server supports TLS over TCP, TCP, and UDP,
                in that order of preference.  Since the client supports TCP and UDP,
                TCP will be used, targeted to a host determined by an SRV lookup of
                _sip._tcp.example.com.  That lookup would return:

                ;;          Priority Weight Port   Target
                    IN SRV  0        1      5060   server1.example.com
                    IN SRV  0        2      5060   server2.example.com

                If a SIP proxy, redirect server, or registrar is to be contacted
                through the lookup of NAPTR records, there MUST be at least three
                records - one with a "SIP+D2T" service field, one with a "SIP+D2U"
                service field, and one with a "SIPS+D2T" service field.  The records
                with SIPS as the protocol in the service field SHOULD be preferred
                (i.e., have a lower value of the order field) above records with SIP
                as the protocol in the service field.  A record with a "SIPS+D2U"
                service field SHOULD NOT be placed into the DNS, since it is not
                possible to use TLS over UDP.

                It is not necessary for the domain suffixes in the NAPTR replacement
                field to match the domain of the original query (i.e., example.com
                above).  However, for backwards compatibility with RFC 2543, a domain
                MUST maintain SRV records for the domain of the original query, even
                if the NAPTR record is in a different domain.  As an example, even
                though the SRV record for TCP is _sip._tcp.school.edu, there MUST
                also be an SRV record at _sip._tcp.example.com.

                RFC 2543 will look up the SRV records for the domain directly.  If
                these do not exist because the NAPTR replacement points to a
                different domain, the client will fail.

                For NAPTR records with SIPS protocol fields, (if the server is using
                a site certificate), the domain name in the query and the domain name
                in the replacement field MUST both be valid based on the site
                certificate handed out by the server in the TLS exchange.  Similarly,
                the domain name in the SRV query and the domain name in the target in
                the SRV record MUST both be valid based on the same site certificate.
                Otherwise, an attacker could modify the DNS records to contain
                replacement values in a different domain, and the client could not
                validate that this was the desired behavior or the result of an
                attack.

                If no NAPTR records are found, the client constructs SRV queries for
                those transport protocols it supports, and does a query for each.
                Queries are done using the service identifier "_sip" for SIP URIs and
                "_sips" for SIPS URIs.  A particular transport is supported if the
                query is successful.  The client MAY use any transport protocol it
                desires which is supported by the server.

                This is a change from RFC 2543.  It specified that a client would
                lookup SRV records for all transports it supported, and merge the
                priority values across those records.  Then, it would choose the
                most preferred record.

                If no SRV records are found, the client SHOULD use TCP for a SIPS
                URI, and UDP for a SIP URI.  However, another transport protocol,
                such as TCP, MAY be used if the guidelines of SIP mandate it for this
                particular request.  That is the case, for example, for requests that
                exceed the path MTU.
             */
                    
            // TLS usage demanded explicitly.
            if(forceTLS){
                transportSetExplicitly = true;
                transport = SIP_Transport.TLS;
            }
            // If the URI specifies a transport protocol in the transport parameter, that transport protocol SHOULD be used.
            else if(uri.Param_Transport != null){
                transportSetExplicitly = true;
                transport = uri.Param_Transport;
            }
            /*  If no transport protocol is specified, but the TARGET is a numeric IP address, 
                the client SHOULD use UDP for a SIP URI, and TCP for a SIPS URI. Similarly, 
                if no transport protocol is specified, and the TARGET is not numeric, but 
                an explicit port is provided, the client SHOULD use UDP for a SIP URI, and 
                TCP for a SIPS URI. However, another transport, such as TCP, MAY be used if 
                the guidelines of SIP mandate it for this particular request. That is the case, 
                for example, for requests that exceed the path MTU.
            */
            else if(Net_Utils.IsIPAddress(uri.Host) || uri.Port != -1){
                if(uri.IsSecure){
                    transport = SIP_Transport.TLS;
                }
                else if(messageSize > MTU){
                    transport = SIP_Transport.TCP;
                }
                else{
                   transport = SIP_Transport.UDP;
                }
            }
            else{
                DnsServerResponse response = null;
                /*
                DnsServerResponse response = m_pDnsClient.Query(uri.Host,QTYPE.NAPTR);
                // NAPTR records available.
                if(response.GetNAPTRRecords().Length > 0){
                    // TODO: Choose suitable here
                    // 1) If SIPS get SIPS if possible.
                    // 2) If message size > MTU, try to use TCP.
                    // 3) Otherwise use UDP.
                    if(uri.IsSecure){
                        // Get SIPS+D2T records.
                    }
                    else if(messageSize > MTU){
                        // Get SIP+D2T records.
                    }
                    else{
                        // Get SIP+D2U records.
                    }
                }
                else{*/
                    Dictionary<string,DNS_rr_SRV[]> supportedTransports = new Dictionary<string,DNS_rr_SRV[]>();
                    bool                            srvRecordsAvailable = false;

                    // Query SRV to see what protocols are supported.
                    response = m_pDnsClient.Query("_sips._tcp." + uri.Host,DNS_QType.SRV);
                    if(response.GetSRVRecords().Length > 0){
                        srvRecordsAvailable = true;
                        supportedTransports.Add(SIP_Transport.TLS,response.GetSRVRecords());
                    }
                    response = m_pDnsClient.Query("_sip._tcp." + uri.Host,DNS_QType.SRV);
                    if(response.GetSRVRecords().Length > 0){
                        srvRecordsAvailable = true;
                        supportedTransports.Add(SIP_Transport.TCP,response.GetSRVRecords());
                    }
                    response = m_pDnsClient.Query("_sip._udp." + uri.Host,DNS_QType.SRV);
                    if(response.GetSRVRecords().Length > 0){
                        srvRecordsAvailable = true;
                        supportedTransports.Add(SIP_Transport.UDP,response.GetSRVRecords());
                    }

                    // SRV records available.
                    if(srvRecordsAvailable){
                        if(uri.IsSecure){
                            if(supportedTransports.ContainsKey(SIP_Transport.TLS)){
                                transport = SIP_Transport.TLS;
                                targetSRV.AddRange(supportedTransports[SIP_Transport.TLS]);
                            }
                            // Target won't support SIPS.
                            else{
                                // TODO: What to do ?
                            }
                        }
                        else if(messageSize > MTU){
                            if(supportedTransports.ContainsKey(SIP_Transport.TCP)){
                                transport = SIP_Transport.TCP;
                                targetSRV.AddRange(supportedTransports[SIP_Transport.TCP]);
                            }
                            else if(supportedTransports.ContainsKey(SIP_Transport.TLS)){
                                transport = SIP_Transport.TLS;
                                targetSRV.AddRange(supportedTransports[SIP_Transport.TLS]);
                            }
                            // Target support UDP only, but TCP is required.
                            else{
                                // TODO: What to do ?
                            }
                        }
                        else{
                            if(supportedTransports.ContainsKey(SIP_Transport.UDP)){
                                transport = SIP_Transport.UDP;
                                targetSRV.AddRange(supportedTransports[SIP_Transport.UDP]);
                            }
                            else if(supportedTransports.ContainsKey(SIP_Transport.TCP)){
                                transport = SIP_Transport.TCP;
                                targetSRV.AddRange(supportedTransports[SIP_Transport.TCP]);
                            }
                            else{
                                transport = SIP_Transport.TLS;
                                targetSRV.AddRange(supportedTransports[SIP_Transport.TLS]);
                            }
                        }
                    }
                    /*  If no SRV records are found, the client SHOULD use TCP for a SIPS
                        URI, and UDP for a SIP URI.  However, another transport protocol,
                        such as TCP, MAY be used if the guidelines of SIP mandate it for this
                        particular request.  That is the case, for example, for requests that
                        exceed the path MTU.
                    */
                    else{
                        if(uri.IsSecure){
                            transport = SIP_Transport.TLS;
                        }
                        else if(messageSize > MTU){
                            transport = SIP_Transport.TCP;
                        }
                        else{
                            transport = SIP_Transport.UDP;
                        }
                    }
                //}
            }            

            #endregion

            #region RFC 3263 4.2 Determining Port and IP Address

            /* 4.2 Determining Port and IP Address

                If TARGET is a numeric IP address, the client uses that address.  If
                the URI also contains a port, it uses that port.  If no port is
                specified, it uses the default port for the particular transport
                protocol.

                If the TARGET was not a numeric IP address, but a port is present in
                the URI, the client performs an A or AAAA record lookup of the domain
                name.  The result will be a list of IP addresses, each of which can
                be contacted at the specific port from the URI and transport protocol
                determined previously.  The client SHOULD try the first record.  If
                an attempt should fail, based on the definition of failure in Section
                4.3, the next SHOULD be tried, and if that should fail, the next
                SHOULD be tried, and so on.

                This is a change from RFC 2543.  Previously, if the port was
                explicit, but with a value of 5060, SRV records were used.  Now, A
                or AAAA records will be used.

                If the TARGET was not a numeric IP address, and no port was present
                in the URI, the client performs an SRV query on the record returned
                from the NAPTR processing of Section 4.1, if such processing was
                performed.  If it was not, because a transport was specified
                explicitly, the client performs an SRV query for that specific
                transport, using the service identifier "_sips" for SIPS URIs.  For a
                SIP URI, if the client wishes to use TLS, it also uses the service
                identifier "_sips" for that specific transport, otherwise, it uses
                "_sip".  If the NAPTR processing was not done because no NAPTR
                records were found, but an SRV query for a supported transport
                protocol was successful, those SRV records are selected. Irregardless
                of how the SRV records were determined, the procedures of RFC 2782,
                as described in the section titled "Usage rules" are followed,
                augmented by the additional procedures of Section 4.3 of this
                document.

                If no SRV records were found, the client performs an A or AAAA record
                lookup of the domain name.  The result will be a list of IP
                addresses, each of which can be contacted using the transport
                protocol determined previously, at the default port for that
                transport.  Processing then proceeds as described above for an
                explicit port once the A or AAAA records have been looked up.
            */

            if(Net_Utils.IsIPAddress(uri.Host)){
                if(uri.Port != -1){
                    retVal.Add(new SIP_Hop(IPAddress.Parse(uri.Host),uri.Port,transport));
                }
                else if(forceTLS || uri.IsSecure){
                    retVal.Add(new SIP_Hop(IPAddress.Parse(uri.Host),5061,transport));
                }
                else{
                    retVal.Add(new SIP_Hop(IPAddress.Parse(uri.Host),5060,transport));
                }
            }
            else if(uri.Port != -1){
                foreach(IPAddress ip in m_pDnsClient.GetHostAddresses(uri.Host)){
                    retVal.Add(new SIP_Hop(ip,uri.Port,transport));
                }
            }
            else{
                //if(naptrRecords){
                    // We need to get (IP:Port)'s foreach SRV record.
                    //DnsServerResponse response = m_pDnsClient.Query("??? need NAPTR value here",QTYPE.SRV);
                //}    
                if(transportSetExplicitly){
                    DnsServerResponse response = null;
                    if(transport == SIP_Transport.TLS){
                        response = m_pDnsClient.Query("_sips._tcp." + uri.Host,DNS_QType.SRV);
                    }
                    else if(transport == SIP_Transport.TCP){
                        response = m_pDnsClient.Query("_sip._tcp." + uri.Host,DNS_QType.SRV);
                    }
                    else{
                        response = m_pDnsClient.Query("_sip._udp." + uri.Host,DNS_QType.SRV);
                    }
                    targetSRV.AddRange(response.GetSRVRecords());
                }                         

                // We have SRV records, resovle them to (IP:Port)'s.
                if(targetSRV.Count > 0){
                    foreach(DNS_rr_SRV record in targetSRV){
                        if(Net_Utils.IsIPAddress(record.Target)){
                            retVal.Add(new SIP_Hop(IPAddress.Parse(record.Target),record.Port,transport));
                        }
                        else{
                            foreach(IPAddress ip in m_pDnsClient.GetHostAddresses(record.Target)){
                                retVal.Add(new SIP_Hop(ip,record.Port,transport));
                            }
                        }
                    }
                }
                // No SRV recors, just use A and AAAA records.
                else{
                    int port = 5060;
                    if(transport == SIP_Transport.TLS){
                        port = 5061;
                    }

                    foreach(IPAddress ip in m_pDnsClient.GetHostAddresses(uri.Host)){
                        retVal.Add(new SIP_Hop(ip,port,transport));
                    }
                }
            }

            #endregion

            return retVal.ToArray();
        }

        #endregion

        #region method CreateRegistration

        /// <summary>
        /// Creates new registration.
        /// </summary>
        /// <param name="server">Registrar server URI. For example: sip:domain.com.</param>
        /// <param name="aor">Registration address of record. For example: user@domain.com.</param>
        /// <param name="contact">Contact URI.</param>
        /// <param name="expires">Gets after how many seconds reigisration expires.</param>
        /// <returns>Returns created registration.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>server</b>,<b>aor</b> or <b>contact</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SIP_UA_Registration CreateRegistration(SIP_Uri server,string aor,AbsoluteUri contact,int expires)
        {
            if(m_State == SIP_StackState.Disposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(server == null){
                throw new ArgumentNullException("server");
            }
            if(aor == null){
                throw new ArgumentNullException("aor");
            }
            if(aor == string.Empty){
                throw new ArgumentException("Argument 'aor' value must be specified.");
            }
            if(contact == null){
                throw new ArgumentNullException("contact");
            }

            lock(m_pRegistrations){
                SIP_UA_Registration registration = new SIP_UA_Registration(this,server,aor,contact,expires);
                registration.Disposed += new EventHandler(delegate(object s,EventArgs e){
                    if(m_State != SIP_StackState.Disposed){
                        m_pRegistrations.Remove(registration);
                    }
                });
                m_pRegistrations.Add(registration);

                return registration;
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets stack state.
        /// </summary>
        public SIP_StackState State
        {
            get{ return m_State; }
        }

        /// <summary>
        /// Gets transport layer what is used to receive and send requests and responses.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public SIP_TransportLayer TransportLayer
        {
            get{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pTransportLayer; 
            }
        }

        /// <summary>
        /// Gets transaction layer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public SIP_TransactionLayer TransactionLayer
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pTransactionLayer; 
            }
        }

        /// <summary>
        /// Gets or sets User-Agent value. Value null menas not specified.
        /// </summary>
        public string UserAgent
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_UserAgent; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_UserAgent = value;
            }
        }

        /// <summary>
        /// Gets digest authentication nonce manager.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public Auth_HttpDigest_NonceManager DigestNonceManager
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pNonceManager; 
            }
        }

        /// <summary>
        /// Gets or sets STUN server name or IP address. This value must be filled if SIP stack is running behind a NAT.
        /// </summary>
        public string StunServer
        {
            get{ return m_pTransportLayer.StunServer; }

            set{
                m_pTransportLayer.StunServer = value;
            }
        }

        /// <summary>
        /// Gets SIP outbound proxy servers collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public List<SIP_Uri> ProxyServers
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pProxyServers; 
            }
        }

        /// <summary>
        /// Gets or sets SIP <b>realm</b> value. Mostly this value is used by <b>digest</b> authentication.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null reference is passed.</exception>
        public string Realm
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_Realm; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value == null){
                    throw new ArgumentNullException();
                }

                m_Realm = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum forwards SIP request may have.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when value contains invalid value.</exception>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public int MaxForwards
        {
            get{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MaxForwards; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 1){
                    throw new ArgumentException("Value must be > 0.");
                }

                m_MaxForwards = value;
            }
        }

        /// <summary>
        /// Gets or sets minimum expire time in seconds what server allows.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int MinimumExpireTime
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_MinExpireTime; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 10){
                    throw new ArgumentException("Property MinimumExpireTime value must be >= 10 !");
                }

                m_MinExpireTime = value;
            }
        }

        /// <summary>
        /// Gets stack supported methods list.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        /// <remarks>This value is appended to <see cref="CreateRequest"/> created request <b>Allow:</b> header.</remarks>
        public List<string> Allow
        {
            get{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pAllow; 
            }
        }

        /// <summary>
        /// Gets stack supported extentions list.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        /// <remarks>This value is appended to <see cref="CreateRequest"/> created request <b>Supported:</b> header.</remarks>
        public List<string> Supported
        {
            get{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSupported; 
            }
        }

        /// <summary>
        /// Gets or sets how many cunncurent connections allowed. Value 0 means not limited. This is used only for TCP based connections.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int MaximumConnections
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_MaximumConnections; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 1){
                    m_MaximumConnections = 0;
                }
                else{
                    m_MaximumConnections = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets maximum allowed SIP message size in bytes. This is used only for TCP based connections.
        /// Value 0 means unlimited.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int MaximumMessageSize
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_MaximumMessageSize; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 1){
                    m_MaximumMessageSize = 0;
                }
                else{
                    m_MaximumMessageSize = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets minimum session expires value in seconds. NOTE: Minimum value is 90 !
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int MinimumSessionExpries
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_MinSessionExpires; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 90){
                    throw new ArgumentException("Minimum session expires value must be >= 90 !");
                }

                m_MinSessionExpires = value;
            }
        }

        /// <summary>
        /// Gets or sets session expires value in seconds. NOTE: This value can't be smaller than MinimumSessionExpries.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int SessionExpries
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_SessionExpires; 
            }

            set{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 90){
                    throw new ArgumentException("Session expires value can't be < MinimumSessionExpries value !");
                }

                m_SessionExpires = value;
            }
        }

        /// <summary>
        /// Gets credentials collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public List<NetworkCredential> Credentials
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pCredentials; 
            }
        }

        /// <summary>
        /// Gets or sets socket bind info. Use this property to specify on which protocol,IP,port server 
        /// listnes and also if connections is SSL.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public IPBindInfo[] BindInfo
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pTransportLayer.BindInfo;
            }

            set{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                m_pTransportLayer.BindInfo = value; 
            }
        }

        /// <summary>
        /// Gets stack DNS client.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public Dns_Client Dns
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pDnsClient; 
            }
        }

        /// <summary>
        /// Gets SIP logger.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public Logger Logger
        {
            get{ 
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pLogger; 
            }
        }

        /// <summary>
        /// Gets current registrations.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_UA_Registration[] Registrations
        {
            get{
                if(m_State == SIP_StackState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRegistrations.ToArray();
            }
        }

        #endregion

        #region Events Implementation
                
        /// <summary>
        /// This event is raised when new incoming SIP request is received. You can control incoming requests
        /// with that event. For example you can require authentication or send what ever error to request maker user.
        /// </summary>
        public event EventHandler<SIP_ValidateRequestEventArgs> ValidateRequest = null;
        
        #region mehtod OnValidateRequest

        /// <summary>
        /// Is called by Transport layer when new incoming SIP request is received.
        /// </summary>
        /// <param name="request">Incoming SIP request.</param>
        /// <param name="remoteEndPoint">Request maker IP end point.</param>
        /// <returns></returns>
        internal SIP_ValidateRequestEventArgs OnValidateRequest(SIP_Request request,IPEndPoint remoteEndPoint)
        {
            SIP_ValidateRequestEventArgs eArgs = new SIP_ValidateRequestEventArgs(request,remoteEndPoint);
            if(this.ValidateRequest != null){
                this.ValidateRequest(this,eArgs);
            }

            return eArgs;
        }

        #endregion


        /// <summary>
        /// This event is raised when new SIP request is received.
        /// </summary>
        public event EventHandler<SIP_RequestReceivedEventArgs> RequestReceived = null;

        #region method OnRequestReceived

        /// <summary>
        /// Raises <b>RequestReceived</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        internal void OnRequestReceived(SIP_RequestReceivedEventArgs e)
        {
            if(this.RequestReceived != null){
                this.RequestReceived(this,e);
            }
        }

        #endregion


        /// <summary>
        /// This event is raised when new stray SIP response is received.
        /// Stray response means that response doesn't match to any transaction.
        /// </summary>
        public event EventHandler<SIP_ResponseReceivedEventArgs> ResponseReceived = null;

        #region method OnResponseReceived

        /// <summary>
        /// Raises <b>ResponseReceived</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        internal void OnResponseReceived(SIP_ResponseReceivedEventArgs e)
        {
            if(this.ResponseReceived != null){
                this.ResponseReceived(this,e);
            }
        }

        #endregion


        /// <summary>
        /// This event is raised by any SIP element when unknown/unhandled error happened.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> Error = null;

        #region method OnError

        /// <summary>
        /// Is called when ever unknown error happens.
        /// </summary>
        /// <param name="x">Exception happened.</param>
        internal void OnError(Exception x)
        {
            if(this.Error != null){
                this.Error(this,new ExceptionEventArgs(x));
            }
        }

        #endregion

        #endregion

    }
}
