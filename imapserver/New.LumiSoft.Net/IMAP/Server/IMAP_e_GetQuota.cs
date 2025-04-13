using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.GetQuota">IMAP_Session.GetQuota</b> event.
    /// </summary>
    public class IMAP_e_GetQuota : EventArgs
    {
        private List<IMAP_r_u_Quota> m_pQuotaResponses = null;
        private IMAP_r_ServerStatus  m_pResponse       = null;
        private string               m_QuotaRoot       = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="quotaRoot">Quota root name.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>quotaRoot</b> is null reference.</exception>
        internal IMAP_e_GetQuota(string quotaRoot,IMAP_r_ServerStatus response)
        {
            if(quotaRoot == null){
                throw new ArgumentNullException("quotaRoot");
            }

            m_QuotaRoot = quotaRoot;
            m_pResponse = response;

            m_pQuotaResponses = new List<IMAP_r_u_Quota>();
        }


        #region Properties implementation

        /// <summary>
        /// Gets QUOTA responses collection.
        /// </summary>
        public List<IMAP_r_u_Quota> QuotaResponses
        {
            get{ return m_pQuotaResponses; }
        }

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
        /// Gets quopta root name.
        /// </summary>
        public string QuotaRoot
        {
            get{ return m_QuotaRoot; }
        }

        #endregion
    }
}
