using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP STATUS response. Defined in RFC 3501 7.2.4.
    /// </summary>
    public class IMAP_r_u_Status : IMAP_r_u
    {
        private string m_FolderName   = "";
        private int    m_MessageCount = 0;
        private int    m_RecentCount  = 0;
        private long   m_UidNext      = 0;
        private long   m_FolderUid    = 0;
        private int    m_UnseenCount  = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name.</param>
        /// <param name="messagesCount">Messages count. Value -1 means not specified.</param>
        /// <param name="recentCount">The "recent" flag set messages count. Value -1 means not specified.</param>
        /// <param name="uidNext">Next message predictable UID value. Value -1 means not specified.</param>
        /// <param name="folderUid">Folder UID value. Value -1 means not specified.</param>
        /// <param name="unseenCount">Unseen messages count. Value -1 means not specified.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_Status(string folder,int messagesCount,int recentCount,long uidNext,long folderUid,int unseenCount)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            m_FolderName   = folder;
            m_MessageCount = messagesCount;
            m_RecentCount  = recentCount;
            m_UidNext      = uidNext;
            m_FolderUid    = folderUid;
            m_UnseenCount  = unseenCount;
        }


        #region static method Parse

        /// <summary>
        /// Parses STATUS response from status-response string.
        /// </summary>
        /// <param name="response">Satatus response string.</param>
        /// <returns>Returns parsed STATUS response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Status Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3501 7.2.4 STATUS Response.
                Contents:   name
                            status parenthesized list

                The STATUS response occurs as a result of an STATUS command.  It
                returns the mailbox name that matches the STATUS specification and
                the requested mailbox status information.

                Example:    S: * STATUS blurdybloop (MESSAGES 231 UIDNEXT 44292)
            */

            StringReader r = new StringReader(response);
            // Eat "*"
            r.ReadWord();
            // Eat "STATUS"
            r.ReadWord();
            
            int  messages  = 0;
            int  recent    = 0;
            long uidNext   = 0;
            long folderUid = 0;
            int  unseen    = 0;

            string   folder = TextUtils.UnQuoteString(IMAP_Utils.Decode_IMAP_UTF7_String(r.ReadWord()));
            string[] items  = r.ReadParenthesized().Split(' ');
            for(int i=0;i<items.Length;i+=2){
                if(items[i].Equals("MESSAGES",StringComparison.InvariantCultureIgnoreCase)){
                    messages = Convert.ToInt32(items[i + 1]);
                }
                else if(items[i].Equals("RECENT",StringComparison.InvariantCultureIgnoreCase)){
                    recent = Convert.ToInt32(items[i + 1]);
                }
                else if(items[i].Equals("UIDNEXT",StringComparison.InvariantCultureIgnoreCase)){
                    uidNext = Convert.ToInt64(items[i + 1]);
                }
                else if(items[i].Equals("UIDVALIDITY",StringComparison.InvariantCultureIgnoreCase)){
                    folderUid = Convert.ToInt64(items[i + 1]);
                }
                else if(items[i].Equals("UNSEEN",StringComparison.InvariantCultureIgnoreCase)){
                    unseen = Convert.ToInt32(items[i + 1]);
                }
            }

            return new IMAP_r_u_Status(folder,messages,recent,uidNext,folderUid,unseen);
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
            // Example:    S: * STATUS blurdybloop (MESSAGES 231 UIDNEXT 44292)

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* STATUS");
            retVal.Append(" " + IMAP_Utils.EncodeMailbox(m_FolderName,encoding));
            retVal.Append(" (");
            bool firstItem = true;
            if(m_MessageCount >= 0){ 
                retVal.Append("MESSAGES " + m_MessageCount);   
                firstItem = false;
            }
            if(m_RecentCount >= 0){ 
                if(!firstItem){
                    retVal.Append(' ');
                }
                retVal.Append("RECENT " + m_RecentCount);  
                firstItem = false;
            }
            if(m_UidNext >= 0){ 
                if(!firstItem){
                    retVal.Append(' ');
                }
                retVal.Append("UIDNEXT " + m_UidNext);  
                firstItem = false;
            }
            if(m_FolderUid >= 0){  
                if(!firstItem){
                    retVal.Append(' ');
                }
                retVal.Append("UIDVALIDITY " + m_FolderUid); 
                firstItem = false;
            }
            if(m_UnseenCount >= 0){
                if(!firstItem){
                    retVal.Append(' ');
                }
                retVal.Append("UNSEEN " + m_UnseenCount);   
                firstItem = false;
            }
            retVal.Append(")\r\n");
            
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
        /// Gets messages count.
        /// </summary>
        public int MessagesCount
        {
            get{ return m_MessageCount; }
        }

        /// <summary>
        /// Gets "recent" flag set messages count.
        /// </summary>
        public int RecentCount
        {
            get{ return m_RecentCount;}
        }

        /// <summary>
        /// Gets next message predictable UID value.
        /// </summary>
        public long UidNext
        {
            get{ return m_UidNext; }
        }

        /// <summary>
        /// Gets folder UID value.
        /// </summary>
        public long FolderUid
        {
            get{ return m_FolderUid; }
        }

        /// <summary>
        /// Gets unseen messages count.
        /// </summary>
        public int UnseenCount
        {
            get{ return m_UnseenCount; }
        }

        #endregion
    }
}
