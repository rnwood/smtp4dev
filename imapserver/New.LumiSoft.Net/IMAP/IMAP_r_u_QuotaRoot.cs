using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP QUOTAROOT response. Defined in RFC 2087 5.2.
    /// </summary>
    public class IMAP_r_u_QuotaRoot : IMAP_r_u
    {
        private string   m_FolderName = "";
        private string[] m_QuotaRoots = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="quotaRoots">Quota roots.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>quotaRoots</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_QuotaRoot(string folder,string[] quotaRoots)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' name must be specified.","folder");
            }
            if(quotaRoots == null){
                throw new ArgumentNullException("quotaRoots");
            }

            m_FolderName = folder;
            m_QuotaRoots = quotaRoots;
        }


        #region static method Parse

        /// <summary>
        /// Parses QUOTAROOT response from quotaRoot-response string.
        /// </summary>
        /// <param name="response">QUOTAROOT response string.</param>
        /// <returns>Returns parsed QUOTAROOT response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_QuotaRoot Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 2087 5.2. QUOTAROOT Response.
                Data:       mailbox name
                            zero or more quota root names

                This response occurs as a result of a GETQUOTAROOT command.  The
                first string is the mailbox and the remaining strings are the
                names of the quota roots for the mailbox.

                Example:    S: * QUOTAROOT INBOX ""
                            S: * QUOTAROOT comp.mail.mime
            */

            StringReader r = new StringReader(response);
            // Eat "*"
            r.ReadWord();
            // Eat "QUOTAROOT"
            r.ReadWord();

            string folderName = TextUtils.UnQuoteString(IMAP_Utils.Decode_IMAP_UTF7_String(r.ReadWord()));
            List<string> quotaRoots = new List<string>();
            while(r.Available > 0){
                string quotaRoot = r.ReadWord();
                if(quotaRoot != null){
                    quotaRoots.Add(quotaRoot);
                }
                else{
                    break;
                }
            }

            return new IMAP_r_u_QuotaRoot(folderName,quotaRoots.ToArray());
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            return ToString(IMAP_Mailbox_Encoding.None);
        }

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <param name="encoding">Specifies how mailbox name is encoded.</param>
        /// <returns>Returns this as string.</returns>
        public override string ToString(IMAP_Mailbox_Encoding encoding)
        {
            // Example:    S: * QUOTAROOT INBOX ""

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* QUOTAROOT " + IMAP_Utils.EncodeMailbox(m_FolderName,encoding));
            foreach(string root in m_QuotaRoots){
                retVal.Append(" \"" + root + "\"");
            }
            retVal.Append("\r\n");

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
        /// Gets quota roots.
        /// </summary>
        public string[] QuotaRoots
        {
            get{ return m_QuotaRoots; }
        }

        #endregion
    }
}
