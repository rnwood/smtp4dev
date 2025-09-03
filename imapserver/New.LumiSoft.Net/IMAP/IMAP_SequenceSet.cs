using System;
using System.Collections.Generic;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// Obsolete IMAP sequence set. This is a stub implementation for compatibility.
    /// </summary>
    [Obsolete("This class is obsolete")]
    public class IMAP_SequenceSet
    {
        public static IMAP_SequenceSet Parse(string value)
        {
            return new IMAP_SequenceSet();
        }

        public string ToSequenceSetString()
        {
            return "";
        }
    }
}