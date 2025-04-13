using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.DNS.Client
{
    /// <summary>
    /// This class represents DNS client state.
    /// </summary>
    public enum DNS_ClientTransactionState
    {
        /// <summary>
        /// Transaction waits for start.
        /// </summary>
        WaitingForStart,

        /// <summary>
        /// Transaction is progress.
        /// </summary>
        Active,

        /// <summary>
        /// Transaction is completed(has got response from DNS server).
        /// </summary>
        Completed,

        /// <summary>
        /// Transaction is disposed.
        /// </summary>
        Disposed
    }
}
