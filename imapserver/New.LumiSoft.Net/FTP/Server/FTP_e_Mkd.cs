using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP.Server
{
    /// <summary>
    /// This class provides data for <see cref="FTP_Session.Mkd"/> event.
    /// </summary>
    public class FTP_e_Mkd : EventArgs
    {
        private FTP_t_ReplyLine[] m_pReplyLines = null;
        private string            m_DirName     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="dirName">Directory name with optional path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>dirName</b> is null reference.</exception>
        public FTP_e_Mkd(string dirName)
        {
            if(dirName == null){
                throw new ArgumentNullException("dirName");
            }

            m_DirName = dirName;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets FTP server response.
        /// </summary>
        public FTP_t_ReplyLine[] Response
        {
            get{ return m_pReplyLines; }

            set{ m_pReplyLines = value; }
        }

        /// <summary>
        /// Gets directory name with optional path.
        /// </summary>
        public string DirName
        {
            get{ return m_DirName; }
        }

        #endregion
    }
}
