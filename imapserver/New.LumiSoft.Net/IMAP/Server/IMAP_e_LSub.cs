using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.List">IMAP_Session.LSub</b> event.
    /// </summary>
    public class IMAP_e_LSub : EventArgs
    {
        private string              m_FolderReferenceName = null;
        private string              m_FolderFilter        = null;
        private List<IMAP_r_u_LSub> m_pFolders            = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="referenceName">Folder reference name.</param>
        /// <param name="folderFilter">Folder filter.</param>
        internal IMAP_e_LSub(string referenceName,string folderFilter)
        {
            m_FolderReferenceName = referenceName;
            m_FolderFilter        = folderFilter;

            m_pFolders = new List<IMAP_r_u_LSub>();
        }


        #region Properties implementation

        /// <summary>
        /// Gets folder reference name. Value null means not specified.
        /// </summary>
        public string FolderReferenceName
        {
            get{ return m_FolderReferenceName; }
        }

        /// <summary>
        /// Gets folder filter.
        /// </summary>
        /// <remarks>
        /// The character "*" is a wildcard, and matches zero or more
        /// characters at this position.  The character "%" is similar to "*",
        /// but it does not match a hierarchy delimiter.  If the "%" wildcard
        /// is the last character of a mailbox name argument, matching levels
        /// of hierarchy are also returned.
        /// </remarks>
        public string FolderFilter
        {
            get{ return m_FolderFilter; }
        }

        /// <summary>
        /// Gets IMAP folders collection.
        /// </summary>
        public List<IMAP_r_u_LSub> Folders
        {
            get{ return m_pFolders; }
        }

        #endregion
    }
}
