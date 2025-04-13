using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class holds known RTCP packet types.
    /// </summary>
    public class RTCP_PacketType
    {
        /// <summary>
        /// Sender report.
        /// </summary>
        public const int SR = 200;

        /// <summary>
        /// Receiver report.
        /// </summary>
        public const int RR = 201;

        /// <summary>
        /// Session description.
        /// </summary>
        public const int SDES = 202;

        /// <summary>
        /// BYE.
        /// </summary>
        public const int BYE = 203;

        /// <summary>
        /// Application specifiec data.
        /// </summary>
        public const int APP = 204;
    }
}
