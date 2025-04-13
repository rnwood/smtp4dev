using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class provides data for SIP_Stack.ValidateRequest event.
    /// </summary>
    public class SIP_ValidateRequestEventArgs : EventArgs
    {
        private SIP_Request m_pRequest        = null;
        private IPEndPoint  m_pRemoteEndPoint = null;
        private string      m_ResponseCode    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="request">Incoming SIP request.</param>
        /// <param name="remoteEndpoint">IP end point what made request.</param>
        public SIP_ValidateRequestEventArgs(SIP_Request request,IPEndPoint remoteEndpoint)
        {
            m_pRequest        = request;
            m_pRemoteEndPoint = remoteEndpoint;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets incoming SIP request.
        /// </summary>
        public SIP_Request Request
        {
            get{ return m_pRequest; }
        }

        /// <summary>
        /// Gets IP end point what made request.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get{ return m_pRemoteEndPoint; }
        }

        /// <summary>
        /// Gets or sets response code. Value null means SIP stack will handle it.
        /// </summary>
        public string ResponseCode
        {
            get{ return m_ResponseCode; }

            set{ m_ResponseCode = value; }
        }

        #endregion
    }
}
