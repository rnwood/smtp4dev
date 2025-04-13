using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP LIST response. Defined in RFC 3501 7.2.2.
    /// </summary>
    public class IMAP_r_u_List : IMAP_r_u
    {
        private string   m_FolderName        = "";
        private char     m_Delimiter         = '/';
        private string[] m_pFolderAttributes = new string[0];
                
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="delimiter">Hierarchy delimiter char.</param>
        /// <param name="attributes">Folder attributes.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_List(string folder,char delimiter,string[] attributes)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }

            m_FolderName = folder;
            m_Delimiter  = delimiter;
            if(attributes != null){
                m_pFolderAttributes = attributes;
            }
        }

        /// <summary>
        /// Default constructor. (Hierarchy delimiter request)
        /// </summary>
        /// <param name="delimiter">Hierarchy delimiter char.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal IMAP_r_u_List(char delimiter)
        {
            m_Delimiter = delimiter;
        }


        #region static method Parse

        /// <summary>
        /// Parses LIST response from list-response string.
        /// </summary>
        /// <param name="listResponse">List response string.</param>
        /// <returns>Returns parsed list response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>listResponse</b> is null reference.</exception>
        public static IMAP_r_u_List Parse(string listResponse)
        {
            if(listResponse == null){
                throw new ArgumentNullException("listResponse");
            }

            /* RFC 3501 7.2.2. LIST Response.
                Contents:   name attributes
                            hierarchy delimiter
                            name

                The LIST response occurs as a result of a LIST command.  It
                returns a single name that matches the LIST specification.  There
                can be multiple LIST responses for a single LIST command.

                Four name attributes are defined:

                \Noinferiors
                    It is not possible for any child levels of hierarchy to exist
                    under this name; no child levels exist now and none can be
                    created in the future.

                \Noselect
                    It is not possible to use this name as a selectable mailbox.

                \Marked
                    The mailbox has been marked "interesting" by the server; the
                    mailbox probably contains messages that have been added since
                    the last time the mailbox was selected.

                \Unmarked
                    The mailbox does not contain any additional messages since the
                    last time the mailbox was selected.

                If it is not feasible for the server to determine whether or not
                the mailbox is "interesting", or if the name is a \Noselect name,
                the server SHOULD NOT send either \Marked or \Unmarked.

                The hierarchy delimiter is a character used to delimit levels of
                hierarchy in a mailbox name.  A client can use it to create child
                mailboxes, and to search higher or lower levels of naming
                hierarchy.  All children of a top-level hierarchy node MUST use
                the same separator character.  A NIL hierarchy delimiter means
                that no hierarchy exists; the name is a "flat" name.

                The name represents an unambiguous left-to-right hierarchy, and
                MUST be valid for use as a reference in LIST and LSUB commands.
                Unless \Noselect is indicated, the name MUST also be valid as an
                argument for commands, such as SELECT, that accept mailbox names.

                Example:    S: * LIST (\Noselect) "/" ~/Mail/foo
            */

            StringReader r = new StringReader(listResponse);
            // Eat "*"
            r.ReadWord();
            // Eat "LIST"
            r.ReadWord();

            string attributes = r.ReadParenthesized();
            string delimiter  = r.ReadWord();
            string folder     = TextUtils.UnQuoteString(IMAP_Utils.DecodeMailbox(r.ReadToEnd().Trim()));

            return new IMAP_r_u_List(folder,delimiter[0],attributes == string.Empty ? new string[0] : attributes.Split(' '));
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
            /*
                Example:  S: * LIST (\Noselect) "/" ~/Mail/foo
             
                          C: A101 LIST "" ""
                          S: * LIST (\Noselect) "/" ""
                          S: A101 OK LIST Completed
            */

            // Hierarchy delimiter request.
            if(string.IsNullOrEmpty(m_FolderName)){
                return "* LIST (\\Noselect) \"/\" \"\"\r\n";
            }
            else{
                StringBuilder retVal = new StringBuilder();
                retVal.Append("* LIST (");
                if(m_pFolderAttributes != null){
                    for(int i=0;i<m_pFolderAttributes.Length;i++){
                        if(i > 0){
                            retVal.Append(" ");
                        }
                        retVal.Append(m_pFolderAttributes[i]);
                    }
                }
                retVal.Append(") ");
                retVal.Append("\"" + m_Delimiter + "\" ");
                retVal.Append(IMAP_Utils.EncodeMailbox(m_FolderName,encoding));
                retVal.Append("\r\n");

                return retVal.ToString();
            }
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
        /// Gets hierarchy delimiter char.
        /// </summary>
        public char HierarchyDelimiter
        {
            get{ return m_Delimiter; }
        }

        /// <summary>
        /// Gets folder attributes list.
        /// </summary>
        public string[] FolderAttributes
        {
            get{ return m_pFolderAttributes; }
        }

        #endregion
    }
}
