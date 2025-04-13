using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Rename">IMAP_Session.Rename</b> event.
    /// </summary>
    public class IMAP_e_Rename : EventArgs
    {
        private IMAP_r_ServerStatus m_pResponse     = null;
        private string              m_CmdTag        = null;
        private string              m_CurrentFolder = null;
        private string              m_NewFolder     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="currentFolder">Current folder name with optional path.</param>
        /// <param name="newFolder">New folder name with optional path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>cmdTag</b>,<b>currentFolder</b> or <b>newFolder</b> is null reference.</exception>
        internal IMAP_e_Rename(string cmdTag,string currentFolder,string newFolder)
        {
            if(cmdTag == null){
                throw new ArgumentNullException("cmdTag");
            }
            if(currentFolder == null){
                throw new ArgumentNullException("currentFolder");
            }
            if(newFolder == null){
                throw new ArgumentNullException("newFolder");
            }

            m_CmdTag        = cmdTag;
            m_CurrentFolder = currentFolder;
            m_NewFolder     = newFolder;
        }


        #region Properties impalementation

        /// <summary>
        /// Gets or sets IMAP server response to this operation.
        /// </summary>
        public IMAP_r_ServerStatus Response
        {
            get{ return m_pResponse; }

            set{ m_pResponse = value; }
        }

        /// <summary>
        /// Gets IMAP command tag value.
        /// </summary>
        public string CmdTag
        {
            get{ return m_CmdTag; }
        }

        /// <summary>
        /// Gets current folder name with optional path.
        /// </summary>
        public string CurrentFolder
        {
            get{ return m_CurrentFolder; }
        }

        /// <summary>
        /// Gets new folder name with optional path.
        /// </summary>
        public string NewFolder
        {
            get{ return m_NewFolder; }
        }

        #endregion
    }
}
