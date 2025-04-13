using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP message. This is base class for SIP_Request and SIP_Response. Defined in RFC 3261.
    /// </summary>
    public abstract class SIP_Message
    {
        private SIP_HeaderFieldCollection m_pHeader = null;
        private byte[]                    m_Data    = null;

        /// <summary>
        /// Default constuctor.
        /// </summary>
        public SIP_Message()
        {
            m_pHeader = new SIP_HeaderFieldCollection();
        }


        #region method InternalParse

        /// <summary>
        /// Parses SIP message from specified byte array.
        /// </summary>
        /// <param name="data">SIP message data.</param>
        protected void InternalParse(byte[] data)
        {
            InternalParse(new MemoryStream(data));
        }

        /// <summary>
        /// Parses SIP message from specified stream.
        /// </summary>
        /// <param name="stream">SIP message stream.</param>
        protected void InternalParse(Stream stream)
        {
            /* SIP message syntax:
                header-line<CRFL>
                ....
                <CRFL>
                data size of Content-Length header field.
            */

            // Parse header
            this.Header.Parse(stream);

            // Parse data
            int contentLength = 0;
            try{
                contentLength = Convert.ToInt32(m_pHeader.GetFirst("Content-Length:").Value);
            }
            catch{
            }
            if(contentLength > 0){
                byte[] data = new byte[contentLength];
                stream.Read(data,0,data.Length);
                this.Data = data;
            }
        }

        #endregion

        #region mehtod InternalToStream

        /// <summary>
        /// Stores SIP_Message to specified stream.
        /// </summary>
        /// <param name="stream">Stream where to store SIP_Message.</param>
        protected void InternalToStream(Stream stream)
        {
            // Ensure that we add right Contnet-Length.
            m_pHeader.RemoveAll("Content-Length:");
            if(m_Data != null){
                m_pHeader.Add("Content-Length:",Convert.ToString(m_Data.Length));
            }
            else{
                m_pHeader.Add("Content-Length:",Convert.ToString(0));
            }

            // Store header
            byte[] header = Encoding.UTF8.GetBytes(m_pHeader.ToHeaderString());
            stream.Write(header,0,header.Length);

            // Store data
            if(m_Data != null && m_Data.Length > 0){
                stream.Write(m_Data,0,m_Data.Length);
            }
        }

        #endregion

        
        #region Properties Implementation

        /// <summary>
        /// Gets direct access to header.
        /// </summary>
        public SIP_HeaderFieldCollection Header
        {
            get{ return m_pHeader; }
        }
        
        /// <summary>
        /// Gets or sets what features end point supports.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_AcceptRange> Accept
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_AcceptRange>(this,"Accept:"); }
        }

        /// <summary>
        /// Gets or sets Accept-Contact header value. Defined in RFC 3841.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_ACValue> AcceptContact
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_ACValue>(this,"Accept-Contact:"); }
        }

        /// <summary>
        /// Gets encodings what end point supports. Example: Accept-Encoding: gzip.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_Encoding> AcceptEncoding
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_Encoding>(this,"Accept-Encoding:"); }
        }

        /// <summary>
        /// Gets preferred languages for reason phrases, session descriptions, or
        /// status responses carried as message bodies in the response. If no Accept-Language 
        /// header field is present, the server SHOULD assume all languages are acceptable to the client.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_Language> AcceptLanguage
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_Language>(this,"Accept-Language:"); }
        }

        /// <summary>
        /// Gets Accept-Resource-Priority headers. Defined in RFC 4412.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_RValue> AcceptResourcePriority
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_RValue>(this,"Accept-Resource-Priority:"); }
        }

        /// <summary>
        /// Gets AlertInfo values collection. When present in an INVITE request, the Alert-Info header 
        /// field specifies an alternative ring tone to the UAS. When present in a 180 (Ringing) response, 
        /// the Alert-Info header field specifies an alternative ringback tone to the UAC.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_AlertParam> AlertInfo
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_AlertParam>(this,"Alert-Info:"); }
        }

        /// <summary>
        /// Gets methods collection which is supported by the UA which generated the message.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_Method> Allow
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_Method>(this,"Allow:"); }
        }

        /// <summary>
        /// Gets Allow-Events header which indicates the event packages supported by the client. Defined in rfc 3265.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_EventType> AllowEvents
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_EventType>(this,"Allow-Events:"); }
        }

        /// <summary>
        /// Gets the Authentication-Info header fields which provides for mutual authentication 
        /// with HTTP Digest.
        /// </summary>
        public SIP_SVGroupHFCollection<SIP_t_AuthenticationInfo> AuthenticationInfo
        {   
            get{ return new SIP_SVGroupHFCollection<SIP_t_AuthenticationInfo>(this,"Authentication-Info:"); }
        }

        /// <summary>
        /// Gets the Authorization header fields which contains authentication credentials of a UA.
        /// </summary>
        public SIP_SVGroupHFCollection<SIP_t_Credentials> Authorization
        {
            get{ return new SIP_SVGroupHFCollection<SIP_t_Credentials>(this,"Authorization:"); }
        }

        /// <summary>
        /// Gets or sets the Call-ID header field which uniquely identifies a particular invitation or all 
        /// registrations of a particular client.
        /// Value null means not specified.
        /// </summary>
        public string CallID
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Call-ID:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Call-ID:");
                }
                else{
                    m_pHeader.Set("Call-ID:",value);
                }
            }
        }

        /// <summary>
        /// Gets the Call-Info header field which provides additional information about the
        /// caller or callee, depending on whether it is found in a request or response.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_Info> CallInfo
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_Info>(this,"Call-Info:"); }
        }

        /// <summary>
        /// Gets contact header fields. The Contact header field provides a SIP or SIPS URI that can be used
        /// to contact that specific instance of the UA for subsequent requests.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_ContactParam> Contact
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_ContactParam>(this,"Contact:"); }
        }

        /// <summary>
        /// Gets or sets the Content-Disposition header field which describes how the message body
        /// or, for multipart messages, a message body part is to be interpreted by the UAC or UAS.
        /// Value null means not specified.
        /// </summary>
        public SIP_t_ContentDisposition ContentDisposition
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Content-Disposition:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_ContentDisposition>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Content-Disposition:");
                }
                else{
                    m_pHeader.Set("Content-Disposition:",value.ToStringValue());
                }
            }
        }

        /// <summary>
        /// Gets the Content-Encodings which is used as a modifier to the "media-type". When present, 
        /// its value indicates what additional content codings have been applied to the entity-body, 
        /// and thus what decoding mechanisms MUST be applied in order to obtain the media-type referenced 
        /// by the Content-Type header field.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_ContentCoding> ContentEncoding
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_ContentCoding>(this,"Content-Encoding:"); }
        }

        /// <summary>
        /// Gets content languages.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_LanguageTag> ContentLanguage
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_LanguageTag>(this,"Content-Language:"); }
        }

        /// <summary>
        /// Gets SIP request content data size in bytes.
        /// </summary>
        public int ContentLength
        {
            get{
                if(m_Data == null){
                    return 0;
                }
                else{
                    return m_Data.Length;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Content-Type header field which indicates the media type of the
        /// message-body sent to the recipient.
        /// Value null means not specified.
        /// </summary>
        public string ContentType
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Content-Type:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Content-Type:");
                }
                else{
                    m_pHeader.Set("Content-Type:",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets command sequence number and the request method.
        /// Value null means not specified.
        /// </summary>
        public SIP_t_CSeq CSeq
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("CSeq:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_CSeq>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("CSeq:");
                }
                else{
                    m_pHeader.Set("CSeq:",value.ToStringValue());
                }
            }
        }

        /// <summary>
        /// Gets or sets date and time. Value DateTime.MinValue means that value not specified.
        /// </summary>
        public DateTime Date
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Date:");
                if(h != null){
                    return DateTime.ParseExact(h.Value,"r",System.Globalization.DateTimeFormatInfo.InvariantInfo);
                }
                else{
                    return DateTime.MinValue; 
                }
            }

            set{
                if(value == DateTime.MinValue){
                    m_pHeader.RemoveFirst("Date:");
                }
                else{
                    m_pHeader.Set("Date:",value.ToString("r"));
                }
            }
        }

        /// <summary>
        /// Gets the Error-Info header field which provides a pointer to additional
        /// information about the error status response.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_ErrorUri> ErrorInfo
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_ErrorUri>(this,"Error-Info:"); }
        }

        /// <summary>
        /// Gets or sets Event header. Defined in RFC 3265.
        /// </summary>
        public SIP_t_Event Event
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Event:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_Event>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Event:");
                }
                else{
                    m_pHeader.Set("Event:",value.ToStringValue());
                }
            }
        }

        /// <summary>
        /// Gets or sets relative time after which the message (or content) expires.
        /// Value -1 means that value not specified.
        /// </summary>
        public int Expires
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Expires:");
                if(h != null){
                    return Convert.ToInt32(h.Value);
                }
                else{
                    return -1; 
                } 
            }

            set{
                if(value < 0){
                    m_pHeader.RemoveFirst("Expires:");
                }
                else{
                    m_pHeader.Set("Expires:",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets initiator of the request.
        /// Value null means not specified.
        /// </summary>
        public SIP_t_From From
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("From:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_From>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("From:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_From>("From:",value));
                }
            }
        }

        /// <summary>
        /// Gets History-Info headers. Defined in RFC 4244.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_HiEntry> HistoryInfo
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_HiEntry>(this,"History-Info:"); }
        }

        /// <summary>
        /// Identity header value. Value null means not specified. Defined in RFC 4474.
        /// </summary>
        public string Identity
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Identity:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Identity:");
                }
                else{
                    m_pHeader.Set("Identity:",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets Identity-Info header value. Value null means not specified. 
        /// Defined in RFC 4474.
        /// </summary>
        public SIP_t_IdentityInfo IdentityInfo
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Identity-Info:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_IdentityInfo>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Identity-Info:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_IdentityInfo>("Identity-Info:",value));
                }
            }
        }


        /// <summary>
        /// Gets the In-Reply-To header fields which enumerates the Call-IDs that this call 
        /// references or returns.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_CallID> InReplyTo
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_CallID>(this,"In-Reply-To:"); }
        }

        /// <summary>
        /// Gets or sets Join header which indicates that a new dialog (created by the INVITE in which 
        /// the Join header field in contained) should be joined with a dialog identified by the header 
        /// field, and any associated dialogs or conferences. Defined in 3911. Value null means not specified.
        /// </summary>
        public SIP_t_Join Join
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Join:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_Join>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Join:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_Join>("Join:",value));
                }
            }
        }

        /// <summary>
        /// Gets or sets limit the number of proxies or gateways that can forward the request 
        /// to the next downstream server.
        /// Value -1 means that value not specified.
        /// </summary>
        public int MaxForwards
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Max-Forwards:");
                if(h != null){
                    return Convert.ToInt32(h.Value);
                }
                else{
                    return -1; 
                } 
            }

            set{
                if(value < 0){
                    m_pHeader.RemoveFirst("Max-Forwards:");
                }
                else{
                    m_pHeader.Set("Max-Forwards:",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets mime version. Currently 1.0 is only defined value.
        /// Value null means not specified.
        /// </summary>
        public string MimeVersion
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Mime-Version:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Mime-Version:");
                }
                else{
                    m_pHeader.Set("Mime-Version:",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets minimum refresh interval supported for soft-state elements managed by that server.
        /// Value -1 means that value not specified.
        /// </summary>
        public int MinExpires
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Min-Expires:");
                if(h != null){
                    return Convert.ToInt32(h.Value);
                }
                else{
                    return -1; 
                } 
            }

            set{
                if(value < 0){
                    m_pHeader.RemoveFirst("Min-Expires:");
                }
                else{
                    m_pHeader.Set("Min-Expires:",value.ToString());
                }
            }
        }
        
        /// <summary>
        /// Gets or sets Min-SE header which indicates the minimum value for the session interval, 
        /// in units of delta-seconds. Defined in 4028. Value null means not specified.
        /// </summary>
        public SIP_t_MinSE MinSE
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Min-SE:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_MinSE>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Min-SE:");
                }
                else{
                    m_pHeader.Set("Min-SE:",value.ToStringValue());
                }
            }
        }

        /// <summary>
        /// Gets or sets organization name which the SIP element issuing the request or response belongs.
        /// Value null means not specified.
        /// </summary>        
        public string Organization
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Organization:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Organization:");
                }
                else{
                    m_pHeader.Set("Organization:",value);
                }
            }
        }

        /// <summary>
        /// Gets an Path header. It is used in conjunction with SIP REGISTER requests and with 200 
        /// class messages in response to REGISTER (REGISTER responses). Defined in rfc 3327.
        /// </summary>
        public SIP_SVGroupHFCollection<SIP_t_AddressParam> Path
        {
            get{ return new SIP_SVGroupHFCollection<SIP_t_AddressParam>(this,"Path:"); }
        }

        /// <summary>
        /// Gest or sets priority that the SIP request should have to the receiving human or its agent.
        /// Value null means not specified.
        /// </summary>
        public string Priority
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Priority:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Priority:");
                }
                else{
                    m_pHeader.Set("Priority:",value);
                }
            }
        }

        // Privacy                       [RFC3323]

        /// <summary>
        /// Gets an proxy authentication challenge.
        /// </summary>
        public SIP_SVGroupHFCollection<SIP_t_Challenge> ProxyAuthenticate
        {
            get{ return new SIP_SVGroupHFCollection<SIP_t_Challenge>(this,"Proxy-Authenticate:"); }
        }

        /// <summary>
        /// Gest credentials containing the authentication information of the user agent 
        /// for the proxy and/or realm of the resource being requested.
        /// </summary>
        public SIP_SVGroupHFCollection<SIP_t_Credentials> ProxyAuthorization
        {
            get{ return new SIP_SVGroupHFCollection<SIP_t_Credentials>(this,"Proxy-Authorization:"); }
        }

        /// <summary>
        /// Gets proxy-sensitive features that must be supported by the proxy.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_OptionTag> ProxyRequire
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_OptionTag>(this,"Proxy-Require:"); }
        }

        /// <summary>
        /// Gets or sets RAck header. Defined in 3262. Value null means not specified.
        /// </summary>
        public SIP_t_RAck RAck
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("RAck:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_RAck>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("RAck:");
                }
                else{
                    m_pHeader.Set("RAck:",value.ToStringValue());
                }
            }
        }

        /// <summary>
        /// Gets the Reason header. Defined in rfc 3326.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_ReasonValue> Reason
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_ReasonValue>(this,"Reason:"); }
        }
        
        /// <summary>
        /// Gets the Record-Route header fields what is inserted by proxies in a request to
        /// force future requests in the dialog to be routed through the proxy.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_AddressParam> RecordRoute
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_AddressParam>(this,"Record-Route:"); }
        }

        /// <summary>
        /// Gets or sets Refer-Sub header. Defined in rfc 4488. Value null means not specified.
        /// </summary>
        public SIP_t_ReferSub ReferSub
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Refer-Sub:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_ReferSub>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Refer-Sub:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_ReferSub>("Refer-Sub:",value));
                }
            }
        }

        /// <summary>
        /// Gets or sets Refer-To header. Defined in rfc 3515. Value null means not specified.
        /// </summary>
        public SIP_t_AddressParam ReferTo
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Refer-To:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_AddressParam>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Refer-To:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_AddressParam>("Refer-To:",value));
                }
            }
        }

        /// <summary>
        /// Gets or sets Referred-By header. Defined in rfc 3892. Value null means not specified.
        /// </summary>
        public SIP_t_ReferredBy ReferredBy
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Referred-By:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_ReferredBy>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Referred-By:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_ReferredBy>("Referred-By:",value));
                }
            }
        }

        /// <summary>
        /// Gets Reject-Contact headers. Defined in RFC 3841.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_RCValue> RejectContact
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_RCValue>(this,"Reject-Contact:"); }
        }

        /// <summary>
        /// Gets or sets Replaces header. Defined in rfc 3891. Value null means not specified.
        /// </summary>
        public SIP_t_Replaces Replaces
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Replaces:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_Replaces>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Replaces:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_Replaces>("Replaces:",value));
                }
            }
        }
        
        /// <summary>
        /// Gets logical return URI that may be different from the From header field.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_AddressParam> ReplyTo
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_AddressParam>(this,"Reply-To:"); }
        }

        /// <summary>
        /// Gets or sets Request-Disposition header. The Request-Disposition header field specifies caller preferences for
        /// how a server should process a request. Defined in rfc 3841.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_Directive> RequestDisposition
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_Directive>(this,"Request-Disposition:"); }
        }

        /// <summary>
        /// Gets options that the UAC expects the UAS to support in order to process the request.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_OptionTag> Require
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_OptionTag>(this,"Require:"); }
        }

        /// <summary>
        /// Gets Resource-Priority headers. Defined in RFC 4412.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_RValue> ResourcePriority
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_RValue>(this,"Resource-Priority:"); }
        }

        /// <summary>
        /// Gets or sets how many seconds the service is expected to be unavailable to the requesting client.
        /// Value null means that value not specified.
        /// </summary>
        public SIP_t_RetryAfter RetryAfter
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Retry-After:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_RetryAfter>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Retry-After:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_RetryAfter>("Retry-After:",value));
                }
            }
        }

        /// <summary>
        /// Gets force routing for a request through the listed set of proxies.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_AddressParam> Route
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_AddressParam>(this,"Route:"); }
        }

        /// <summary>
        /// Gets or sets RSeq header. Value -1 means that value not specified. Defined in rfc 3262.
        /// </summary>
        public int RSeq
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("RSeq:");
                if(h != null){
                    return Convert.ToInt32(h.Value);
                }
                else{
                    return -1; 
                } 
            }

            set{
                if(value < 0){
                    m_pHeader.RemoveFirst("RSeq:");
                }
                else{
                    m_pHeader.Set("RSeq:",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets Security-Client headers. Defined in RFC 3329.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_SecMechanism> SecurityClient
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_SecMechanism>(this,"Security-Client:"); }
        }

        /// <summary>
        /// Gets Security-Server headers. Defined in RFC 3329.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_SecMechanism> SecurityServer
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_SecMechanism>(this,"Security-Server:"); }
        }

        /// <summary>
        /// Gets Security-Verify headers. Defined in RFC 3329.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_SecMechanism> SecurityVerify
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_SecMechanism>(this,"Security-Verify:"); }
        }

        /// <summary>
        /// Gets or sets the software used by the UAS to handle the request.
        /// Value null means not specified.
        /// </summary>
        public string Server
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Server:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Server:");
                }
                else{
                    m_pHeader.Set("Server:",value);
                }
            }
        }

        /// <summary>
        /// Gets the Service-Route header. Defined in rfc 3608.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_AddressParam> ServiceRoute
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_AddressParam>(this,"Service-Route:"); }
        }

        /// <summary>
        /// Gets or sets Session-Expires expires header. Value null means that value not specified. 
        /// Defined in rfc 4028.
        /// </summary>
        public SIP_t_SessionExpires SessionExpires
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Session-Expires:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_SessionExpires>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Session-Expires:");
                }
                else{
                    m_pHeader.Set("Session-Expires:",value.ToStringValue());
                }
            }
        }

        /// <summary>
        /// Gets or sets SIP-ETag header value. Value null means not specified. Defined in RFC 3903.
        /// </summary>
        public string SIPETag
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("SIP-ETag:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("SIP-ETag:");
                }
                else{
                    m_pHeader.Set("SIP-ETag:",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets SIP-ETag header value. Value null means not specified. Defined in RFC 3903.
        /// </summary>
        public string SIPIfMatch
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("SIP-If-Match:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("SIP-If-Match:");
                }
                else{
                    m_pHeader.Set("SIP-If-Match:",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets call subject text.
        /// Value null means not specified.
        /// </summary>
        public string Subject
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("Subject:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Subject:");
                }
                else{
                    m_pHeader.Set("Subject:",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets Subscription-State header value. Value null means that value not specified. 
        /// Defined in RFC 3265. 
        /// </summary>
        public SIP_t_SubscriptionState SubscriptionState
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Subscription-State:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_SubscriptionState>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Subscription-State:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_SubscriptionState>("Subscription-State:",value));
                }
            }
        }

        /// <summary>
        /// Gets extensions supported by the UAC or UAS. Known values are defined in SIP_OptionTags class.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_OptionTag> Supported
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_OptionTag>(this,"Supported:"); }
        }

        /// <summary>
        /// Gets or sets Target-Dialog header value. Value null means that value not specified. 
        /// Defined in RFC 4538.
        /// </summary>
        public SIP_t_TargetDialog TargetDialog
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Target-Dialog:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_TargetDialog>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Target-Dialog:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_TargetDialog>("Target-Dialog:",value));
                }
            }
        }

        /// <summary>
        /// Gets or sets when the UAC sent the request to the UAS. 
        /// Value null means that value not specified.
        /// </summary>
        public SIP_t_Timestamp Timestamp
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("Timestamp:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_Timestamp>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("Timestamp:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_Timestamp>("Timestamp:",value));
                }
            }
        }

        /// <summary>
        /// Gets or sets logical recipient of the request.
        /// Value null means not specified.
        /// </summary>
        public SIP_t_To To
        {
            get{
                SIP_HeaderField h = m_pHeader.GetFirst("To:");
                if(h != null){
                    return ((SIP_SingleValueHF<SIP_t_To>)h).ValueX;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("To:");
                }
                else{
                    m_pHeader.Add(new SIP_SingleValueHF<SIP_t_To>("To:",value));
                }
            }
        }

        /// <summary>
        /// Gets features not supported by the UAS.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_OptionTag> Unsupported
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_OptionTag>(this,"Unsupported:"); }
        }

        /// <summary>
        /// Gets or sets information about the UAC originating the request.
        /// Value null means not specified.
        /// </summary>
        public string UserAgent
        {
            get{ 
                SIP_HeaderField h = m_pHeader.GetFirst("User-Agent:");
                if(h != null){
                    return h.Value;
                }
                else{
                    return null; 
                }
            }

            set{
                if(value == null){
                    m_pHeader.RemoveFirst("User-Agent:");
                }
                else{
                    m_pHeader.Set("User-Agent:",value);
                }
            }
        }

        /// <summary>
        /// Gets Via header fields.The Via header field indicates the transport used for the transaction
        /// and identifies the location where the response is to be sent.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_ViaParm> Via
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_ViaParm>(this,"Via:"); }
        }

        /// <summary>
        /// Gets additional information about the status of a response.
        /// </summary>
        public SIP_MVGroupHFCollection<SIP_t_WarningValue> Warning
        {
            get{ return new SIP_MVGroupHFCollection<SIP_t_WarningValue>(this,"Warning:"); }
        }

        /// <summary>
        /// Gets or authentication challenge.
        /// </summary>
        public SIP_SVGroupHFCollection<SIP_t_Challenge> WWWAuthenticate
        {
            get{ return new SIP_SVGroupHFCollection<SIP_t_Challenge>(this,"WWW-Authenticate:"); }
        }

        /// <summary>
        /// Gets or sets content data.
        /// </summary>
        public byte[] Data
        {
            get{ return m_Data; }

            set{ m_Data = value; }
        }

        #endregion

    }
}
