using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Specifies dialog state.
    /// </summary>
    public enum SIP_DialogState
    {
        /// <summary>
        /// Dialog isn't established yet.
        /// </summary>
        Early,

        /// <summary>
        /// Dialog has got 2xx response.
        /// </summary>
        Confirmed,

        /// <summary>
        /// Dialog is terminating.
        /// </summary>
        Terminating,

        /// <summary>
        /// Dialog is terminated.
        /// </summary>
        Terminated,

        /// <summary>
        /// Dialog is disposed.
        /// </summary>
        Disposed,
    }
}
