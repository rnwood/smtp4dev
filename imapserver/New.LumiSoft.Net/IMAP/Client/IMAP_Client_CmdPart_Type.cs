using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// This class specified IMAP command part type.
    /// </summary>
    internal enum IMAP_Client_CmdPart_Type
    {
        /// <summary>
        /// Command part is constant value.
        /// </summary>
        Constant,

        /// <summary>
        /// Command part is IMAP string.
        /// </summary>
        String,
    }
}
