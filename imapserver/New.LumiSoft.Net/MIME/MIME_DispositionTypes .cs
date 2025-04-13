using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class holds MIME content disposition types. Defined in RFC 2183.
    /// </summary>
    public class MIME_DispositionTypes
    {
        /// <summary>
        /// A bodypart should be marked `inline' if it is intended to be displayed automatically upon display of the message. 
        /// Inline bodyparts should be presented in the order in which they occur, subject to the normal semantics of multipart messages.
        /// </summary>
        public static readonly string Inline = "inline";

        /// <summary>
        /// Bodyparts can be designated `attachment' to indicate that they are separate from the main body of the mail message, 
        /// and that their display should not be automatic, but contingent upon some further action of the user.
        /// </summary>
        public static readonly string Attachment = "attachment";
    }
}
