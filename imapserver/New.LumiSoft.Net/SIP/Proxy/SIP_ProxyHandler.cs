using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// This base class for SIP proxy request handlers.
    /// </summary>
    public class SIP_ProxyHandler
    {
        private object m_pTag = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_ProxyHandler()
        {
        }


        #region method ProcessRequest

        /// <summary>
        /// This method is called when new SIP request received.
        /// </summary>
        /// <param name="requestContext">SIP proxy request context.</param>
        /// <returns>Returns true if request handled by this method, otherwise false.</returns>
        /// <remarks>
        /// This method is called when SIP proxy receives new out of transaction request.
        /// </remarks>
        public virtual bool ProcessRequest(SIP_RequestContext requestContext)
        {
            // REMOVE ME:

            /* Handler description.
                *) Any tel: URI is routed to the specified target gateway.
                *) URIs starting with + is routed to the specified target gateway.
                *) Require authentication.
            */

            // TODO: ACK

            // This is not URI we want.
            if(requestContext.Request.RequestLine.Uri.Scheme.ToLower() != "tel" && !(requestContext.Request.RequestLine.Uri is SIP_Uri)){
                return false;
            }

            SIP_Uri requestUri = (SIP_Uri)requestContext.Request.RequestLine.Uri;

            long dummy = 0;
            if(requestUri.User.StartsWith("+") || Int64.TryParse(requestUri.User,out dummy)){
                // Not authenticated, send authentication challenge.
                if(requestContext.User == null){
                    requestContext.ChallengeRequest();

                    return true;
                }

                // Create staefull proxy context for request forwarding.
                SIP_ProxyContext proxyContext = requestContext.ProxyContext;

                // Add target server credentials, if any.
                //proxyContext.Credentials.Add(new NetworkCredential("user","password","domain-realm"));
                // ...

                // Start statefull request proxying.
                proxyContext.Start();

                return true;
            }            

            return false;
        }

        #endregion

        #region method OnResponseReceived

        /*
        public virtual bool OnResponseReceived()
        {
        }
        */

        #endregion
                

        #region method IsLocalUri

        /// <summary>
        /// Gets if the specified URI is local URI.
        /// </summary>
        /// <returns>Returns true if the specified uri is local URI.</returns>
        public bool IsLocalUri()
        {
            return false;
        }

        #endregion

        #region method GetRegistrarContacts

        /// <summary>
        /// 
        /// </summary>
        public void GetRegistrarContacts()
        {
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets a value indicating whether another request can use this handler.
        /// </summary>
        public virtual bool IsReusable
        {
            get{ return false; }
        }

        /// <summary>
        /// Gets or stets user data.
        /// </summary>
        public object Tag
        {
            get{ return m_pTag; }

            set{ m_pTag = value; }
        }

        #endregion

    }
}
