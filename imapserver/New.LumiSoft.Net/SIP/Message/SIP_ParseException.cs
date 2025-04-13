using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// The exception that is thrown when a SIP message parsing fails.
    /// </summary>
    public class SIP_ParseException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">The message what describes the error.</param>
        public SIP_ParseException(string message) : base(message)
        {
        }
    }
}
