using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP LSUB response. Defined in RFC 3501 7.2.3.
    /// </summary>
    public class IMAP_r_u_LSub : IMAP_r_u
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
        public IMAP_r_u_LSub(string folder,char delimiter,string[] attributes)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            m_FolderName = folder;
            m_Delimiter  = delimiter;
            if(attributes != null){
                m_pFolderAttributes = attributes;
            }
        }


        #region static method Parse

        /// <summary>
        /// Parses LSUB response from lsub-response string.
        /// </summary>
        /// <param name="lSubResponse">LSub response string.</param>
        /// <returns>Returns parsed lsub response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>lSubResponse</b> is null reference.</exception>
        public static IMAP_r_u_LSub Parse(string lSubResponse)
        {
            if(lSubResponse == null){
                throw new ArgumentNullException("lSubResponse");
            }

            /* RFC 3501 7.2.3. LSUB Response.
                Contents:   name attributes
                            hierarchy delimiter
                            name

                The LSUB response occurs as a result of an LSUB command.  It
                returns a single name that matches the LSUB specification.  There
                can be multiple LSUB responses for a single LSUB command.  The
                data is identical in format to the LIST response.

                Example:    S: * LSUB () "." #news.comp.mail.misc
            */

            StringReader r = new StringReader(lSubResponse);
            // Eat "*"
            r.ReadWord();
            // Eat "LSUB"
            r.ReadWord();

            string attributes = r.ReadParenthesized();
            string delimiter  = r.ReadWord();
            string folder     = TextUtils.UnQuoteString(IMAP_Utils.DecodeMailbox(r.ReadToEnd().Trim()));

            return new IMAP_r_u_LSub(folder,delimiter[0],attributes == string.Empty ? new string[0] : attributes.Split(' '));
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
            // Example:    S: * LSUB (\Noselect) "/" ~/Mail/foo

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* LSUB (");
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
