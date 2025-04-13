using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Search">IMAP_Session.Search</b> event.
    /// </summary>
    /// <remarks>
    /// IMAP SEARCH handler application should provide message UID per each search criteria matched message
    /// by calling <see cref="IMAP_e_Search.AddMessage(long)"/> method.</remarks>
    public class IMAP_e_Search : EventArgs
    {
        private IMAP_r_ServerStatus m_pResponse = null;
        private IMAP_Search_Key     m_pCriteria = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="criteria">Serach criteria.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>criteria</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_Search(IMAP_Search_Key criteria,IMAP_r_ServerStatus response)
        {
            if(criteria == null){
                throw new ArgumentNullException("criteria");
            }

            m_pResponse = response;
            m_pCriteria = criteria;
        }


        #region method AddMessage

        /// <summary>
        /// Adds message which matches search criteria.
        /// </summary>
        /// <param name="uid">Message UID value.</param>
        public void AddMessage(long uid)
        {
            OnMatched(uid);
        }

        #endregion


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
        /// Gets search criteria.
        /// </summary>
        public IMAP_Search_Key Criteria
        {
            get{ return m_pCriteria; }
        }

        #endregion

        #region Events implementation
                
        /// <summary>
        /// Is raised when new message matches search criteria.
        /// </summary>
        internal event EventHandler<EventArgs<long>> Matched = null;

        #region method OnMatched

        /// <summary>
        /// Raises <b>Matched</b> event.
        /// </summary>
        /// <param name="uid">Message UID.</param>
        private void OnMatched(long uid)
        {
            if(this.Matched != null){
                this.Matched(this,new EventArgs<long>(uid));
            }
        }

        #endregion

        #endregion
    }
}
