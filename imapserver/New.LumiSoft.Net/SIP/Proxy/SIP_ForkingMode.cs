using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// This enum specifies SIP proxy server 'forking' mode.
    /// </summary>
    public enum SIP_ForkingMode
    {
        /// <summary>
        /// No forking. The contact with highest q value is used.
        /// </summary>
        None,

        /// <summary>
        /// All contacts are processed parallel at same time.
        /// </summary>
        Parallel,

        /// <summary>
        /// In a sequential search, a proxy server attempts each contact address in sequence, 
        /// proceeding to the next one only after the previous has generated a final response. 
        /// Contacts are processed from highest q value to lower.
        /// </summary>
        Sequential,
    }
}
