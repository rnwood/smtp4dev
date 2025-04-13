using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Copy">IMAP_Session.Copy</b> event.
    /// </summary>
    public class IMAP_e_Copy : EventArgs
    {
        private IMAP_r_ServerStatus m_pResponse     = null;
        private string              m_SourceFolder  = null;
        private string              m_TargetFolder  = null;
        private IMAP_MessageInfo[]  m_pMessagesInfo = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="sourceFolder">Source folder name with optional path.</param>
        /// <param name="targetFolder">Target folder name </param>
        /// <param name="messagesInfo">Messages info.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sourceFolder</b>,<b>targetFolder</b>,<b>messagesInfo</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_Copy(string sourceFolder,string targetFolder,IMAP_MessageInfo[] messagesInfo,IMAP_r_ServerStatus response)
        {
            if(sourceFolder == null){
                throw new ArgumentNullException("sourceFolder");
            }
            if(targetFolder == null){
                throw new ArgumentNullException("targetFolder");
            }
            if(messagesInfo == null){
                throw new ArgumentNullException("messagesInfo");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_pResponse     = response;
            m_SourceFolder  = sourceFolder;
            m_TargetFolder  = targetFolder;
            m_pMessagesInfo = messagesInfo;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets IMAP server response to this operation.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference value set.</exception>
        public IMAP_r_ServerStatus Response
        {
            get{ return m_pResponse; }

            set{ 
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_pResponse = value; 
            }
        }

        /// <summary>
        /// Gets source folder name with optional path.
        /// </summary>
        public string SourceFolder
        {
            get{ return m_SourceFolder; }
        }

        /// <summary>
        /// Gets target folder name with optional path.
        /// </summary>
        public string TargetFolder
        {
            get{ return m_TargetFolder; }
        }

        /// <summary>
        /// Gets messages info.
        /// </summary>
        public IMAP_MessageInfo[] MessagesInfo
        {
            get{ return m_pMessagesInfo; }
        }

        #endregion
    }
}
