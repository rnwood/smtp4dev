using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{    
    /// <summary>
    /// This class provides data for <b>SIP_ServerTransaction.ResponseSent</b> method.
    /// </summary>
    public class SIP_ResponseSentEventArgs : EventArgs
    {
        private SIP_ServerTransaction m_pTransaction = null;
        private SIP_Response          m_pResponse    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="transaction">Server transaction.</param>
        /// <param name="response">SIP response.</param>
        /// <exception cref="ArgumentNullException">Is raised when any of the arguments is null.</exception>
        public SIP_ResponseSentEventArgs(SIP_ServerTransaction transaction,SIP_Response response)
        {
            if(transaction == null){
                throw new ArgumentNullException("transaction");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_pTransaction = transaction;
            m_pResponse    = response;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets server transaction which sent response.
        /// </summary>
        public SIP_ServerTransaction ServerTransaction
        {
            get{ return m_pTransaction; }
        }

        /// <summary>
        /// Gets response which was sent.
        /// </summary>
        public SIP_Response Response
        {
            get{ return m_pResponse; }
        }

        #endregion

    }
}
