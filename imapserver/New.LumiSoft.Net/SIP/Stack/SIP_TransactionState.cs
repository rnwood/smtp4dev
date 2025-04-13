using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This enum holds SIP transaction states. Defined in RFC 3261.
    /// </summary>
    public enum SIP_TransactionState
    {
        /// <summary>
        /// Client transaction waits <b>Start</b> method to be called.
        /// </summary>
        WaitingToStart,

        /// <summary>
        /// Calling to recipient. This is used only by INVITE client transaction.
        /// </summary>
        Calling,

        /// <summary>
        /// This is transaction initial state. Used only in Non-INVITE transaction.
        /// </summary>
        Trying,

        /// <summary>
        /// This is INVITE server transaction initial state. Used only in INVITE server transaction.
        /// </summary>
        Proceeding,

        /// <summary>
        /// INVITE transaction has sent or received 2xx response.
        /// </summary>
        Accpeted,

        /// <summary>
        /// Transaction has got final response.
        /// </summary>
        Completed,

        /// <summary>
        /// Transation has got ACK from request maker. This is used only by INVITE server transaction.
        /// </summary>
        Confirmed,

        /// <summary>
        /// Transaction has terminated and waits disposing.
        /// </summary>
        Terminated,
        
        /// <summary>
        /// Transaction has disposed.
        /// </summary>
        Disposed,
    }
}
