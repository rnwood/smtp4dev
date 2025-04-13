using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP.Server
{
    /// <summary>
    /// This class provides data for <see cref="FTP_Session.Appe"/> event.
    /// </summary>
    public class FTP_e_Appe : EventArgs
    {
        private string            m_FileName    = null;
        private FTP_t_ReplyLine[] m_pReplyLines = null;
        private Stream            m_pFileStream = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="file">File name with option path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public FTP_e_Appe(string file)
        {
            if(file == null){
                throw new ArgumentNullException("file");
            }
            if(file == string.Empty){
                throw new ArgumentException("Argument 'file' name must be specified.","file");
            }

            m_FileName = file;
        }


        #region Properties implementation

        /// <summary>
        /// Gets or sets error response.
        /// </summary>
        public FTP_t_ReplyLine[] Error
        {
            get{ return m_pReplyLines; }

            set{ m_pReplyLines = value; }
        }

        /// <summary>
        /// Gets file name with optional path.
        /// </summary>
        public string FileName
        {
            get{ return m_FileName; }
        }

        /// <summary>
        /// Gets or sets file stream.
        /// </summary>
        public Stream FileStream
        {
            get{ return m_pFileStream; }

            set{ m_pFileStream = value; }
        }

        #endregion
    }
}
