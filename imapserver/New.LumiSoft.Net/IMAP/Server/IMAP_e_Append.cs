using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Append">IMAP_Session.Append</b> event.
    /// </summary>
    public class IMAP_e_Append : EventArgs
    {
        private IMAP_r_ServerStatus m_pResponse = null;
        private string              m_Folder    = null;
        private string[]            m_pFlags    = null;
        private DateTime            m_Date      = DateTime.MinValue;
        private int                 m_Size      = 0;
        private Stream              m_pStream   = null;      

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="date">IMAP internal date. Value DateTime.MinValue means not specified.</param>
        /// <param name="size">Message size in bytes.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is riased when <b>folder</b>,<b>flags</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_Append(string folder,string[] flags,DateTime date,int size,IMAP_r_ServerStatus response)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(flags == null){
                throw new ArgumentNullException("flags");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_Folder    = folder;
            m_pFlags    = flags;
            m_Date      = date;
            m_Size      = size;
            m_pResponse = response;
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
        /// Gets message flags.
        /// </summary>
        public string[] Flags
        {
            get{ return m_pFlags; }
        }

        /// <summary>
        /// Gets message internal date. Value DateTime.MinValue means not specified.
        /// </summary>
        public DateTime InternalDate
        {
            get{ return m_Date; }
        }

        /// <summary>
        /// Gets message size in bytes.
        /// </summary>
        public int Size
        {
            get{ return m_Size; }
        }

        /// <summary>
        /// Gets or sets message stream.
        /// </summary>
        public Stream Stream
        {
            get{ return m_pStream; }

            set{ m_pStream = value; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This event is raised when message storing has completed.
        /// </summary>
        public event EventHandler Completed = null;

        #region method OnCompleted

        /// <summary>
        /// Raises <b>Completed</b> event.
        /// </summary>
        internal void OnCompleted()
        {
            if(this.Completed != null){
                this.Completed(this,new EventArgs());
            }

            // Release event.
            this.Completed = null;
        }

        #endregion

        #endregion
    }
}
