using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Select">IMAP_Session.Select</b> event.
    /// </summary>
    public class IMAP_e_Select : EventArgs
    {
        private string              m_CmdTag          = null;
        private IMAP_r_ServerStatus m_pResponse       = null;
        private string              m_Folder          = null;
        private bool                m_IsReadOnly      = false;
        private int                 m_FolderUID       = 0;
        private List<string>        m_pFlags          = null;
        private List<string>        m_pPermanentFlags = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="folder">Folder name with optional path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>cmdTag</b> or <b>folder</b> is null reference.</exception>
        internal IMAP_e_Select(string cmdTag,string folder)
        {
            if(cmdTag == null){
                throw new ArgumentNullException("cmdTag");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }

            m_CmdTag = cmdTag;
            m_Folder = folder;

            m_pFlags = new List<string>();
            m_pPermanentFlags = new List<string>();

            // Add default falgs.
            m_pFlags.AddRange(new string[]{"\\Answered","\\Flagged","\\Deleted","\\Seen","\\Draft"});
            m_pPermanentFlags.AddRange(new string[]{"\\Answered","\\Flagged","\\Deleted","\\Seen","\\Draft"});
        }


        #region Properties implementation

        /// <summary>
        /// Gets command tag.
        /// </summary>
        public string CmdTag
        {
            get{ return m_CmdTag; }
        }

        /// <summary>
        /// Gets or sets IMAP server error response to this operation. Value means no error.
        /// </summary>
        public IMAP_r_ServerStatus ErrorResponse
        {
            get{ return m_pResponse; }

            set{ m_pResponse = value; }
        }

        /// <summary>
        /// Gets folder name with optional path.
        /// </summary>
        public string Folder
        {
            get{ return m_Folder; }
        }

        /// <summary>
        /// Gets or sets if specified folder is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get{ return m_IsReadOnly; }

            set{ m_IsReadOnly = value; }
        }

        /// <summary>
        /// Gets or sets folder UID value. Value 0 means not specified.
        /// </summary>
        public int FolderUID
        {
            get{ return m_FolderUID; }

            set{ m_FolderUID = value; }
        }

        /// <summary>
        /// Gets folder supported flags collection.
        /// </summary>
        public List<string> Flags
        {
            get{ return m_pFlags; }
        }

        /// <summary>
        /// Gets folder supported permanent flags collection.
        /// </summary>
        public List<string> PermanentFlags
        {
            get{ return m_pPermanentFlags; }
        }

        #endregion
    }
}
