using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP.Server
{
    /// <summary>
    /// This class provides data for <see cref="FTP_Session.Dele"/> event.
    /// </summary>
    public class FTP_e_Dele : EventArgs
    {
        private FTP_t_ReplyLine[] m_pReplyLines = null;
        private string            m_FileName    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="fileName">File name with optional path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>fileName</b> is null reference.</exception>
        public FTP_e_Dele(string fileName)
        {
            if(fileName == null){
                throw new ArgumentNullException("fileName");
            }

            m_FileName = fileName;
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
        /// Gets file name with optional path.
        /// </summary>
        public string FileName
        {
            get{ return m_FileName; }
        }

        #endregion
    }
}
