using System;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// Obsolete IMAP message flags. This is a stub implementation for compatibility.
    /// </summary>
    [Obsolete("This class is obsolete")]
    public enum IMAP_MessageFlags
    {
        None = 0,
        Seen = 1,
        Answered = 2,
        Flagged = 4,
        Deleted = 8,
        Draft = 16
    }
}