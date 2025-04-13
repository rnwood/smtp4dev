using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Expunge">IMAP_Session.Expunge</b> event.
    /// </summary>
    public class IMAP_e_Expunge : EventArgs
    {
        private IMAP_r_ServerStatus m_pResponse = null;
        private string              m_Folder    = null;
        private IMAP_MessageInfo    m_pMsgInfo  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="msgInfo">Message info.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is riased when <b>folder</b>,<b>msgInfo</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_Expunge(string folder,IMAP_MessageInfo msgInfo,IMAP_r_ServerStatus response)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(msgInfo == null){
                throw new ArgumentNullException("msgInfo");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }
            
            m_pResponse = response;
            m_Folder    = folder;
            m_pMsgInfo  = msgInfo;
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
        /// Gets folder name with optional path.
        /// </summary>
        public string Folder
        {
            get{ return m_Folder; }
        }

        /// <summary>
        /// Gets message info.
        /// </summary>
        public IMAP_MessageInfo MessageInfo
        {
            get{ return m_pMsgInfo; }
        }

        #endregion
    }
}
