using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class specifies IMAP mailbox name encoding.
    /// </summary>
    public enum IMAP_Mailbox_Encoding
    {
        /// <summary>
        /// Mailbox names are not encoded.
        /// </summary>
        None,

        /// <summary>
        /// Mailbox names are encoded with IMAP UTF-7 encoding. For more info see <see href="http://tools.ietf.org/html/rfc3501#section-5.1.3">rfc3501</see>.
        /// </summary>
        ImapUtf7,

        /// <summary>
        /// Mailbox names are encoded with IMAP UTF-8 encoding. For more info see <see href="http://tools.ietf.org/html/rfc5738#section-3">rfc5738</see>.
        /// </summary>
        ImapUtf8,
    }
}
