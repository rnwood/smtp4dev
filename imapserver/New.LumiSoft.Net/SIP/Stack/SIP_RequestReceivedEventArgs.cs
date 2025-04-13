using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class provides data for <b>SIP_Dialog.RequestReceived</b> event and <b>SIP_Core.OnRequestReceived</b>> method.
    /// </summary>
    public class SIP_RequestReceivedEventArgs : EventArgs
    {
        private SIP_Stack             m_pStack       = null;
        private SIP_Flow              m_pFlow        = null;
        private SIP_Request           m_pRequest     = null;
        private SIP_ServerTransaction m_pTransaction = null;
        private bool                  m_IsHandled    = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Reference to SIP stack.</param>
        /// <param name="flow">SIP data flow.</param>
        /// <param name="request">Recieved request.</param>
        internal SIP_RequestReceivedEventArgs(SIP_Stack stack,SIP_Flow flow,SIP_Request request) : this(stack,flow,request,null)
        {           
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Reference to SIP stack.</param>
        /// <param name="flow">SIP data flow.</param>
        /// <param name="request">Recieved request.</param>
        /// <param name="transaction">SIP server transaction which must be used to send response back to request maker.</param>
        internal SIP_RequestReceivedEventArgs(SIP_Stack stack,SIP_Flow flow,SIP_Request request,SIP_ServerTransaction transaction)
        {
            m_pStack       = stack;
            m_pFlow        = flow;
            m_pRequest     = request;
            m_pTransaction = transaction;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets data flow what received SIP request.
        /// </summary>
        public SIP_Flow Flow
        {
            get{ return m_pFlow; }
        }

        /// <summary>
        /// Gets the received rquest.
        /// </summary>
        public SIP_Request Request
        {
            get{ return m_pRequest; }
        }

        /// <summary>
        /// Gets server transaction for that request. Server transaction is created when this property is 
        /// first accessed. If you don't need server transaction for that request, for example statless proxy, 
        /// just don't access this property. For ACK method, this method always return null, because ACK 
        /// doesn't create transaction !
        /// </summary>
        public SIP_ServerTransaction ServerTransaction
        {
            get{
                // ACK never creates transaction.
                if(m_pRequest.RequestLine.Method == SIP_Methods.ACK){
                    return null;
                }

                // Create server transaction for that request.
                if(m_pTransaction == null){
                    m_pTransaction = m_pStack.TransactionLayer.EnsureServerTransaction(m_pFlow,m_pRequest);
                }

                return m_pTransaction; 
            }
        }

        /// <summary>
        /// Gets SIP dialog where Request belongs to. Returns null if Request doesn't belong any dialog.
        /// </summary>
        public SIP_Dialog Dialog
        {
            get{ return m_pStack.TransactionLayer.MatchDialog(m_pRequest); }
        }

        /// <summary>
        /// Gets or sets if request is handled.
        /// </summary>
        public bool IsHandled
        {
            get{ return m_IsHandled; }

            set{ m_IsHandled = true; }
        }

        #endregion

    }
}
