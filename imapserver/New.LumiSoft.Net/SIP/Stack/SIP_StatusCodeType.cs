using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Specifies SIP status code type. Defined in rfc 3261.
    /// </summary>
    public enum SIP_StatusCodeType
    {
        /// <summary>
        /// Request received, continuing to process the request. 1xx status code.
        /// </summary>
        Provisional,

        /// <summary>
        /// Action was successfully received, understood, and accepted. 2xx status code.
        /// </summary>
        Success,

        /// <summary>
        /// Request must be redirected(forwarded). 3xx status code.
        /// </summary>
        Redirection,

        /// <summary>
        /// Request contains bad syntax or cannot be fulfilled at this server. 4xx status code.
        /// </summary>
        RequestFailure,

        /// <summary>
        /// Server failed to fulfill a valid request. 5xx status code.
        /// </summary>
        ServerFailure,

        /// <summary>
        /// Request cannot be fulfilled at any server. 6xx status code.
        /// </summary>
        GlobalFailure 
    }
}
