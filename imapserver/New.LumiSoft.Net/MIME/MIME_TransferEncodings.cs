using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class holds MIME content transfer encodings. Defined in RFC 2045 6.
    /// </summary>
    public class MIME_TransferEncodings
    {        
        /// <summary>
        /// Up to 998 octets per line of the code range 1..127 with CR and LF (codes 13 and 10 respectively) only allowed to 
        /// appear as part of a CRLF line ending. This is the default value.
        /// Defined in RFC 2045 6.2.
        /// </summary>
        public static readonly string SevenBit = "7bit";

        /// <summary>
        /// Up to 998 octets per line with CR and LF (codes 13 and 10 respectively) only allowed to appear as part of a CRLF line ending.
        /// Defined in RFC 2045 6.2.
        /// </summary>
        public static readonly string EightBit = "8bit";

        /// <summary>
        /// Used to encode arbitrary octet sequences into a form that satisfies the rules of 7bit. 
        /// Designed to be efficient and mostly human readable when used for text data consisting primarily of US-ASCII characters 
        /// but also containing a small proportion of bytes with values outside that range.
        /// Defined in RFC 2045 6.7.
        /// </summary>
        public static readonly string QuotedPrintable = "quoted-printable";

        /// <summary>
        /// Used to encode arbitrary octet sequences into a form that satisfies the rules of 7bit. Has a fixed overhead and is 
        /// intended for non text data and text that is not ASCII heavy.
        /// Defined in RFC 2045 6.8.
        /// </summary>
        public static readonly string Base64 = "base64";

        /// <summary>
        /// Any sequence of octets. This type is not widely used. Defined in RFC 3030.
        /// </summary>
        public static readonly string Binary = "binary";
    }
}
