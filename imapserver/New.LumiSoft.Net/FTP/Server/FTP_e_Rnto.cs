using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP.Server
{
    /// <summary>
    /// This class provides data for <see cref="FTP_Session.Rnto"/> event.
    /// </summary>
    public class FTP_e_Rnto : EventArgs
    {
        private FTP_t_ReplyLine[] m_pReplyLines = null;
        private string            m_SourcePath  = null;
        private string            m_TargetPath  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="sourcePath">Source file or directory path.</param>
        /// <param name="targetPath">Target file or directory path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sourcePath</b> or <b>targetPath</b> is null reference.</exception>
        public FTP_e_Rnto(string sourcePath,string targetPath)
        {
            if(sourcePath == null){
                throw new ArgumentNullException("sourcePath");
            }
            if(targetPath == null){
                throw new ArgumentNullException("targetPath");
            }

            m_SourcePath = sourcePath;
            m_TargetPath = targetPath;
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
        /// Gets source path.
        /// </summary>
        public string SourcePath
        {
            get{ return m_SourcePath; }
        }

        /// <summary>
        /// Gets target path.
        /// </summary>
        public string TargetPath
        {
            get{ return m_TargetPath; }
        }

        #endregion
    }
}
