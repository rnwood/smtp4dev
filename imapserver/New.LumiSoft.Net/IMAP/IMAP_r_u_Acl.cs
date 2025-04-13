using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP ACL response. Defined in RFC 4314 3.6.2.
    /// </summary>
    public class IMAP_r_u_Acl : IMAP_r_u
    {
        private string           m_FolderName = "";
        private IMAP_Acl_Entry[] m_pEntries   = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folderName">Folder name with path.</param>
        /// <param name="entries">ACL entries.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folderName</b> or <b>entries</b> is null reference.</exception>
        public IMAP_r_u_Acl(string folderName,IMAP_Acl_Entry[] entries)
        {
            if(folderName == null){
                throw new ArgumentNullException("folderName");
            }
            if(folderName == string.Empty){
                throw new ArgumentException("Argument 'folderName' value must be specified.","folderName");
            }
            if(entries == null){
                throw new ArgumentNullException("entries");
            }

            m_FolderName = folderName;
            m_pEntries   = entries;
        }


        #region static method Parse

        /// <summary>
        /// Parses ACL response from acl-response string.
        /// </summary>
        /// <param name="aclResponse">ACL response.</param>
        /// <returns>Returns parsed ACL response.</returns>
        /// <exception cref="ArgumentNullException">Is raised wehn <b>aclResponse</b> is null reference.</exception>
        public static IMAP_r_u_Acl Parse(string aclResponse)
        {
            if(aclResponse == null){
                throw new ArgumentNullException("aclResponse");
            }

            /* RFC 4314 3.6. ACL Response.
                Data:       mailbox name
                            zero or more identifier rights pairs

                The ACL response occurs as a result of a GETACL command.  The first
                string is the mailbox name for which this ACL applies.  This is
                followed by zero or more pairs of strings; each pair contains the
                identifier for which the entry applies followed by the set of rights
                that the identifier has.
             
                Example:    C: A002 GETACL INBOX
                            S: * ACL INBOX Fred rwipsldexta
                            S: A002 OK Getacl complete
            */

            StringReader r = new StringReader(aclResponse);
            // Eat "*"
            r.ReadWord();
            // Eat "ACL"
            r.ReadWord();

            string               folderName = TextUtils.UnQuoteString(IMAP_Utils.Decode_IMAP_UTF7_String(r.ReadWord()));
            string[]             items      = r.ReadToEnd().Split(' ');
            List<IMAP_Acl_Entry> entries    = new List<IMAP_Acl_Entry>();
            for(int i=0;i<items.Length;i+=2){
                entries.Add(new IMAP_Acl_Entry(items[i],items[i + 1]));
            }

            return new IMAP_r_u_Acl(folderName,entries.ToArray());
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
            // Example:    S: * ACL INBOX Fred rwipslda test rwipslda

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* ACL ");
            retVal.Append(IMAP_Utils.EncodeMailbox(m_FolderName,encoding));
            foreach(IMAP_Acl_Entry e in m_pEntries){
                retVal.Append(" \"" + e.Identifier + "\" \"" + e.Rights + "\"");
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
        /// Gets ACL entries.
        /// </summary>
        public IMAP_Acl_Entry[] Entires
        {
            get{ return m_pEntries; }
        }

        #endregion
    }
}
