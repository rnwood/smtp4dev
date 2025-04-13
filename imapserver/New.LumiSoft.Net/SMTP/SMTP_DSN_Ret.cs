using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP
{
    /// <summary>
    /// This value represents DSN RET value. Defined in RFC 3461 4.3.
    /// </summary>
    public enum SMTP_DSN_Ret
    {
        /// <summary>
        /// Value not specified, server will choose default type.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Requests that the entire message be returned in any "failed"
        /// Delivery Status Notification issued for this recipient.
        /// </summary>
        FullMessage = 1,

        /// <summary>
        /// Requests that only the headers of the message be returned.
        /// </summary>
        Headers = 3
    }
}
