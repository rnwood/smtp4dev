using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This enum specifies <b>RTP_SyncSource</b> state.
    /// </summary>
    public enum RTP_SourceState
    {
        /// <summary>
        /// Source is passive, sending only RTCP packets.
        /// </summary>
        Passive = 1,

        /// <summary>
        /// Source is active, sending RTP packets.
        /// </summary>
        Active = 2,

        /// <summary>
        /// Source has disposed.
        /// </summary>
        Disposed = 3,
    }
}
