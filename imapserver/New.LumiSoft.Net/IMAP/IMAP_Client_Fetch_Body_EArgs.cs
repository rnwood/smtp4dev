using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// This class represents FETCH BODY[] data item. Defined in RFC 3501.
    /// </summary>
    [Obsolete("Use Fetch(bool uid,IMAP_t_SeqSet seqSet,IMAP_t_Fetch_i[] items,EventHandler<EventArgs<IMAP_r_u>> callback) intead.")]
    public class IMAP_Client_Fetch_Body_EArgs : EventArgs
    {
        private string m_Section = null;
        private int    m_Offset  = -1;
        private Stream m_pStream = null;

        /// <summary>
        /// Defualt constructor.
        /// </summary>
        /// <param name="bodySection">Body section value.</param>
        /// <param name="offset">Body data offset.</param>
        internal IMAP_Client_Fetch_Body_EArgs(string bodySection,int offset)
        {
            m_Section = bodySection;
            m_Offset  = offset;
        }


        #region Properties implementation

        /// <summary>
        /// Gets BODY section value. Value null means not specified(full message).
        /// </summary>
        public string BodySection
        {
            get{ return m_Section; }
        }

        /// <summary>
        /// Gets BODY data returning start offset. Value null means not specified.
        /// </summary>
        public int Offset
        {
            get{ return m_Offset; }
        }

        /// <summary>
        /// Gets or sets stream where BODY data is stored.
        /// </summary>
        public Stream Stream
        {
            get{ return m_pStream; }

            set{ m_pStream = value; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This method is called when message storing has completed.
        /// </summary>
        public event EventHandler StoringCompleted = null;

        #region method OnStoringCompleted

        /// <summary>
        /// Raises <b>StoringCompleted</b> event.
        /// </summary>
        internal void OnStoringCompleted()
        {
            if(this.StoringCompleted != null){
                this.StoringCompleted(this,new EventArgs());
            }
        }

        #endregion

        #endregion
    }
}
