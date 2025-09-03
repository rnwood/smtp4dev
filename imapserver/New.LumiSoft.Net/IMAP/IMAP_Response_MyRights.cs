using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP MYRIGHTS response. Defined in RFC 4314 3.8.
    /// </summary>
    [Obsolete("Use class IMAP_r_u_MyRights instead.")]
    public class IMAP_Response_MyRights : IMAP_r_u
    {
        private string m_FolderName = "";
        private string m_pRights    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="rights">Rights values.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_Response_MyRights(string folder,string rights)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            m_FolderName = folder;
            
            m_pRights = rights;
        }


        #region static method Parse

        /// <summary>
        /// Parses MYRIGHTS response from MYRIGHTS-response string.
        /// </summary>
        /// <param name="myRightsResponse">MYRIGHTS response line.</param>
        /// <returns>Returns parsed MYRIGHTS response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>myRightsResponse</b> is null reference.</exception>
        public static IMAP_Response_MyRights Parse(string myRightsResponse)
        {
            if(myRightsResponse == null){
                throw new ArgumentNullException("myRightsResponse");
            }

            /* RFC 4314 3.8. MYRIGHTS Response.
                Data:       mailbox name
                            rights

                The MYRIGHTS response occurs as a result of a MYRIGHTS command.  The
                first string is the mailbox name for which these rights apply.  The
                second string is the set of rights that the client has.

                Section 2.1.1 details additional server requirements related to
                handling of the virtual "d" and "c" rights.
             
                Example:    C: A003 MYRIGHTS INBOX
                            S: * MYRIGHTS INBOX rwiptsldaex
                            S: A003 OK Myrights complete
            */

            StringReader r = new StringReader(myRightsResponse);
            // Eat "*"
            r.ReadWord();
            // Eat "MYRIGHTS"
            r.ReadWord();

            string folder = IMAP_Utils.Decode_IMAP_UTF7_String(r.ReadWord(true));
            string rights = r.ReadToEnd().Trim();

            return new IMAP_Response_MyRights(folder,rights);
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * MYRIGHTS INBOX rwiptsldaex

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* MYRIGHTS \"" + m_FolderName + "\" \"" + m_pRights + "\"\r\n");

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets folder name.
        /// </summary>
        public string FolderName
        {
            get{ return m_FolderName; }
        }

        /// <summary>
        /// Gets rights list.
        /// </summary>
        public string Rights
        {
            get{ return m_pRights; }
        }

        #endregion
    }
}
