using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.UA
{
    /// <summary>
    /// This enum specifies SIP UA call states.
    /// </summary>
    [Obsolete("Use SIP stack instead.")]
    public enum SIP_UA_CallState
    {
        /// <summary>
        /// Outgoing call waits to be started.
        /// </summary>
        WaitingForStart,

        /// <summary>
        /// Outgoing calling is in progress.
        /// </summary>
        Calling,

        /// <summary>
        /// Outgoing call remote end party is ringing.
        /// </summary>
        Ringing,

        /// <summary>
        /// Outgoing call remote end pary queued a call.
        /// </summary>
        Queued,

        /// <summary>
        /// Incoming call waits to be accepted.
        /// </summary>
        WaitingToAccept,

        /// <summary>
        /// Call is active.
        /// </summary>
        Active,
                
        /// <summary>
        /// Call is terminating.
        /// </summary>
        Terminating,

        /// <summary>
        /// Call is terminated.
        /// </summary>
        Terminated,

        /// <summary>
        /// Call has disposed.
        /// </summary>
        Disposed
    }
}
