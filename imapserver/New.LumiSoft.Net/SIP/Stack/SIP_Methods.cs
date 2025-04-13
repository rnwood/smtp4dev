using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class represents known SIP request methods.
    /// </summary>
    public class SIP_Methods
    {
        /// <summary>
        /// ACK method. Defined in RFC 3261.
        /// </summary>
        public const string ACK = "ACK";

        /// <summary>
        /// BYE method. Defined in RFC 3261.
        /// </summary>
        public const string BYE = "BYE";

        /// <summary>
        /// CANCEL method. Defined in RFC 3261.
        /// </summary>
        public const string CANCEL = "CANCEL";

        /// <summary>
        /// INFO method. Defined in RFC 2976.
        /// </summary>
        public const string INFO = "INFO";

        /// <summary>
        /// INVITE method. Defined in RFC 3261.
        /// </summary>
        public const string INVITE = "INVITE";

        /// <summary>
        /// MESSAGE method. Defined in RFC 3428.
        /// </summary>
        public const string MESSAGE = "MESSAGE";

        /// <summary>
        /// NOTIFY method. Defined in RFC 3265.
        /// </summary>
        public const string NOTIFY = "NOTIFY";
                
        /// <summary>
        /// OPTIONS method. Defined in RFC 3261.
        /// </summary>
        public const string OPTIONS = "OPTIONS";

        /// <summary>
        /// PRACK method. Defined in RFC 3262.
        /// </summary>
        public const string PRACK = "PRACK";

        /// <summary>
        /// PUBLISH method. Defined in RFC 3903.
        /// </summary>
        public const string PUBLISH = "PUBLISH";

        /// <summary>
        /// REFER method. Defined in RFC 3515.
        /// </summary>
        public const string REFER = "REFER";
                
        /// <summary>
        /// REGISTER method. Defined in RFC 3261.
        /// </summary>
        public const string REGISTER = "REGISTER";

        /// <summary>
        /// SUBSCRIBE method. Defined in RFC 3265.
        /// </summary>
        public const string SUBSCRIBE = "SUBSCRIBE";

        /// <summary>
        /// UPDATE method. Defined in RFC 3311.
        /// </summary>
        public const string UPDATE = "UPDATE";
    }
}
