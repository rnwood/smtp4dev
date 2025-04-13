using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.ListRights">IMAP_Session.ListRights</b> event.
    /// </summary>
    public class IMAP_e_ListRights : EventArgs
    {
        private IMAP_r_u_ListRights m_pListRightsResponse = null;
        private IMAP_r_ServerStatus m_pResponse           = null;
        private string              m_Folder              = null;
        private string              m_Identifier          = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="identifier">ACL identifier (normally user or group name).</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b>,<b>identifier</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_ListRights(string folder,string identifier,IMAP_r_ServerStatus response)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(identifier == null){
                throw new ArgumentNullException("identifier");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_Folder     = folder;
            m_Identifier = identifier;
            m_pResponse  = response;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets LISTRIGHTS response.
        /// </summary>
        public IMAP_r_u_ListRights ListRightsResponse
        {
            get{ return m_pListRightsResponse; }

            set{ m_pListRightsResponse = value; }
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

        /// <summary>
        /// Gets ACL identifier (normally user or group name).
        /// </summary>
        public string Identifier
        {
            get{ return m_Identifier; }
        }

        #endregion
    }
}
