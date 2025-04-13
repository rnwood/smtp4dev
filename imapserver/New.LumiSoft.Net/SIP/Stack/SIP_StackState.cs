using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class holds SIP stack state.
    /// </summary>
    public enum SIP_StackState
    {
        /// <summary>
        /// Stack has started and running.
        /// </summary>
        Started = 0,

        /// <summary>
        /// Stack is stopped.
        /// </summary>
        Stopped = 1,

        /// <summary>
        /// Stack is shutting down.
        /// </summary>
        Stopping = 2,

        /// <summary>
        /// Stack has disposed.
        /// </summary>
        Disposed = 3,
    }
}
