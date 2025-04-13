using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Client.FetchGetStoreStream">IMAP_Client.FetchGetStoreStream</b> event.
    /// </summary>
    public class IMAP_Client_e_FetchGetStoreStream : EventArgs
    {
        private IMAP_r_u_Fetch   m_pFetchResponse = null;
        private IMAP_t_Fetch_r_i m_pDataItem      = null;
        private Stream           m_pStream        = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="fetch">Fetch response.</param>
        /// <param name="dataItem">Fetch data-item.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>fetch</b> or <b>dataItem</b> is null reference.</exception>
        public IMAP_Client_e_FetchGetStoreStream(IMAP_r_u_Fetch fetch,IMAP_t_Fetch_r_i dataItem)
        {
            if(fetch == null){
                throw new ArgumentNullException("fetch");
            }
            if(dataItem == null){
                throw new ArgumentNullException("dataItem");
            }

            m_pFetchResponse = fetch;
            m_pDataItem      = dataItem;
        }


        #region Properties implementation

        /// <summary>
        /// Gets related FETCH response.
        /// </summary>
        public IMAP_r_u_Fetch FetchResponse
        {
            get{ return m_pFetchResponse; }
        }

        /// <summary>
        /// Gets FETCH data-item which stream to get.
        /// </summary>
        public IMAP_t_Fetch_r_i DataItem
        {
            get{ return m_pDataItem; }
        }

        /// <summary>
        /// Gets stream where to store data-item data.
        /// </summary>
        public Stream Stream
        {
            get{ return m_pStream; }

            set{ m_pStream = value; }
        }

        #endregion
    }
}
