using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class holds IMAP server FETCH return data type.
    /// </summary>
    public enum IMAP_Fetch_DataType
    {
        /// <summary>
        /// Message header.
        /// </summary>
        MessageHeader = 1,

        /// <summary>
        /// Full message.
        /// </summary>
        FullMessage = 2,

        /// <summary>
        /// Message structure(Full message, all body data truncated).
        /// </summary>
        MessageStructure = 3
    }
}
