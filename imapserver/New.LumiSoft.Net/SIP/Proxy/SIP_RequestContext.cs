using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// This class represent incoming new(out of transaction) SIP request.
    /// </summary>
    public class SIP_RequestContext
    {
        private SIP_Proxy             m_pProxy        = null;
        private SIP_Request           m_pRequest      = null;
        private SIP_Flow              m_pFlow         = null;
        private SIP_ServerTransaction m_pTransaction  = null;
        private List<SIP_ProxyTarget> m_pTargets      = null;
        private string                m_User          = null;
        private SIP_ProxyContext      m_pProxyContext = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="proxy">Owner SIP proxy server.</param>
        /// <param name="request">The request what is represented by this context.</param>
        /// <param name="flow">Data flow what received request.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>proxy</b>, <b>request</b> or <b>flow</b> is null reference.</exception>
        internal SIP_RequestContext(SIP_Proxy proxy,SIP_Request request,SIP_Flow flow)
        {
            if(proxy == null){
                throw new ArgumentNullException("proxy");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(flow == null){
                throw new ArgumentNullException("flow");
            }

            m_pProxy   = proxy;
            m_pRequest = request;
            m_pFlow    = flow;

            m_pTargets = new List<SIP_ProxyTarget>();
        }


        #region method ForwardStatelessly

        /// <summary>
        /// Forwards current request statelessly.
        /// </summary>
        public void ForwardStatelessly()
        {
            // TODO:
            throw new NotImplementedException();
        }

        #endregion

        #region method ChallengeRequest

        /// <summary>
        /// Forces incoming request to authenticate by sending authentication-challenge response to request sender.
        /// </summary>
        public void ChallengeRequest()
        {
            throw new NotImplementedException();
        }

        #endregion


        #region method SetUser

        /// <summary>
        /// Sets current user.
        /// </summary>
        /// <param name="user">User name.</param>
        internal void SetUser(string user)
        {
            m_User = user;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets current incoming SIP request.
        /// </summary>
        public SIP_Request Request
        {
            get{ return m_pRequest; }
        }

        /// <summary>
        /// Gets or creates server transaction that will handle request.
        /// </summary>
        /// <remarks>If server transaction doesn't exist, it will be created.</remarks>
        /// <exception cref="InvalidOperationException">Is raised when this request-Method is ACK(ACK request is transactionless SIP method).</exception>
        public SIP_ServerTransaction Transaction
        {
            get{
                if(Request.RequestLine.Method == SIP_Methods.ACK){
                    throw new InvalidOperationException("ACK request is transactionless SIP method.");
                }

                // Create server transaction for that request.
                if(m_pTransaction == null){
                    m_pTransaction = m_pProxy.Stack.TransactionLayer.EnsureServerTransaction(m_pFlow,m_pRequest);
                }

                return m_pTransaction; 
            }
        }

        /// <summary>
        /// Gets proxy determined request targets.
        /// </summary>
        public List<SIP_ProxyTarget> Targets
        {
            get{ return m_pTargets; }
        }

        /// <summary>
        /// Gets authenticated user name. Returns null if user not authenticated.
        /// </summary>
        public string User
        {
            get{ return m_User; }
        }

        /// <summary>
        /// Gets or creates statefull proxy context for this request.
        /// </summary>
        public SIP_ProxyContext ProxyContext
        {
            get{
                if(m_pProxyContext == null){
                    m_pProxy.CreateProxyContext(this,this.Transaction,this.Request,true);
                }

                return m_pProxyContext; 
            }
        }

        #endregion
    }
}
