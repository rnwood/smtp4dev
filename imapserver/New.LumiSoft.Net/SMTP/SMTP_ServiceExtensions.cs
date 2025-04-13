using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP
{
    /// <summary>
    ///  This class holds known SMTP service extensions. Defined in http://www.iana.org/assignments/mail-parameters.
    /// </summary>
    public class SMTP_ServiceExtensions
    {
        /// <summary>
        /// Send as mail. Defined in RFC RFC 821.
        /// </summary>
        public static readonly string SEND = "SEND";
                   
        /// <summary>
        /// Send as mail or terminal. Defined in RFC 821.
        /// </summary>
        public static readonly string SOML = "SOML";

        /// <summary>
        /// Send as mail and terminal. Defined in RFC 821.
        /// </summary>
        public static readonly string SAML = "SAML";

        /// <summary>
        /// Expand the mailing list. Defined in RFC 821,
        /// </summary>
        public static readonly string EXPN = "EXPN";

        /// <summary>
        /// Supply helpful information. Defined in RFC 821.
        /// </summary>
        public static readonly string HELP = "HELP";

        /// <summary>
        /// Turn the operation around. Defined in RFC 821.
        /// </summary>
        public static readonly string TURN = "TURN";

        /// <summary>
        /// Use 8-bit data. Defined in RFC 1652.
        /// </summary>
        public static readonly string _8BITMIME = "8BITMIME";

        /// <summary>
        /// Message size declaration. Defined in RFC 1870.
        /// </summary>
        public static readonly string SIZE  = "SIZE";

        /// <summary>
        /// Chunking. Defined in RFC 3030.
        /// </summary>
        public static readonly string CHUNKING = "CHUNKING";

        /// <summary>
        /// Binary MIME. Defined in RFC 3030.
        /// </summary>
        public static readonly string BINARYMIME = "BINARYMIME";

        /// <summary>
        /// Checkpoint/Restart. Defined in RFC 1845.
        /// </summary>
        public static readonly string CHECKPOINT = "CHECKPOINT";

        /// <summary>
        /// Command Pipelining. Defined in RFC 2920.
        /// </summary>
        public static readonly string PIPELINING = "PIPELINING";

        /// <summary>
        /// Delivery Status Notification. Defined in RFC 1891.
        /// </summary>
        public static readonly string DSN = "DSN";

        /// <summary>
        /// Extended Turn. Defined in RFC 1985.
        /// </summary>
        public static readonly string ETRN = "ETRN";

        /// <summary>
        /// Enhanced Status Codes. Defined in RFC 2034.
        /// </summary>
        public static readonly string ENHANCEDSTATUSCODES = "ENHANCEDSTATUSCODES";

        /// <summary>
        /// Start TLS. Defined in RFC 3207.
        /// </summary>
        public static readonly string STARTTLS = "STARTTLS";

        /// <summary>
        /// Notification of no soliciting. Defined in RFC 3865.
        /// </summary>
        public static readonly string NO_SOLICITING = "NO-SOLICITING";

        /// <summary>
        /// Message Tracking. Defined in RFC 3885.
        /// </summary>
        public static readonly string MTRK = "MTRK";

        /// <summary>
        /// SMTP Responsible Submitter. Defined in RFC 4405.
        /// </summary>
        public static readonly string SUBMITTER = "SUBMITTER";

        /// <summary>
        /// Authenticated TURN. Defined in RFC 2645.
        /// </summary>
        public static readonly string ATRN = "ATRN";

        /// <summary>
        /// Authentication. Defined in RFC 4954.
        /// </summary>
        public static readonly string AUTH = "AUTH";

        /// <summary>
        /// Remote Content. Defined in RFC 4468.
        /// </summary>
        public static readonly string BURL = "BURL";

        /// <summary>
        /// Future Message Release. Defined in RFC 4865.
        /// </summary>
        public static readonly string FUTURERELEASE = "FUTURERELEASE";
    }
}
