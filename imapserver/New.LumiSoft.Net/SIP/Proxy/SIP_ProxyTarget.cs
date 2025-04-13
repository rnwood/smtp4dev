using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// Represents SIP proxy target in the SIP proxy "target set". Defined in RFC 3261 16.
    /// </summary>
    public class SIP_ProxyTarget
    {
        private SIP_Uri  m_pTargetUri = null;
        private SIP_Flow m_pFlow      = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="targetUri">Target request-URI.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>targetUri</b> is null reference.</exception>
        public SIP_ProxyTarget(SIP_Uri targetUri) : this(targetUri,null)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="targetUri">Target request-URI.</param>
        /// <param name="flow">Data flow to try for forwarding.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>targetUri</b> is null reference.</exception>
        public SIP_ProxyTarget(SIP_Uri targetUri,SIP_Flow flow)
        {
            if(targetUri == null){
                throw new ArgumentNullException("targetUri");
            }

            m_pTargetUri = targetUri;
            m_pFlow      = flow;
        }


        #region Properties implementation

        /// <summary>
        /// Gets target URI.
        /// </summary>
        public SIP_Uri TargetUri
        {
            get{ return m_pTargetUri; }
        }

        /// <summary>
        /// Gets data flow. Value null means that new flow must created.
        /// </summary>
        public SIP_Flow Flow
        {
            get{ return m_pFlow; }
        }

        #endregion

    }
}
