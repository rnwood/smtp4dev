using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.POP3
{
    /// <summary>
    /// This class holds known POP3 extended capabilities. Defined in http://www.iana.org/assignments/pop3-extension-mechanism.
    /// </summary>
    public class POP3_ExtendedCapabilities
    {
        /// <summary>
        /// The TOP capability indicates the optional TOP command is available. Defined in RFC 2449.
        /// </summary>
        public static readonly string TOP = "TOP";

        /// <summary>
        /// The USER capability indicates that the USER and PASS commands are supported. Defined in RFC 2449.
        /// </summary>
        public static readonly string USER = "USER";

        /// <summary>
        /// The SASL capability indicates that the AUTH command is available and that it supports an optional base64 
        /// encoded second argument for an initial client response as described in the SASL specification. Defined in RFC 2449.
        /// </summary>
        public static readonly string SASL = "SASL";

        /// <summary>
        /// The RESP-CODES capability indicates that any response text issued by this server which begins with an open 
        /// square bracket ("[") is an extended response code. Defined in RFC 2449.
        /// </summary>
        public static readonly string RESP_CODES = "RESP-CODES";

        /// <summary>
        /// LOGIN-DELAY capability. Defined in RFC 2449.
        /// </summary>
        public static readonly string LOGIN_DELAY = "LOGIN-DELAY";

        /// <summary>
        /// The PIPELINING capability indicates the server is capable of accepting multiple commands at a time; 
        /// the client does not have to wait for the response to a command before issuing a subsequent command.
        ///  Defined in RFC 2449.
        /// </summary>
        public static readonly string PIPELINING = "PIPELINING";

        /// <summary>
        /// EXPIRE capability. Defined in RFC 2449.
        /// </summary>
        public static readonly string EXPIRE = "EXPIRE";

        /// <summary>
        /// UIDL command is supported. Defined in RFC 2449.
        /// </summary>
        public static readonly string UIDL = "UIDL";

        /// <summary>
        /// STLS(start TLS) command supported.  Defined in RFC 2449.
        /// </summary>
        public static readonly string STLS = "STLS";
    }
}
