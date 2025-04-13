using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class holds SIP transports. Defined in RFC 3261.
    /// </summary>
    public class SIP_Transport
    {
        /// <summary>
        /// UDP protocol.
        /// </summary>
        public const string UDP = "UDP";

        /// <summary>
        /// TCP protocol.
        /// </summary>
        public const string TCP = "TCP";

        /// <summary>
        /// TCP + SSL protocol.
        /// </summary>
        public const string TLS = "TLS";
    }
}
