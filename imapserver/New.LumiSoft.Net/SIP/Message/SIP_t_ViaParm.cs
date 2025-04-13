using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;

using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "via-parm" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
    ///     via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
    ///     via-ttl           =  "ttl" EQUAL ttl
    ///     via-maddr         =  "maddr" EQUAL host
    ///     via-received      =  "received" EQUAL (IPv4address / IPv6address)
    ///     via-branch        =  "branch" EQUAL token
    ///     via-extension     =  generic-param
    ///     sent-protocol     =  protocol-name SLASH protocol-version SLASH transport
    ///     protocol-name     =  "SIP" / token
    ///     protocol-version  =  token
    ///     transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
    ///     sent-by           =  host [ COLON port ]
    ///     ttl               =  1*3DIGIT ; 0 to 255
    ///         
    ///     Via extentions:
    ///       // RFC 3486
    ///       via-compression  =  "comp" EQUAL ("sigcomp" / other-compression)
    ///       // RFC 3581
    ///       response-port  =  "rport" [EQUAL 1*DIGIT]
    /// </code>
    /// </remarks>
    public class SIP_t_ViaParm : SIP_t_ValueWithParams
    {
        private string       m_ProtocolName      = "";
        private string       m_ProtocolVersion   = "";
        private string       m_ProtocolTransport = "";
        private HostEndPoint m_pSentBy           = null;

        /// <summary>
        /// Defualt constructor.
        /// </summary>
        public SIP_t_ViaParm()
        {
            m_ProtocolName      = "SIP";
            m_ProtocolVersion   = "2.0";
            m_ProtocolTransport = "UDP";
            m_pSentBy           = new HostEndPoint("localhost",-1);
        }


        #region static method CreateBranch

        /// <summary>
        /// Creates new branch paramter value.
        /// </summary>
        /// <returns></returns>
        public static string CreateBranch()
        {
            // The value of the branch parameter MUST start with the magic cookie "z9hG4bK".

            return "z9hG4bK-" + Guid.NewGuid().ToString().Replace("-","");
        }

        #endregion


        #region method Parse

        /// <summary>
        /// Parses "via-parm" from specified value.
        /// </summary>
        /// <param name="value">SIP "via-parm" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("reader");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "via-parm" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
                via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
                via-ttl           =  "ttl" EQUAL ttl
                via-maddr         =  "maddr" EQUAL host
                via-received      =  "received" EQUAL (IPv4address / IPv6address)
                via-branch        =  "branch" EQUAL token
                via-extension     =  generic-param
                sent-protocol     =  protocol-name SLASH protocol-version
                                     SLASH transport
                protocol-name     =  "SIP" / token
                protocol-version  =  token
                transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
                sent-by           =  host [ COLON port ]
                ttl               =  1*3DIGIT ; 0 to 255
             
                Via extentions:
                // RFC 3486
                via-compression  =  "comp" EQUAL ("sigcomp" / other-compression)
                // RFC 3581
                response-port  =  "rport" [EQUAL 1*DIGIT]
             
                Examples:
                Via: SIP/2.0/UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
                // Specifically, LWS on either side of the ":" or "/" is allowed.
                Via: SIP / 2.0 / UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // protocol-name
            string word = reader.QuotedReadToDelimiter('/');
            if(word == null){
                throw new SIP_ParseException("Via header field protocol-name is missing !");
            }
            this.ProtocolName = word.Trim();
            // protocol-version
            word = reader.QuotedReadToDelimiter('/');
            if(word == null){
                throw new SIP_ParseException("Via header field protocol-version is missing !");
            }        
            this.ProtocolVersion = word.Trim();
            // transport
            word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Via header field transport is missing !");
            }
            this.ProtocolTransport = word.Trim();
            // sent-by
            word = reader.QuotedReadToDelimiter(new char[]{';',','},false);
            if(word == null){
                throw new SIP_ParseException("Via header field sent-by is missing !");
            }
            this.SentBy = HostEndPoint.Parse(word.Trim());

            // Parse parameters
            this.ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "via-parm" value.
        /// </summary>
        /// <returns>Returns "via-parm" value.</returns>
        public override string ToStringValue()
        {
            /*
                Via               =  ( "Via" / "v" ) HCOLON via-parm *(COMMA via-parm)
                via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
                via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
                via-ttl           =  "ttl" EQUAL ttl
                via-maddr         =  "maddr" EQUAL host
                via-received      =  "received" EQUAL (IPv4address / IPv6address)
                via-branch        =  "branch" EQUAL token
                via-extension     =  generic-param
                sent-protocol     =  protocol-name SLASH protocol-version
                                     SLASH transport
                protocol-name     =  "SIP" / token
                protocol-version  =  token
                transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
                sent-by           =  host [ COLON port ]
                ttl               =  1*3DIGIT ; 0 to 255
             
                Via extentions:
                // RFC 3486
                via-compression  =  "comp" EQUAL ("sigcomp" / other-compression)
                // RFC 3581
                response-port  =  "rport" [EQUAL 1*DIGIT]
             
                Examples:
                Via: SIP/2.0/UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
                // Specifically, LWS on either side of the ":" or "/" is allowed.
                Via: SIP / 2.0 / UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
            */

            StringBuilder retVal = new StringBuilder();
            retVal.Append(this.ProtocolName + "/" + this.ProtocolVersion + "/" + this.ProtocolTransport + " ");
            retVal.Append(this.SentBy.ToString());
            retVal.Append(this.ParametersToString());

            return retVal.ToString();
        }

        #endregion

        
        #region Properties Implementation

        /// <summary>
        /// Gets sent protocol name. Normally this is always SIP.
        /// </summary>
        public string ProtocolName
        {
            get{ return m_ProtocolName; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property ProtocolName can't be null or empty !");
                }

                m_ProtocolName = value;
            }
        }

        /// <summary>
        /// Gets sent protocol version. 
        /// </summary>
        public string ProtocolVersion
        {
            get{ return m_ProtocolVersion; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property ProtocolVersion can't be null or empty !");
                }

                m_ProtocolVersion = value;
            }
        }

        /// <summary>
        /// Gets sent protocol transport. Currently known values are: UDP,TCP,TLS,SCTP. This value is always in upper-case.
        /// </summary>
        public string ProtocolTransport
        {
            get{ return m_ProtocolTransport.ToUpper(); }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property ProtocolTransport can't be null or empty !");
                }

                m_ProtocolTransport = value;
            }
        }

        /// <summary>
        /// Gets host name or IP with optional port. Examples: lumiosft.ee,10.0.0.1:80.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        public HostEndPoint SentBy
        {
            get{ return m_pSentBy; }

            set{
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_pSentBy = value;
            }
        }

        /// <summary>
        /// Gets sent-by port, if no port explicity set, transport default is returned.
        /// </summary>
        public int SentByPortWithDefault
        {
            get{
                if(m_pSentBy.Port != -1){
                    return m_pSentBy.Port;
                }
                else{
                    if(this.ProtocolTransport == SIP_Transport.TLS){
                        return 5061;
                    }
                    else{
                        return 5060;
                    }
                }
            }
        }
               
        /// <summary>
        /// Gets or sets 'branch' parameter value. The branch parameter in the Via header field values 
        /// serves as a transaction identifier. The value of the branch parameter MUST start
        /// with the magic cookie "z9hG4bK". Value null means that branch paramter doesn't exist.
        /// </summary>
        public string Branch
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["branch"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("branch");
                }
                else{
                    if(!value.StartsWith("z9hG4bK")){
                        throw new ArgumentException("Property Branch value must start with magic cookie 'z9hG4bK' !");
                    }

                    this.Parameters.Set("branch",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'comp' parameter value. Value null means not specified. Defined in RFC 3486.
        /// </summary>
        public string Comp
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["comp"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("comp");
                }
                else{
                    this.Parameters.Set("comp",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'maddr' parameter value. Value null means not specified.
        /// </summary>
        public string Maddr
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["maddr"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("maddr");
                }
                else{
                    this.Parameters.Set("maddr",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'received' parameter value. Value null means not specified.
        /// </summary>
        public IPAddress Received
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["received"];
                if(parameter != null){
                    return IPAddress.Parse(parameter.Value);
                }
                else{
                    return null;
                }
            }

            set{
                if(value == null){
                    this.Parameters.Remove("received");
                }
                else{
                    this.Parameters.Set("received",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets 'rport' parameter value. Value -1 means not specified and value 0 means empty "" rport.
        /// </summary>
        public int RPort
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["rport"];
                if(parameter != null){
                    if(parameter.Value == ""){
                        return 0;
                    }
                    else{
                        return Convert.ToInt32(parameter.Value);
                    }
                }
                else{
                    return -1;
                }
            }

            set{
                if(value < 0){
                    this.Parameters.Remove("rport");
                }
                else if(value == 0){
                    this.Parameters.Set("rport","");
                }
                else{
                    this.Parameters.Set("rport",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets 'ttl' parameter value. Value -1 means not specified.
        /// </summary>
        public int Ttl
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["ttl"];
                if(parameter != null){
                    return Convert.ToInt32(parameter.Value);                    
                }
                else{
                    return -1;
                }
            }

            set{
                if(value < 0){
                    this.Parameters.Remove("ttl");
                }
                else{
                    this.Parameters.Set("ttl",value.ToString());
                }
            }
        }

        #endregion

    }
}
