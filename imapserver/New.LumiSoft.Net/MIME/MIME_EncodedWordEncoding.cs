using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This enum specifies MIME RFC 2047 'encoded-word' encoding method.
    /// </summary>
    public enum MIME_EncodedWordEncoding
    {
        /// <summary>
        /// The "B" encoding. Defined in RFC 2047 (section 4.1).
        /// </summary>
        Q,

        /// <summary>
        /// The "Q" encoding. Defined in RFC 2047 (section 4.2).
        /// </summary>
        B
    }
}
