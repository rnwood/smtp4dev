using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This enum specifies RTP stream mode.
    /// </summary>
    public enum RTP_StreamMode
    {
        /// <summary>
        /// RTP data is sent only.
        /// </summary>
        Send = 0,

        /// <summary>
        /// RTP data is received only.
        /// </summary>
        Receive = 1,

        /// <summary>
        /// RTP data is sent and received.
        /// </summary>
        SendReceive = 2,

        /// <summary>
        /// No data is sent, only RTCP packets sent.
        /// </summary>
        Inactive = 3,
    }
}
