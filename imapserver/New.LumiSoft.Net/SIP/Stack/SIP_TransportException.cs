using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// The exception that is thrown when a transport error occurs.
    /// </summary>
    public class SIP_TransportException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="errorText">Error text describing error.</param>
        public SIP_TransportException(string errorText) : base(errorText)
        {
        }
    }
}
