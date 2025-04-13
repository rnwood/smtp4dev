using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.GetAcl">IMAP_Session.GetAcl</b> event.
    /// </summary>
    public class IMAP_e_GetAcl : EventArgs
    {
        private List<IMAP_r_u_Acl>  m_pAclResponses = null;
        private IMAP_r_ServerStatus m_pResponse     = null;
        private string              m_Folder        = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_GetAcl(string folder,IMAP_r_ServerStatus response)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_Folder    = folder;
            m_pResponse = response;

            m_pAclResponses = new List<IMAP_r_u_Acl>();
        }


        #region Properties implementation
        
        /// <summary>
        /// Gets ACL responses collection.
        /// </summary>
        public List<IMAP_r_u_Acl> AclResponses
        {
            get{ return m_pAclResponses; }
        }

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

        #endregion
    }
}
