using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// This class provides mail message related utility methods.
    /// </summary>
    public class Mail_Utils
    {
        #region static method SMTP_Mailbox

        /// <summary>
        /// Reads SMTP "Mailbox" from the specified MIME reader.
        /// </summary>
        /// <param name="reader">MIME reader.</param>
        /// <returns>Returns SMTP "Mailbox" or null if no SMTP mailbox available.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>reader</b> is null reference.</exception>
        internal static string SMTP_Mailbox(MIME_Reader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // TODO:

            /* RFC 5321.
                Mailbox        = Local-part "@" ( Domain / address-literal )
                Local-part     = Dot-string / Quoted-string ; MAY be case-sensitive
                Dot-string     = Atom *("."  Atom)
            */

            StringBuilder retVal = new StringBuilder();
            if(reader.Peek(true) == '\"'){
                retVal.Append("\"" + reader.QuotedString() + "\"");
            }
            else{
                retVal.Append(reader.DotAtom());
            }

            if(reader.Peek(true) != '@'){
                return null;
            }
            else{
                // Eat "@".
                reader.Char(true);

                retVal.Append('@');
                retVal.Append(reader.DotAtom());
            }
                        
            return retVal.ToString();
        }

        #endregion
    }
}
